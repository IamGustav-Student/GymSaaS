using GymSaaS.Application.Pagos.Commands.ProcesarWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WebhooksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/webhooks/mercadopago
        [HttpPost("mercadopago")]
        public async Task<IActionResult> RecibirNotificacion([FromQuery] string topic, [FromQuery] string id)
        {
            // MercadoPago manda ?topic=payment&id=123456
            if (topic == "payment" && !string.IsNullOrEmpty(id))
            {
                // Disparamos el comando en segundo plano (Fire and Forget seguro)
                // En producción idealmente usarías una cola (Hangfire/RabbitMQ), 
                // pero para MVP el Mediator directo funciona.
                await _mediator.Send(new ProcesarWebhookCommand(id));
            }

            // SIEMPRE responder 200 OK a MercadoPago, o te seguirá mandando el aviso
            return Ok();
        }
    }
}