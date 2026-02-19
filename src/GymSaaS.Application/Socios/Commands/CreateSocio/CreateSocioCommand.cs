// =============================================================================
// ARCHIVO: CreateSocioCommand.cs
// CAPA: Application/Socios/Commands/CreateSocio
// PROPÓSITO: Crea un nuevo socio en el sistema y lo asocia a su Tenant.
//
// CAMBIOS EN ESTE ARCHIVO:
//   - Se inyecta INotificationService en el handler (NUEVO).
//   - Se agrega llamada a EnviarBienvenidaNuevoSocio() después de guardar
//     el socio exitosamente (NUEVO).
//   - Toda la lógica original (validación de límite de socios SaaS, creación
//     del socio, asignación automática de membresía) se conserva INTACTA.
// =============================================================================

using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Socios.Commands.CreateSocio
{
    public record CreateSocioCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string Dni { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Telefono { get; init; } = string.Empty;
        public DateTime? FechaNacimiento { get; init; }
        public int TipoMembresiaId { get; init; }
    }

    public class CreateSocioCommandHandler : IRequestHandler<CreateSocioCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        // NUEVO: Servicio de notificaciones y configuración para armar el link del portal
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CreateSocioCommandHandler> _logger;

        public CreateSocioCommandHandler(
            IApplicationDbContext context,
            ICurrentTenantService currentTenantService,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<CreateSocioCommandHandler> logger)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<int> Handle(CreateSocioCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentTenantService.TenantId;

            // =================================================================================
            // LÓGICA ORIGINAL CONSERVADA: VALIDACIÓN DE LÍMITES DEL PLAN (SAAS GATEKEEPER)
            // =================================================================================
            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == tenantId, cancellationToken);

                if (tenant != null && tenant.MaxSocios.HasValue)
                {
                    var cantidadSociosActuales = await _context.Socios
                        .CountAsync(s => s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

                    if (cantidadSociosActuales >= tenant.MaxSocios.Value)
                    {
                        throw new InvalidOperationException(
                            $"Has alcanzado el límite de {tenant.MaxSocios} socios permitidos por tu Plan {tenant.Plan}. " +
                            "¡Tu gimnasio está creciendo! Actualiza a PREMIUM para eliminar los límites y seguir sumando alumnos.");
                    }
                }
            }
            // =================================================================================

            // LÓGICA ORIGINAL CONSERVADA: Creación del socio
            var entity = new Socio
            {
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Dni = request.Dni,
                Email = request.Email,
                Telefono = request.Telefono,
                FechaNacimiento = request.FechaNacimiento,
                FechaAlta = DateTime.UtcNow,
                Activo = true,
                TenantId = tenantId ?? string.Empty
            };

            _context.Socios.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // LÓGICA ORIGINAL CONSERVADA: Asignación automática de membresía
            if (request.TipoMembresiaId > 0)
            {
                var tipo = await _context.TiposMembresia
                    .FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);

                if (tipo != null)
                {
                    var membresia = new MembresiaSocio
                    {
                        SocioId = entity.Id,
                        TipoMembresiaId = tipo.Id,
                        FechaInicio = DateTime.UtcNow,
                        FechaFin = DateTime.UtcNow.AddDays(tipo.DuracionDias),
                        PrecioPagado = tipo.Precio,
                        Activa = true,
                        TenantId = entity.TenantId
                    };
                    _context.MembresiasSocios.Add(membresia);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            // ==============================================================
            // NUEVO: Notificación de bienvenida por WhatsApp
            // ==============================================================
            // Solo enviamos si el socio tiene teléfono registrado.
            // El DNI es el usuario para el portal del alumno (como vemos en PortalController)
            if (!string.IsNullOrEmpty(request.Telefono))
            {
                _ = EnviarBienvenidaAsync(entity);
            }

            return entity.Id;
        }

        /// <summary>
        /// Envía el mensaje de bienvenida en background.
        /// Si falla, el socio ya fue creado — la notificación es secundaria.
        /// </summary>
        private async Task EnviarBienvenidaAsync(Socio socio)
        {
            try
            {
                // Construimos el link al portal de alumnos usando la BaseUrl del appsettings
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://gymvo.app";
                var linkPortal = $"{baseUrl}/Portal/Login";

                await _notificationService.EnviarBienvenidaNuevoSocio(
                    nombreSocio: socio.Nombre,
                    telefono: socio.Telefono!,
                    dni: socio.Dni,
                    linkPortal: linkPortal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error enviando bienvenida por WhatsApp al socio {SocioId}",
                    socio.Id);
            }
        }
    }
}