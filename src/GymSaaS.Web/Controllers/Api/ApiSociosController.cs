using GymSaaS.Application.Socios.Queries.GetSocios;
using GymSaaS.Application.Socios.Commands.CreateSocio;
using GymSaaS.Application.Socios.Commands.UpdateSocio;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/socios")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiSociosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApiSociosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<SocioDto>>> Get()
        {
            var result = await _mediator.Send(new GetSociosQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SocioDto>> Get(int id)
        {
            var result = await _mediator.Send(new GetSocioByIdQuery(id));
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateSocioCommand command)
        {
            try
            {
                var id = await _mediator.Send(command);
                return Ok(id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Exitoso = false, Mensaje = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSocioCommand command)
        {
            if (id != command.Id) return BadRequest();

            try
            {
                await _mediator.Send(command);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Exitoso = false, Mensaje = ex.Message });
            }
        }

        // Podríamos agregar DELETE aquí siguiendo el mismo patrón
    }
}
