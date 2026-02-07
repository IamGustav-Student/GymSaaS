using GymSaaS.Application.Clases.Commands.CreateClase;
using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Commands.UpdateClase;
using GymSaaS.Application.Clases.Queries.GetClaseById;
using GymSaaS.Application.Clases.Queries.GetClases;
using GymSaaS.Application.Socios.Queries.GetSocios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class ClasesController : Controller
    {
        private readonly IMediator _mediator;

        public ClasesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // --- MÉTODOS MVC EXISTENTES (Para Admin) ---
        public async Task<IActionResult> Index()
        {
            var clases = await _mediator.Send(new GetClasesQuery());
            return View(clases);
        }

        // ... (Resto de métodos Create, Edit, etc. se mantienen igual) ...

        // --- NUEVO: API ENDPOINTS (Para PWA Móvil) ---

        [HttpGet("api/clases")]
        public async Task<IActionResult> GetClasesApi()
        {
            try
            {
                var clases = await _mediator.Send(new GetClasesQuery());
                return Ok(clases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("api/clases/{id}/reservar")]
        public async Task<IActionResult> ReservarApi(int id, [FromBody] ReservarClaseCommand command)
        {
            if (id != command.ClaseId) return BadRequest("ID mismatch");

            try
            {
                await _mediator.Send(command);
                return Ok(new { mensaje = "Reserva exitosa" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}