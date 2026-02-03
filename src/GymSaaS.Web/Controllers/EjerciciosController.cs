using GymSaaS.Application.Ejercicios.Commands.CreateEjercicio;
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
    }
}