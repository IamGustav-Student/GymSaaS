using GymSaaS.Application.Socios.Commands.CreateSocio;
using GymSaaS.Application.Socios.Commands.UpdateSocio;
using GymSaaS.Application.Socios.Commands.DeleteSocio;
using GymSaaS.Application.Socios.Queries.GetSocios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class SociosController : Controller
    {
        private readonly IMediator _mediator;

        public SociosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: Socios (Lista)
        public async Task<IActionResult> Index()
        {
            var socios = await _mediator.Send(new GetSociosQuery());
            return View(socios);
        }

        // GET: Socios/Create (Formulario)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Socios/Create (Guardar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSocioCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            try
            {
                await _mediator.Send(command);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear socio: " + ex.Message);
                return View(command);
            }
        }

        // GET: Socios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _mediator.Send(new GetSocioByIdQuery(id));
            if (dto == null) return NotFound();

            var command = new UpdateSocioCommand
            {
                Id = dto.Id,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Telefono = dto.Telefono
            };

            return View(command);
        }

        // POST: Socios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateSocioCommand command)
        {
            if (id != command.Id) return BadRequest();
            if (!ModelState.IsValid) return View(command);

            try
            {
                await _mediator.Send(command);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(command);
            }
        }

        // POST: Socios/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteSocioCommand(id));
            return RedirectToAction(nameof(Index));
        }

        // GET: Socios/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var socioDto = await _mediator.Send(new GetSocioByIdQuery(id));

            if (socioDto == null)
            {
                return NotFound();
            }

            return View(socioDto);
        }
    }
}