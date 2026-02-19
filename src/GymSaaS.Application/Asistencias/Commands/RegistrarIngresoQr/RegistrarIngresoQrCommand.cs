// =============================================================================
// ARCHIVO: RegistrarIngresoQrCommand.cs
// CAPA: Application/Asistencias/Commands/RegistrarIngresoQr
// PROPÓSITO: Procesa el escaneo QR del socio para registrar su ingreso.
//            Valida membresía activa, días permitidos y geolocalización.
//
// CAMBIOS EN ESTE ARCHIVO:
//   - Se inyecta INotificationService (NUEVO).
//   - Cuando el acceso ES DENEGADO por membresía vencida, se envía
//     notificación al celular del socio con link de renovación (NUEVO).
//   - Cuando el acceso ES PERMITIDO, se verifica si el socio alcanzó
//     un hito de asistencias (10, 50, 100...) para enviar felicitación
//     gamificada por WhatsApp (NUEVO).
//   - Toda la lógica original (Haversine, QR, zonas horarias, días de
//     acceso por membresía, deduplicación) se conserva INTACTA.
// =============================================================================

using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr
{
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

    public class RegistrarIngresoQrCommandHandler
        : IRequestHandler<RegistrarIngresoQrCommand, IngresoQrResult>
    {
        private readonly IApplicationDbContext _context;

        // NUEVO: Notificaciones para acceso denegado y logros de gamificación
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrarIngresoQrCommandHandler> _logger;

        // Hitos de asistencia que disparan una felicitación (múltiplos clave)
        // El socio recibe WhatsApp cuando llega exactamente a uno de estos números
        private static readonly HashSet<int> HitosGamificacion = new() { 10, 25, 50, 100, 200, 500 };

        public RegistrarIngresoQrCommandHandler(
            IApplicationDbContext context,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<RegistrarIngresoQrCommandHandler> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IngresoQrResult> Handle(
            RegistrarIngresoQrCommand request,
            CancellationToken cancellationToken)
        {
            // ==============================================================
            // LÓGICA ORIGINAL — CONSERVADA INTACTA
            // ==============================================================

            var socio = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) return Fail("Socio no encontrado");

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Code == socio.TenantId, cancellationToken);

            if (tenant == null) return Fail("Error crítico: Gimnasio no configurado");

            var timeZoneId = tenant.TimeZoneId ?? "Argentina Standard Time";
            TimeZoneInfo timeZone;
            try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch { timeZone = TimeZoneInfo.Local; }
            var horaGimnasio = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var ultimoIngreso = await _context.Asistencias
                .Where(a => a.SocioId == socio.Id && a.FechaHora > horaGimnasio.AddMinutes(-2))
                .OrderByDescending(a => a.FechaHora)
                .FirstOrDefaultAsync(cancellationToken);

            if (ultimoIngreso != null && ultimoIngreso.Permitido)
            {
                return new IngresoQrResult
                {
                    Exitoso = true,
                    Mensaje = "Acceso ya registrado (Pase libre).",
                    NombreSocio = socio.Nombre,
                    FotoUrl = socio.FotoUrl
                };
            }

            bool accesoPermitido = true;
            string motivoRechazo = string.Empty;
            bool rechazadoPorMembresia = false; // NUEVO: para saber si enviar link de renovación

            if (!string.IsNullOrEmpty(request.CodigoQrEscaneado) &&
                tenant.CodigoQrGym != request.CodigoQrEscaneado)
            {
                accesoPermitido = false;
                motivoRechazo = "Código QR inválido o de otra sucursal.";
            }

            if (accesoPermitido &&
                tenant.Latitud.HasValue &&
                tenant.Longitud.HasValue &&
                request.LatitudUsuario != 0)
            {
                var dist = CalcularDistanciaHaversine(
                    request.LatitudUsuario, request.LongitudUsuario,
                    tenant.Latitud.Value, tenant.Longitud.Value);

                if (dist > tenant.RadioPermitidoMetros)
                {
                    accesoPermitido = false;
                    motivoRechazo = $"Estás demasiado lejos ({dist:N0}m). Acércate al gimnasio.";
                }
            }

            if (accesoPermitido)
            {
                var membresiaActiva = socio.Membresias
                    .FirstOrDefault(m => m.Activa && m.FechaFin.Date >= horaGimnasio.Date);

                if (membresiaActiva == null)
                {
                    accesoPermitido = false;
                    rechazadoPorMembresia = true; // NUEVO: marcamos el motivo específico
                    motivoRechazo = "Membresía vencida o inexistente.";
                }
                else if (membresiaActiva.TipoMembresia == null)
                {
                    accesoPermitido = false;
                    motivoRechazo = "Error en tipo de membresía.";
                }
                else
                {
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

            // ==============================================================
            // NUEVO: Notificación de acceso DENEGADO por membresía vencida
            // ==============================================================
            if (!accesoPermitido && rechazadoPorMembresia && !string.IsNullOrEmpty(socio.Telefono))
            {
                _ = EnviarNotificacionAccesoDenegadoAsync(socio);
            }

            if (!accesoPermitido)
            {
                return Fail(motivoRechazo, socio.Nombre, socio.FotoUrl);
            }

            // ==============================================================
            // NUEVO: Verificación de hito de gamificación al ingreso exitoso
            // ==============================================================
            // Contamos TODAS las asistencias del socio (incluyendo la que acaba de registrarse)
            _ = VerificarYEnviarLogroGamificacionAsync(socio);

            return new IngresoQrResult
            {
                Exitoso = true,
                Mensaje = "¡Bienvenido! Que tengas un buen entrenamiento.",
                NombreSocio = socio.Nombre,
                FotoUrl = socio.FotoUrl
            };
        }

        /// <summary>
        /// Envía el link de renovación cuando el acceso es denegado por membresía vencida.
        /// Así el socio puede renovar desde su celular sin pasar por recepción.
        /// </summary>
        private async Task EnviarNotificacionAccesoDenegadoAsync(Socio socio)
        {
            try
            {
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://gymvo.app";
                var linkRenovacion = $"{baseUrl}/Portal/Renovar";

                await _notificationService.EnviarNotificacionAccesoDenegado(
                    nombreSocio: socio.Nombre,
                    telefono: socio.Telefono!,
                    linkRenovacion: linkRenovacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error enviando notificación de acceso denegado al socio {SocioId}",
                    socio.Id);
            }
        }

        /// <summary>
        /// Cuenta las asistencias totales del socio y, si llegó a un hito
        /// (10, 25, 50, 100...), le manda una felicitación por WhatsApp.
        /// Integra la gamificación con las notificaciones.
        /// </summary>
        private async Task VerificarYEnviarLogroGamificacionAsync(Socio socio)
        {
            try
            {
                if (string.IsNullOrEmpty(socio.Telefono)) return;

                // Contamos el total de asistencias aprobadas del socio
                var totalAsistencias = await _context.Asistencias
                    .IgnoreQueryFilters()
                    .CountAsync(a => a.SocioId == socio.Id && a.Permitido);

                // Solo enviamos si el total actual es exactamente un hito
                if (!HitosGamificacion.Contains(totalAsistencias)) return;

                // Determinamos el nivel según la misma lógica de GetGamificationStatsQuery
                string nivel = totalAsistencias switch
                {
                    >= 500 => "Leyenda",
                    >= 100 => "Elite",
                    >= 50 => "Avanzado",
                    >= 10 => "Intermedio",
                    _ => "Novato"
                };

                await _notificationService.EnviarFelicitacionLogro(
                    nombreSocio: socio.Nombre,
                    telefono: socio.Telefono,
                    totalAsistencias: totalAsistencias,
                    nivelActual: nivel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error verificando logro de gamificación para socio {SocioId}",
                    socio.Id);
            }
        }

        // Método auxiliar original — CONSERVADO INTACTO
        private IngresoQrResult Fail(string msg, string nombre = "", string? foto = null)
            => new IngresoQrResult { Exitoso = false, Mensaje = msg, NombreSocio = nombre, FotoUrl = foto };

        // Algoritmo Haversine original — CONSERVADO INTACTO
        private double CalcularDistanciaHaversine(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            var R = 6371e3;
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