using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GymSaaS.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IApplicationDbContext context,
            IMercadoPagoService mercadoPagoService,
            IConfiguration configuration,
            ILogger<WebhooksController> logger)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] JsonElement json)
        {
            try
            {
                var dataId = ExtraerDataId(json);
                if (!EsFirmaValida(dataId))
                {
                    _logger.LogWarning("Webhook de MercadoPago rechazado: firma inválida.");
                    return Unauthorized();
                }

                _logger.LogInformation("Webhook recibido de MercadoPago: {Data}", json.ToString());

                if (json.TryGetProperty("type", out var typeProperty) && typeProperty.GetString() == "payment")
                {
                    if (!string.IsNullOrEmpty(dataId))
                    {
                        await ProcesarNotificacionPago(dataId);
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

            // Si no es un pago de socio, puede ser una suscripción SaaS del Tenant.
            var externalReference = await _mercadoPagoService.ObtenerExternalReferenceAsync(paymentId);
            if (string.IsNullOrEmpty(externalReference) || !externalReference.StartsWith("SUBSCRIPTION|"))
            {
                _logger.LogWarning("Webhook de pago {PaymentId} no coincide con ningún pago de socio ni referencia de suscripción.", paymentId);
                return;
            }

            var estado = await _mercadoPagoService.ObtenerEstadoPagoAsync(paymentId);
            await ActivarSuscripcionTenant(externalReference, paymentId, estado);
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

        // Endpoint IPN clásico para suscripciones de Tenant (topic/id por query string)
        [HttpPost("mercadopago-saas")]
        public async Task<IActionResult> MercadoPagoSaaSWebhook([FromQuery] string topic, [FromQuery] string id)
        {
            if (!EsFirmaValida(id))
            {
                _logger.LogWarning("Webhook SaaS de MercadoPago rechazado: firma inválida.");
                return Unauthorized();
            }

            if (topic == "payment" && !string.IsNullOrEmpty(id))
            {
                var externalReference = await _mercadoPagoService.ObtenerExternalReferenceAsync(id);
                if (!string.IsNullOrEmpty(externalReference) && externalReference.StartsWith("SUBSCRIPTION|"))
                {
                    var estado = await _mercadoPagoService.ObtenerEstadoPagoAsync(id);
                    await ActivarSuscripcionTenant(externalReference, id, estado);
                }
                else
                {
                    _logger.LogWarning("Webhook SaaS: pago {Id} sin referencia de suscripción reconocible.", id);
                }
            }

            return Ok();
        }

        private async Task ActivarSuscripcionTenant(string externalReference, string paymentId, string estado)
        {
            var tenantIdStr = externalReference.Split('|', 2).ElementAtOrDefault(1);
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out var tenantId))
            {
                _logger.LogWarning("External reference de suscripción con formato inválido: {Ref}", externalReference);
                return;
            }

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
            {
                _logger.LogWarning("Webhook de suscripción: Tenant {TenantId} no encontrado.", tenantId);
                return;
            }

            if (estado == "approved")
            {
                tenant.Status = SubscriptionStatus.Active;
                tenant.SubscriptionEndsAt = DateTime.UtcNow.AddMonths(1);
                _logger.LogInformation("Suscripción del Tenant {TenantId} activada por pago {PaymentId}.", tenantId, paymentId);
            }
            else if (estado is "rejected" or "cancelled")
            {
                tenant.Status = SubscriptionStatus.PastDue;
                _logger.LogInformation("Pago {PaymentId} de suscripción del Tenant {TenantId} rechazado/cancelado (estado: {Estado}).", paymentId, tenantId, estado);
            }
            else
            {
                _logger.LogInformation("Pago {PaymentId} de suscripción del Tenant {TenantId} en estado intermedio: {Estado}.", paymentId, tenantId, estado);
                return;
            }

            await _context.SaveChangesAsync(default);
        }

        private static string? ExtraerDataId(JsonElement json)
        {
            if (json.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProperty))
            {
                return idProperty.ValueKind == JsonValueKind.Number
                    ? idProperty.GetRawText()
                    : idProperty.GetString();
            }
            return null;
        }

        /// <summary>
        /// Verifica la firma HMAC-SHA256 de MercadoPago (header x-signature) usando el manifest
        /// documentado: "id:{data.id};request-id:{x-request-id};ts:{ts};".
        /// Si no hay secreto configurado (MercadoPago:WebhookSecret), se omite la verificación
        /// y solo se deja constancia en el log — para producción el secreto es obligatorio.
        /// </summary>
        private bool EsFirmaValida(string? dataId)
        {
            var secret = _configuration["MercadoPago:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("MercadoPago:WebhookSecret no configurado — webhook aceptado sin verificar firma.");
                return true;
            }

            if (!Request.Headers.TryGetValue("x-signature", out var signatureHeader) ||
                !Request.Headers.TryGetValue("x-request-id", out var requestIdHeader))
            {
                return false;
            }

            var parts = signatureHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);

            if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var v1))
            {
                return false;
            }

            var manifest = $"id:{dataId};request-id:{requestIdHeader};ts:{ts};";
            var computedHash = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest))
            ).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(v1));
        }
    }
}
