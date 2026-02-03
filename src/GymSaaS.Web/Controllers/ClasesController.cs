using GymSaaS.Application.Clases.Commands.CreateClase;
using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Commands.UpdateClase; // Nuevo
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

        public async Task<IActionResult> Index()
        {
            var clases = await _mediator.Send(new GetClasesQuery());
            return View(clases);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClaseCommand command)
        {
            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                TempData["SuccessMessage"] = "Clase agendada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }

        // === NUEVO: VER LISTADO (Details) ===
        public async Task<IActionResult> Details(int id)
        {
            var clase = await _mediator.Send(new GetClaseByIdQuery(id));
            if (clase == null) return NotFound();
            return View(clase);
        }

        // === RESERVAR ===
        public async Task<IActionResult> Reservar(int id)
        {
            var clase = await _mediator.Send(new GetClaseByIdQuery(id));
            if (clase == null) return NotFound();

            var socios = await _mediator.Send(new GetSociosQuery());
            ViewData["ListaSocios"] = new SelectList(socios, "Id", "NombreCompleto");
            ViewBag.ClaseInfo = clase;

            return View(new ReservarClaseCommand { ClaseId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservarClaseCommand command)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _mediator.Send(command);
                    TempData["SuccessMessage"] = "Inscripción realizada con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            var clase = await _mediator.Send(new GetClaseByIdQuery(command.ClaseId));
            var socios = await _mediator.Send(new GetSociosQuery());
            ViewData["ListaSocios"] = new SelectList(socios, "Id", "NombreCompleto");
            ViewBag.ClaseInfo = clase;

            return View(command);
        }

        // === NUEVO: EDITAR CLASE ===
        public async Task<IActionResult> Edit(int id)
        {
            var clase = await _mediator.Send(new GetClaseByIdQuery(id));
            if (clase == null) return NotFound();

            var command = new UpdateClaseCommand
            {
                Id = clase.Id,
                Nombre = clase.Nombre,
                Instructor = clase.Instructor,
                FechaHoraInicio = clase.FechaHoraInicio,
                DuracionMinutos = clase.DuracionMinutos,
                CupoMaximo = clase.CupoMaximo,
                Activa = clase.Activa
            };

            return View(command);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateClaseCommand command)
        {
            if (id != command.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                TempData["SuccessMessage"] = "Clase actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }
    }
}