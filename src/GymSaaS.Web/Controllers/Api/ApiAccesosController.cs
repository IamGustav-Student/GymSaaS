using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/accesos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiAccesosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApiAccesosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarIngresoQrCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (result.Exitoso)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Exitoso = false, Mensaje = $"Error interno: {ex.Message}" });
            }
        }
    }
}
