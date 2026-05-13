using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Commands.UpdateTipoMembresia;
using GymSaaS.Application.Membresias.Commands.DeleteTipoMembresia;
using GymSaaS.Application.Membresias.Commands.RenovarMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/membresias")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiMembresiasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApiMembresiasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("tipos")]
        public async Task<ActionResult<List<TipoMembresiaDto>>> GetTipos()
        {
            var result = await _mediator.Send(new GetTiposMembresiaQuery());
            return Ok(result);
        }

        [HttpPost("tipos")]
        public async Task<ActionResult<int>> Create([FromBody] CreateTipoMembresiaCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("tipos/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoMembresiaCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("tipos/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteTipoMembresiaCommand(id));
            return Ok();
        }

        [HttpPost("renovar")]
        public async Task<ActionResult<int>> Renovar([FromBody] RenovarMembresiaCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Exitoso = false, Mensaje = ex.Message });
            }
        }
    }
}
