using GymSaaS.Application.Pagos.Commands.EjecutarReintentos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CronJobsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;

        public CronJobsController(IMediator mediator, IConfiguration configuration)
        {
            _mediator = mediator;
            _configuration = configuration;
        }

        [HttpPost("procesar-reintentos")]
        public async Task<IActionResult> ProcesarReintentos([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            // Seguridad simple para Cron Jobs
            var configuredKey = _configuration["CronJobApiKey"] ?? "ClaveSecreta123";

            if (apiKey != configuredKey)
            {
                return Unauthorized();
            }

            var cantidad = await _mediator.Send(new EjecutarReintentosCommand());
            return Ok(new { mensaje = $"Proceso finalizado. Pagos reintentados: {cantidad}" });
        }
    }
}