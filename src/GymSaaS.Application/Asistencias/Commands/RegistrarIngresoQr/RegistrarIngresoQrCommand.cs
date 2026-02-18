using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr
{
    // Esta clase es la respuesta que devolverá el servidor
    public class IngresoQrResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string NombreSocio { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
    }

    // El comando ahora acepta opcionalmente un CheckInCode del gimnasio
    public record RegistrarIngresoQrCommand : IRequest<IngresoQrResult>
    {
        public int? SocioId { get; init; } // Usado cuando el gimnasio escanea al socio
        public string? TenantCheckInCode { get; init; } // Usado cuando el socio escanea al gimnasio
        public string CodigoQrEscaneado { get; init; } = string.Empty;
        public double LatitudUsuario { get; init; }
        public double LongitudUsuario { get; init; }
    }

    public class RegistrarIngresoQrCommandHandler : IRequestHandler<RegistrarIngresoQrCommand, IngresoQrResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public RegistrarIngresoQrCommandHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<IngresoQrResult> Handle(RegistrarIngresoQrCommand request, CancellationToken cancellationToken)
        {
            Socio? socio = null;

            // Lógica Nueva: Identificar al socio según el método de escaneo
            if (!string.IsNullOrEmpty(request.TenantCheckInCode))
            {
                // MODO AUTO-CHECKIN: El socio escaneó el QR del gimnasio
                // EXPLICACIÓN DEL ERROR: _currentTenantService.TenantId devuelve un string.
                // Si el Id de tu entidad Tenant es Guid o Int, debemos comparar correctamente.
                var tenant = await _context.Tenants
                    .IgnoreQueryFilters() // Ignoramos filtros para validar la existencia real del gimnasio
                    .FirstOrDefaultAsync(t => t.Id.ToString() == _currentTenantService.TenantId, cancellationToken);

                if (tenant == null || tenant.Code != request.TenantCheckInCode)
                {
                    return Fail("Código de gimnasio inválido o no pertenece a esta sede.");
                }

                // Buscamos al socio por el SocioId que viene del portal (usuario logueado)
                socio = await _context.Socios.FindAsync(request.SocioId);
            }
            else
            {
                // MODO MONITOR: El gimnasio escaneó el QR del socio
                socio = await _context.Socios.FindAsync(request.SocioId);
            }

            if (socio == null) return Fail("Socio no encontrado.");

            // --- Lógica Existente (Mantenida intacta) ---
            bool accesoPermitido = true;
            string motivoRechazo = "";

            // Validación de Membresía
            var membresia = await _context.MembresiasSocios
                .Where(m => m.SocioId == socio.Id && m.Activa)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync(cancellationToken);

            if (membresia == null || membresia.FechaFin < DateTime.UtcNow)
            {
                accesoPermitido = false;
                motivoRechazo = "Sin membresía activa o vencida.";
            }

            // Registro en la base de datos (Asistencia)
            var asistencia = new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = DateTime.UtcNow, // Asegúrate que el nombre de columna en tu entidad sea este
                Permitido = accesoPermitido,
                Tipo = string.IsNullOrEmpty(request.TenantCheckInCode) ? "Escaneo_Gimnasio" : "Auto_CheckIn"
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync(cancellationToken);

            if (!accesoPermitido)
            {
                return Fail(motivoRechazo, socio.Nombre, socio.FotoUrl);
            }

            return new IngresoQrResult
            {
                Exitoso = true,
                Mensaje = "¡Bienvenido! Entrenamiento registrado.",
                NombreSocio = socio.Nombre,
                FotoUrl = socio.FotoUrl
            };
        }

        private IngresoQrResult Fail(string msg, string nombre = "", string? foto = null)
            => new IngresoQrResult { Exitoso = false, Mensaje = msg, NombreSocio = nombre, FotoUrl = foto };
    }
}