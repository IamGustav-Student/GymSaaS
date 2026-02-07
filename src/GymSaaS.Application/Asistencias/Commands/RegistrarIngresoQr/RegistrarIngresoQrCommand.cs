using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr
{
    // DTO de resultado
    public class IngresoQrResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string NombreSocio { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
    }

    // Comando
    public record RegistrarIngresoQrCommand : IRequest<IngresoQrResult>
    {
        public int SocioId { get; init; }
        public string CodigoQrEscaneado { get; init; } = string.Empty;
        public double LatitudUsuario { get; init; }
        public double LongitudUsuario { get; init; }
    }

    // Handler Blindado
    public class RegistrarIngresoQrCommandHandler : IRequestHandler<RegistrarIngresoQrCommand, IngresoQrResult>
    {
        private readonly IApplicationDbContext _context;

        public RegistrarIngresoQrCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IngresoQrResult> Handle(RegistrarIngresoQrCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener Socio y dependencias (Membresías + Tipo)
            var socio = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) return Fail("Socio no encontrado");

            // 2. Obtener Configuración del Tenant (Para Geo y Timezone)
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Code == socio.TenantId, cancellationToken);

            if (tenant == null) return Fail("Error crítico: Gimnasio no configurado");

            // 3. DEFINICIÓN DE TIEMPO LOCAL (Corrección Timezone)
            var timeZoneId = tenant.TimeZoneId ?? "Argentina Standard Time";
            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                timeZone = TimeZoneInfo.Local; // Fallback seguro
            }
            var horaGimnasio = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // 4. CHECK ANTI-PASSBACK (Evitar doble fichaje en 2 minutos)
            var ultimoIngreso = await _context.Asistencias
                .Where(a => a.SocioId == socio.Id && a.FechaHora > horaGimnasio.AddMinutes(-2))
                .OrderByDescending(a => a.FechaHora)
                .FirstOrDefaultAsync(cancellationToken);

            if (ultimoIngreso != null && ultimoIngreso.Permitido)
            {
                return new IngresoQrResult
                {
                    Exitoso = true, // Devolvemos true para no alarmar al usuario, pero no re-registramos
                    Mensaje = "Acceso ya registrado (Pase libre).",
                    NombreSocio = socio.Nombre,
                    FotoUrl = socio.FotoUrl
                };
            }

            // 5. VALIDACIONES SECUENCIALES
            bool accesoPermitido = true;
            string motivoRechazo = string.Empty;

            // A. Validación QR (Si aplica)
            if (!string.IsNullOrEmpty(request.CodigoQrEscaneado) && tenant.CodigoQrGym != request.CodigoQrEscaneado)
            {
                accesoPermitido = false;
                motivoRechazo = "Código QR inválido o de otra sucursal.";
            }

            // B. Validación Geográfica (Si aplica y pasó el QR)
            if (accesoPermitido && tenant.Latitud.HasValue && tenant.Longitud.HasValue && request.LatitudUsuario != 0)
            {
                var dist = CalcularDistanciaHaversine(request.LatitudUsuario, request.LongitudUsuario, tenant.Latitud.Value, tenant.Longitud.Value);
                if (dist > tenant.RadioPermitidoMetros)
                {
                    accesoPermitido = false;
                    motivoRechazo = $"Estás demasiado lejos ({dist:N0}m). Acércate al gimnasio.";
                }
            }

            // C. Validación Membresía
            if (accesoPermitido)
            {
                var membresiaActiva = socio.Membresias
                    .FirstOrDefault(m => m.Activa && m.FechaFin.Date >= horaGimnasio.Date);

                if (membresiaActiva == null)
                {
                    accesoPermitido = false;
                    motivoRechazo = "Membresía vencida o inexistente.";
                }
                else
                {
                    // Validar día de la semana permitido
                    var diaActual = horaGimnasio.DayOfWeek;
                    bool diaValido = diaActual switch
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

                    if (!diaValido)
                    {
                        accesoPermitido = false;
                        motivoRechazo = $"Tu plan no permite acceso los {horaGimnasio.ToString("dddd")}.";
                    }
                }
            }

            // 6. PERSISTENCIA (Guardamos SIEMPRE, incluso si rebota)
            var asistencia = new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = horaGimnasio,
                Permitido = accesoPermitido,
                Detalle = accesoPermitido ? "Acceso Correcto" : motivoRechazo,
                TenantId = socio.TenantId,
                Tipo = string.IsNullOrEmpty(request.CodigoQrEscaneado) ? "Manual_ID" : "QR_App"
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
                Mensaje = "¡Bienvenido! Que tengas un buen entrenamiento.",
                NombreSocio = socio.Nombre,
                FotoUrl = socio.FotoUrl
            };
        }

        private IngresoQrResult Fail(string msg, string nombre = "", string? foto = null)
            => new IngresoQrResult { Exitoso = false, Mensaje = msg, NombreSocio = nombre, FotoUrl = foto };

        private double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // Radio tierra (metros)
            var phi1 = lat1 * Math.PI / 180;
            var phi2 = lat2 * Math.PI / 180;
            var deltaPhi = (lat2 - lat1) * Math.PI / 180;
            var deltaLambda = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }
}