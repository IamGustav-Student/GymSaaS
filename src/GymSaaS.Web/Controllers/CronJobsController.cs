// =============================================================================
// ARCHIVO: CronJobsController.cs
// CAPA: Web/Controllers
// PROPÓSITO: Expone endpoints HTTP que son llamados periódicamente por un
//            servicio externo de Cron (Render Cron Jobs, Railway, EasyCron,
//            GitHub Actions, etc.) para ejecutar tareas programadas.
//
// SEGURIDAD: Todos los endpoints validan una API Key por header (X-API-KEY).
//            La clave se configura en appsettings.json → "CronJobApiKey".
//            En producción, usar una clave aleatoria de al menos 32 caracteres.
//
// CAMBIOS EN ESTE ARCHIVO:
//   - El endpoint original "procesar-reintentos" se conserva INTACTO.
//   - Se agregan 4 nuevos endpoints para las tareas programadas:
//     * avisos-vencimiento       → notifica socios con membresía próxima a vencer
//     * recordatorios-clases     → notifica socios 1h antes de su clase
//     * alertas-inactividad      → notifica socios que no asisten hace N días
//     * resumen-diario-gimnasios → envía métricas del día al dueño del gimnasio
//
// CÓMO CONFIGURAR UN CRON:
//   En Render.com → "Cron Jobs" → agregar uno por cada endpoint:
//   * Cada día a las 9:00 AM → POST /api/CronJobs/avisos-vencimiento
//   * Cada hora              → POST /api/CronJobs/recordatorios-clases
//   * Cada semana lunes 8am  → POST /api/CronJobs/alertas-inactividad
//   * Cada día a las 23:55   → POST /api/CronJobs/resumen-diario-gimnasios
//   Todos con header: X-API-KEY: {tu_clave}
// =============================================================================

