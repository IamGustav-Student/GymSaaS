using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr
{
    // DTO de resultado para devolver info al Front (y futuro SignalR)
    public class IngresoQrResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string NombreSocio { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
    }

    public record RegistrarIngresoQrCommand : IRequest<IngresoQrResult>
    {
        public int SocioId { get; init; }
        public string CodigoQrEscaneado { get; init; } = string.Empty;
        public double LatitudUsuario { get; init; }
        public double LongitudUsuario { get; init; }
    }

    public class RegistrarIngresoQrCommandHandler : IRequestHandler<RegistrarIngresoQrCommand, IngresoQrResult>
    {
        private readonly IApplicationDbContext _context;

        public RegistrarIngresoQrCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IngresoQrResult> Handle(RegistrarIngresoQrCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener Socio y sus Membresías
            var socio = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) return Fail("Socio no encontrado");

            // 2. Obtener Tenant (Gimnasio) para verificar QR y Ubicación
            // Asumimos que el Socio ya tiene el TenantId cargado. 
            // Buscamos el Tenant correspondiente a este socio.
            // Nota: En multitenancy real, el QR escaneado DEBE coincidir con el Tenant del socio.
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Code == socio.TenantId, cancellationToken);

            if (tenant == null) return Fail("Gimnasio no identificado");

            // 3. VALIDACIÓN 1: Código QR Correcto
            if (tenant.CodigoQrGym != request.CodigoQrEscaneado)
            {
                return Fail("El código QR no pertenece a este gimnasio.");
            }

            // 4. VALIDACIÓN 2: Geolocalización (Geofencing)
            if (tenant.Latitud.HasValue && tenant.Longitud.HasValue)
            {
                var distanciaMetros = CalcularDistanciaHaversine(
                    request.LatitudUsuario, request.LongitudUsuario,
                    tenant.Latitud.Value, tenant.Longitud.Value
                );

                if (distanciaMetros > tenant.RadioPermitidoMetros)
                {
                    return Fail($"Estás demasiado lejos del gimnasio ({distanciaMetros:N0}m). Acércate a la entrada.");
                }
            }
            // Si el tenant no tiene coordenadas configuradas, saltamos esta validación (modo permisivo)

            // 5. VALIDACIÓN 3: Membresía Activa y Días Permitidos
            // (Reutilizamos lógica existente o simplificamos para este MVP)
            var membresiaActiva = socio.Membresias.FirstOrDefault(m => m.Activa && m.FechaFin >= DateTime.Now);

            if (membresiaActiva == null)
            {
                return Fail("No tienes una membresía activa o vigente.");
            }

            // Validación de Días (Lógica Parte 2 que implementamos antes)
            var hoy = DateTime.Now.DayOfWeek;
            bool diaPermitido = hoy switch
            {
                DayOfWeek.Monday => membresiaActiva.TipoMembresia.AccesoLunes,
                DayOfWeek.Tuesday => membresiaActiva.TipoMembresia.AccesoMartes,
                DayOfWeek.Wednesday => membresiaActiva.TipoMembresia.AccesoMiercoles,
                DayOfWeek.Thursday => membresiaActiva.TipoMembresia.AccesoJueves,
                DayOfWeek.Friday => membresiaActiva.TipoMembresia.AccesoViernes,
                DayOfWeek.Saturday => membresiaActiva.TipoMembresia.AccesoSabado,
                DayOfWeek.Sunday => membresiaActiva.TipoMembresia.AccesoDomingo,
                _ => false
            };

            if (!diaPermitido)
            {
                return Fail($"Tu plan no permite ingresar los {DateTime.Now.ToString("dddd")}.");
            }

            // 6. ÉXITO: Registrar Asistencia
            var asistencia = new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = DateTime.Now,
                Tipo = "QR_Self_CheckIn",
                TenantId = socio.TenantId
            };
            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync(cancellationToken);

            return new IngresoQrResult
            {
                Exitoso = true,
                Mensaje = "¡Bienvenido! Acceso autorizado.",
                NombreSocio = socio.Nombre,
                FotoUrl = socio.FotoUrl
            };
        }

        private IngresoQrResult Fail(string msg) => new IngresoQrResult { Exitoso = false, Mensaje = msg };

        // Fórmula matemática para calcular distancia entre dos puntos GPS
        private double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // Radio de la tierra en metros
            var phi1 = lat1 * Math.PI / 180;
            var phi2 = lat2 * Math.PI / 180;
            var deltaPhi = (lat2 - lat1) * Math.PI / 180;
            var deltaLambda = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Resultado en metros
        }
    }
}