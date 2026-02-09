using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        public CreateSocioCommandHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<int> Handle(CreateSocioCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentTenantService.TenantId;

            // =================================================================================
            // 🛡️ GATEKEEPER: VALIDACIÓN DE LÍMITES DEL PLAN (SAAS)
            // =================================================================================
            if (!string.IsNullOrEmpty(tenantId))
            {
                // 1. Obtener la configuración del Tenant actual (sin trackear para velocidad)
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == tenantId, cancellationToken);

                // 2. Verificar si tiene un límite numérico establecido
                if (tenant != null && tenant.MaxSocios.HasValue)
                {
                    // 3. Contar socios activos actuales en este gimnasio
                    var cantidadSociosActuales = await _context.Socios
                        .CountAsync(s => s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

                    // 4. Bloquear si supera o iguala el límite
                    if (cantidadSociosActuales >= tenant.MaxSocios.Value)
                    {
                        throw new InvalidOperationException(
                            $"Has alcanzado el límite de {tenant.MaxSocios} socios permitidos por tu Plan {tenant.Plan}. " +
                            "¡Tu gimnasio está creciendo! Actualiza a PREMIUM para eliminar los límites y seguir sumando alumnos.");
                    }
                }
            }
            // =================================================================================

            // Lógica original de creación (Intacta)
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

            // Asignación automática de membresía si se seleccionó una
            if (request.TipoMembresiaId > 0)
            {
                var tipo = await _context.TiposMembresia.FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);
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

            return entity.Id;
        }
    }
}