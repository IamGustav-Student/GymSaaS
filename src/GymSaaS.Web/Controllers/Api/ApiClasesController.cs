using GymSaaS.Application.Clases.Commands.CreateClase;
using GymSaaS.Application.Clases.Commands.UpdateClase;
using GymSaaS.Application.Clases.Commands.CancelarReserva; // Deberíamos tener DeleteClase o similar
using GymSaaS.Application.Clases.Queries.GetClases;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/clases")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiClasesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApiClasesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<ClaseDto>>> GetAll([FromQuery] DateTime? fecha)
        {
            // Nota: El Query original puede filtrar por fecha si lo deseamos
            var result = await _mediator.Send(new GetClasesQuery { /* Fecha = fecha */ });
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateClaseCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClaseCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return Ok();
        }

        // RegistrarAsistencia o Reservar desde el desktop si se requiere
    }
}
