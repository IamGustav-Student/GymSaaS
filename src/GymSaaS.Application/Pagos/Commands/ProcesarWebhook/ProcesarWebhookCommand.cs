// =============================================================================
// ARCHIVO: ProcesarWebhookCommand.cs
// CAPA: Application/Pagos/Commands/ProcesarWebhook
// PROPÓSITO: Procesa las notificaciones que envía Mercado Pago cuando un pago
//            cambia de estado (aprobado, rechazado, etc.).
//
// CAMBIOS EN ESTE ARCHIVO:
//   - Se inyecta INotificationService (NUEVO).
//   - En ProcesarPagoSocio(), cuando el pago es aprobado, se envía una
//     confirmación detallada con el nombre del plan y fecha de vencimiento (NUEVO).
//   - Toda la lógica original (detección de tipo de pago, procesamiento de
//     suscripción del gimnasio, procesamiento de pago de socio, stacking
//     de membresías) se conserva INTACTA.
// =============================================================================

using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Pagos.Commands.ProcesarWebhook
{
    public record ProcesarWebhookCommand(string PaymentId) : IRequest<bool>;

    public class ProcesarWebhookCommandHandler : IRequestHandler<ProcesarWebhookCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mpService;

        // NUEVO: Notificaciones y configuración para los links
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcesarWebhookCommandHandler> _logger;

        public ProcesarWebhookCommandHandler(
            IApplicationDbContext context,
            IMercadoPagoService mpService,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<ProcesarWebhookCommandHandler> logger)
        {
            _context = context;
            _mpService = mpService;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> Handle(ProcesarWebhookCommand request, CancellationToken cancellationToken)
        {
            // ==============================================================
            // LÓGICA ORIGINAL — CONSERVADA INTACTA
            // ==============================================================

            // 1. VERIFICACIÓN CRÍTICA CON MERCADOPAGO
            var estado = await _mpService.ObtenerEstadoPagoAsync(request.PaymentId);
            if (estado != "approved") return false;

            // 2. Recuperamos la referencia externa
            var externalRef = await _mpService.ObtenerExternalReferenceAsync(request.PaymentId);
            if (string.IsNullOrEmpty(externalRef)) return false;

            // DETECCIÓN DE TIPO DE PAGO (SaaS vs Socio)
            if (externalRef.StartsWith("SUBSCRIPTION|"))
            {
                return await ProcesarSuscripcionGimnasio(externalRef, cancellationToken);
            }

            // Si no tiene prefijo, asumimos que es un ID de MembresiaSocio
            if (int.TryParse(externalRef, out int membresiaId))
            {
                return await ProcesarPagoSocio(membresiaId, request.PaymentId, cancellationToken);
            }

            return false;
        }

        // LÓGICA ORIGINAL CONSERVADA: Procesamiento de suscripción del gimnasio a Gymvo
        private async Task<bool> ProcesarSuscripcionGimnasio(
            string externalRef,
            CancellationToken cancellationToken)
        {
            var parts = externalRef.Split('|');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int tenantId)) return false;

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant == null) return false;

            tenant.IsActive = true;
            tenant.Status = SubscriptionStatus.Active;
            tenant.SubscriptionEndsAt = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // LÓGICA ORIGINAL CONSERVADA + NOTIFICACIÓN NUEVA
        private async Task<bool> ProcesarPagoSocio(
            int membresiaId,
            string paymentId,
            CancellationToken cancellationToken)
        {
            // 3. Buscamos la membresía pendiente
            var membresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Include(m => m.TipoMembresia)
                .Include(m => m.Socio) // NUEVO: Incluimos Socio para los datos de notificación
                .FirstOrDefaultAsync(m => m.Id == membresiaId, cancellationToken);

            if (membresia == null) return false;

            // Si ya estaba activa, no hacemos nada (evita duplicar pagos)
            if (membresia.Activa) return true;

            // Verificar Stacking: si tenía otra membresía vigente, encadenamos
            var ultimaMembresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Where(m => m.SocioId == membresia.SocioId
                            && m.Activa
                            && m.Id != membresia.Id
                            && m.TenantId == membresia.TenantId)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync(cancellationToken);

            DateTime fechaInicio = DateTime.Now;
            if (ultimaMembresia != null && ultimaMembresia.FechaFin > DateTime.Now)
            {
                fechaInicio = ultimaMembresia.FechaFin;
            }

            membresia.FechaInicio = fechaInicio;
            membresia.FechaFin = fechaInicio.AddDays(membresia.TipoMembresia!.DuracionDias);
            membresia.Activa = true;

            // GUARDAR EN EL HISTORIAL DE PAGOS
            var nuevoPago = new Pago
            {
                SocioId = membresia.SocioId,
                MembresiaSocioId = membresia.Id,
                FechaPago = DateTime.Now,
                Monto = membresia.PrecioPagado,
                MetodoPago = "MercadoPago",
                TenantId = membresia.TenantId
            };

            _context.Pagos.Add(nuevoPago);
            await _context.SaveChangesAsync(cancellationToken);

            // ==============================================================
            // NUEVO: Notificación de confirmación de pago detallada
            // ==============================================================
            // Ahora que tenemos toda la info del plan y la fecha de vencimiento,
            // enviamos una confirmación mucho más completa que el método genérico
            if (membresia.Socio != null && !string.IsNullOrEmpty(membresia.Socio.Telefono))
            {
                _ = EnviarConfirmacionPagoAsync(membresia);
            }

            return true;
        }

        /// <summary>
        /// Envía la confirmación de pago con los detalles completos del plan.
        /// Se ejecuta en background para no retrasar el procesamiento del webhook.
        /// </summary>
        private async Task EnviarConfirmacionPagoAsync(MembresiaSocio membresia)
        {
            try
            {
                await _notificationService.EnviarConfirmacionPagoDetallada(
                    nombreSocio: membresia.Socio!.Nombre,
                    telefono: membresia.Socio.Telefono!,
                    nombrePlan: membresia.TipoMembresia?.Nombre ?? "Membresía",
                    monto: membresia.PrecioPagado,
                    fechaVencimiento: membresia.FechaFin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error enviando confirmación de pago al socio {SocioId}",
                    membresia.SocioId);
            }
        }
    }
}