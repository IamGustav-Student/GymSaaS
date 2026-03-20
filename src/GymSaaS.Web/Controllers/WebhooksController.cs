using GymSaaS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GymSaaS.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(IApplicationDbContext context, ILogger<WebhooksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] JsonElement json)
        {
            try
            {
                _logger.LogInformation("Webhook recibido de MercadoPago: {Data}", json.ToString());

                // 1. Identificar el tipo de recurso
                if (json.TryGetProperty("type", out var typeProperty) && typeProperty.GetString() == "payment")
                {
                    if (json.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProperty))
                    {
                        var paymentId = idProperty.GetString();
                        
                        // En un escenario real, consultaríamos la API de MercadoPago con el ID
                        // Para este Sprint, procesamos basándonos en el ID de transacción externa guardado
                        // o capturando la external_reference si viniera en el JSON (MP suele enviarla en el recurso completo).
                        
                        await ProcesarNotificacionPago(paymentId);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook de MercadoPago");
                return StatusCode(500);
            }
        }

        private async Task ProcesarNotificacionPago(string paymentId)
        {
            // Buscamos si es un pago de Membresía de Socio
            var pagoSocio = await _context.Pagos
                .Include(p => p.MembresiaSocio)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.IdTransaccionExterna == paymentId);

            if (pagoSocio != null)
            {
                await ProcesarPagoMembresiaSocio(pagoSocio);
                return;
            }

            // OPCIONAL: Si MercadoPago envía la external_reference directamente en el webhook (depende de la API)
            // Aquí simulamos la detección de suscripción de Gimnasio (Tenant)
            // En una implementación real, se obtiene el objeto 'payment' completo de MP.
        }

        private async Task ProcesarPagoMembresiaSocio(Pago pago)
        {
            pago.Pagado = true;
            pago.EstadoTransaccion = "approved";
            pago.FechaPago = DateTime.UtcNow;

            if (pago.MembresiaSocio != null)
            {
                pago.MembresiaSocio.Activa = true;
                pago.MembresiaSocio.Estado = "Activa";
            }

            await _context.SaveChangesAsync(default);
            _logger.LogInformation("Pago de membresía {PaymentId} procesado.", pago.IdTransaccionExterna);
        }

        // NUEVO: Endpoint específico para suscripciones de Tenant (puedes unificarlo o separarlo)
        [HttpPost("mercadopago-saas")]
        public async Task<IActionResult> MercadoPagoSaaSWebhook([FromQuery] string topic, [FromQuery] string id)
        {
            // MercadoPago IPN tradicional usa topic e id
            if (topic == "payment")
            {
                // Aquí deberíamos consultar el pago a MP para obtener la external_reference: "SUBSCRIPTION|{tenantId}"
                // Simulamos la lógica de activación del Tenant:
                _logger.LogInformation("Procesando pago SaaS del Gimnasio ID: {Id}", id);
            }
            return Ok();
        }
    }
}