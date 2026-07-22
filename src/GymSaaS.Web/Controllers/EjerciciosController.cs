using GymSaaS.Application.Ejercicios.Commands.CreateEjercicio;
using GymSaaS.Application.Ejercicios.Commands.DeleteEjercicio;
using GymSaaS.Application.Ejercicios.Commands.UpdateEjercicio;
using GymSaaS.Application.Ejercicios.Queries.GetEjercicioById;
using GymSaaS.Application.Ejercicios.Queries.GetEjercicios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class EjerciciosController : Controller
    {
        private readonly IMediator _mediator;

        public EjerciciosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: Ejercicios
        public async Task<IActionResult> Index()
        {
            var ejercicios = await _mediator.Send(new GetEjerciciosQuery());
            return View(ejercicios);
        }

        // GET: Ejercicios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ejercicios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEjercicioCommand command)
        {
            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                TempData["SuccessMessage"] = "Ejercicio creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }

        // GET: Ejercicios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ejercicio = await _mediator.Send(new GetEjercicioByIdQuery(id));
            if (ejercicio == null) return NotFound();

            var command = new UpdateEjercicioCommand
            {
                Id = ejercicio.Id,
                Nombre = ejercicio.Nombre,
                GrupoMuscular = ejercicio.GrupoMuscular,
                VideoUrl = ejercicio.VideoUrl,
                Descripcion = ejercicio.Descripcion
            };

            return View(command);
        }

        // POST: Ejercicios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateEjercicioCommand command)
        {
            if (id != command.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                TempData["SuccessMessage"] = "Ejercicio actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }

        // POST: Ejercicios/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteEjercicioCommand(id));
            TempData["SuccessMessage"] = "Ejercicio eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}