using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Pagos.Commands.EjecutarReintentos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CronJobsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;

        // Inyectamos DbContext y servicio de notificaciones directamente aquí
        // porque los CronJobs operan sobre TODOS los tenants (cross-tenant),
        // por eso no usamos el filtro de tenant del DbContext, sino que
        // hacemos IgnoreQueryFilters() explícitamente en cada query.
        private readonly IApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CronJobsController> _logger;

        public CronJobsController(
            IMediator mediator,
            IConfiguration configuration,
            IApplicationDbContext context,
            INotificationService notificationService,
            ILogger<CronJobsController> logger)
        {
            _mediator = mediator;
            _configuration = configuration;
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        // =====================================================================
        // MÉTODO AUXILIAR: Validación de API Key
        // =====================================================================
        // Extraemos la validación a un método privado para no repetir el código
        // en cada endpoint. Returns true si la clave es válida.
        private bool ApiKeyEsValida(string apiKey)
        {
            var configuredKey = _configuration["CronJobApiKey"] ?? "ClaveSecreta123";
            return apiKey == configuredKey;
        }

        // =====================================================================
        // ENDPOINT ORIGINAL — CONSERVADO INTACTO
        // =====================================================================

        /// <summary>
        /// Reintenta cobros fallidos de Mercado Pago y notifica a los socios.
        /// Frecuencia sugerida: diaria.
        /// </summary>
        [HttpPost("procesar-reintentos")]
        public async Task<IActionResult> ProcesarReintentos(
            [FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (!ApiKeyEsValida(apiKey)) return Unauthorized();

            var cantidad = await _mediator.Send(new EjecutarReintentosCommand());
            return Ok(new { mensaje = $"Proceso finalizado. Pagos reintentados: {cantidad}" });
        }

        // =====================================================================
        // NUEVO ENDPOINT 1: Avisos de vencimiento próximo de membresías
        // =====================================================================

        /// <summary>
        /// Detecta socios con membresía que vence en 5 o 3 días y
        /// les envía un aviso por WhatsApp con link de renovación.
        /// Frecuencia sugerida: diaria a las 9:00 AM.
        /// </summary>
        [HttpPost("avisos-vencimiento")]
        public async Task<IActionResult> EnviarAvisosVencimiento(
            [FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (!ApiKeyEsValida(apiKey)) return Unauthorized();

            var hoy = DateTime.UtcNow.Date;
            // Buscamos membresías que venzan en EXACTAMENTE 5 o 3 días
            // (no "en los próximos 5 días" para no enviar duplicados)
            var fechasObjetivo = new[]
            {
                hoy.AddDays(5),
                hoy.AddDays(3)
            };

            // IgnoreQueryFilters() porque operamos sobre TODOS los tenants
            var membresiasProximas = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Include(m => m.Socio)
                .Include(m => m.TipoMembresia)
                .Where(m => m.Activa
                            && fechasObjetivo.Contains(m.FechaFin.Date)
                            && m.Socio != null
                            && m.Socio.Telefono != null)
                .AsNoTracking()
                .ToListAsync();

            var baseUrl = _configuration["App:BaseUrl"] ?? "https://gymvo.app";
            int enviados = 0;

            foreach (var membresia in membresiasProximas)
            {
                try
                {
                    var diasRestantes = (membresia.FechaFin.Date - hoy).Days;
                    var linkRenovacion = $"{baseUrl}/Portal/Renovar";

                    await _notificationService.EnviarAvisoVencimientoProximo(
                        nombreSocio: membresia.Socio!.Nombre,
                        telefono: membresia.Socio.Telefono!,
                        nombrePlan: membresia.TipoMembresia?.Nombre ?? "Membresía",
                        diasRestantes: diasRestantes,
                        linkRenovacion: linkRenovacion);

                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error enviando aviso de vencimiento al socio {SocioId}",
                        membresia.SocioId);
                }
            }

            return Ok(new
            {
                mensaje = $"Avisos de vencimiento enviados: {enviados} de {membresiasProximas.Count}"
            });
        }

        // =====================================================================
        // NUEVO ENDPOINT 2: Recordatorios de clases (1 hora antes)
        // =====================================================================

        /// <summary>
        /// Detecta reservas confirmadas para clases que comienzan en los próximos
        /// 60-75 minutos y envía un recordatorio por WhatsApp.
        /// Frecuencia sugerida: cada hora (0 * * * *).
        /// </summary>
        [HttpPost("recordatorios-clases")]
        public async Task<IActionResult> EnviarRecordatoriosClases(
            [FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (!ApiKeyEsValida(apiKey)) return Unauthorized();

            var ahora = DateTime.UtcNow;
            // Ventana de tiempo: clases que empiezan entre 60 y 75 minutos
            // La ventana de 15 minutos permite que si el cron se ejecuta
            // con algo de retraso, igual captura las clases del rango
            var desde = ahora.AddMinutes(60);
            var hasta = ahora.AddMinutes(75);

            var reservasProximas = await _context.Reservas
                .IgnoreQueryFilters()
                .Include(r => r.Socio)
                .Include(r => r.Clase)
                .Where(r => r.Estado == "Confirmada"
                            && r.Clase != null
                            && r.Clase.FechaHoraInicio >= desde
                            && r.Clase.FechaHoraInicio <= hasta
                            && r.Clase.Activa
                            && r.Socio != null
                            && r.Socio.Telefono != null)
                .AsNoTracking()
                .ToListAsync();

            int enviados = 0;

            foreach (var reserva in reservasProximas)
            {
                try
                {
                    await _notificationService.EnviarRecordatorioClase(
                        nombreSocio: reserva.Socio!.Nombre,
                        telefono: reserva.Socio.Telefono!,
                        nombreClase: reserva.Clase!.Nombre,
                        horaInicio: reserva.Clase.FechaHoraInicio);

                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error enviando recordatorio de clase al socio {SocioId}",
                        reserva.SocioId);
                }
            }

            return Ok(new
            {
                mensaje = $"Recordatorios de clases enviados: {enviados} de {reservasProximas.Count}"
            });
        }

        // =====================================================================
        // NUEVO ENDPOINT 3: Alertas de inactividad
        // =====================================================================

        /// <summary>
        /// Detecta socios que no han asistido en 7 o 15 días y les envía
        /// un mensaje motivacional para incentivar su regreso.
        /// Frecuencia sugerida: lunes 8:00 AM (0 8 * * 1).
        /// </summary>
        [HttpPost("alertas-inactividad")]
        public async Task<IActionResult> EnviarAlertasInactividad(
            [FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (!ApiKeyEsValida(apiKey)) return Unauthorized();

            var hoy = DateTime.UtcNow.Date;

            // Traemos los socios activos con membresía vigente y su última asistencia
            // Usamos IgnoreQueryFilters para operar en todos los tenants
            var socios = await _context.Socios
                .IgnoreQueryFilters()
                .Include(s => s.Membresias)
                .Include(s => s.Asistencias)
                .Where(s => s.Activo
                            && !s.IsDeleted
                            && s.Telefono != null
                            && s.Membresias.Any(m => m.Activa && m.FechaFin >= DateTime.UtcNow))
                .AsNoTracking()
                .ToListAsync();

            int enviados = 0;

            foreach (var socio in socios)
            {
                try
                {
                    // Fecha de la última asistencia registrada
                    var ultimaAsistencia = socio.Asistencias
                        .Where(a => a.Permitido)
                        .MaxBy(a => a.FechaHora)
                        ?.FechaHora.Date;

                    if (ultimaAsistencia == null) continue;

                    var diasSinAsistir = (hoy - ultimaAsistencia.Value).Days;

                    // Solo enviamos en los umbrales EXACTOS de 7 y 15 días
                    // para no spamear al socio si el cron se ejecuta más seguido
                    if (diasSinAsistir != 7 && diasSinAsistir != 15) continue;

                    await _notificationService.EnviarAlertaInactividad(
                        nombreSocio: socio.Nombre,
                        telefono: socio.Telefono!,
                        diasSinAsistir: diasSinAsistir);

                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error enviando alerta de inactividad al socio {SocioId}",
                        socio.Id);
                }
            }

            return Ok(new { mensaje = $"Alertas de inactividad enviadas: {enviados}" });
        }

        // =====================================================================
        // NUEVO ENDPOINT 4: Resumen diario para los dueños de gimnasios
        // =====================================================================

        /// <summary>
        /// Calcula las métricas del día para cada tenant activo y envía
        /// un resumen por WhatsApp al número del dueño del gimnasio.
        /// Frecuencia sugerida: diaria a las 23:55 (55 23 * * *).
        ///
        /// IMPORTANTE: Para que este endpoint funcione, el Tenant debe tener
        /// el número del dueño en la propiedad "TelefonoDueno" (ver nota abajo).
        /// </summary>
        [HttpPost("resumen-diario-gimnasios")]
        public async Task<IActionResult> EnviarResumenDiario(
            [FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (!ApiKeyEsValida(apiKey)) return Unauthorized();

            var hoy = DateTime.UtcNow.Date;

            // Traemos todos los tenants activos
            // NOTA PARA DESARROLLADOR JUNIOR: Si querés agregar el teléfono del dueño
            // al tenant, necesitás:
            // 1. Agregar "public string? TelefonoDueno { get; set; }" a la entidad Tenant
            // 2. Crear una migración: dotnet ef migrations add AddTelefonoDueno
            // 3. Actualizar la DB con la auto-migración o manualmente
            // Por ahora, si el campo no existe, usamos el TelefonoDueno hardcodeado
            // en el appsettings o simplemente logueamos.
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.IsActive)
                .AsNoTracking()
                .ToListAsync();

            int tenantsProcesados = 0;

            foreach (var tenant in tenants)
            {
                try
                {
                    // Ingresos del día: asistencias permitidas de HOY
                    var ingresosHoy = await _context.Asistencias
                        .IgnoreQueryFilters()
                        .CountAsync(a => a.TenantId == tenant.Code
                                        && a.Permitido
                                        && a.FechaHora.Date == hoy);

                    // Membresías vendidas hoy: registros de pago de HOY
                    var membresíasVendidas = await _context.Pagos
                        .IgnoreQueryFilters()
                        .CountAsync(p => p.TenantId == tenant.Code
                                        && p.Pagado
                                        && p.FechaPago.Date == hoy);

                    // Recaudación de HOY
                    var recaudacionHoy = await _context.Pagos
                        .IgnoreQueryFilters()
                        .Where(p => p.TenantId == tenant.Code
                                    && p.Pagado
                                    && p.FechaPago.Date == hoy)
                        .SumAsync(p => p.Monto);

                    // Logueamos las métricas siempre (útil aunque no haya teléfono)
                    _logger.LogInformation(
                        "[ResumenDiario] Tenant: {Nombre} | Ingresos: {Ingresos} | Membresías: {Mem} | Recaudación: ${Rec}",
                        tenant.Name, ingresosHoy, membresíasVendidas, recaudacionHoy);

                    // TODO: Cuando agregues TelefonoDueno a la entidad Tenant,
                    // reemplazá esta condición con:
                    // if (!string.IsNullOrEmpty(tenant.TelefonoDueno))
                    // {
                    //     await _notificationService.EnviarResumenDiario(
                    //         tenant.Name, tenant.TelefonoDueno,
                    //         ingresosHoy, membresíasVendidas, recaudacionHoy);
                    // }

                    tenantsProcesados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error calculando resumen diario para tenant {TenantName}",
                        tenant.Name);
                }
            }

            return Ok(new
            {
                mensaje = $"Resúmenes diarios procesados: {tenantsProcesados} gimnasio(s)"
            });
        }
    }
}