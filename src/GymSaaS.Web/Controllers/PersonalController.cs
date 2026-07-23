using GymSaaS.Application.Personal.Commands.CrearPersonal;
using GymSaaS.Application.Personal.Commands.ToggleActivoPersonal;
using GymSaaS.Application.Personal.Queries.GetPersonal;
using GymSaaS.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class PersonalController : Controller
    {
        private readonly IMediator _mediator;

        public PersonalController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var personal = await _mediator.Send(new GetPersonalQuery());
            return View(personal);
        }

        [HttpGet]
        public IActionResult Create() => View(new CrearPersonalCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearPersonalCommand command)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _mediator.Send(command);
                    TempData["SuccessMessage"] = "Empleado creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (FluentValidation.ValidationException valEx)
                {
                    foreach (var error in valEx.Errors)
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            return View(command);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int usuarioId)
        {
            var usuarioActualIdStr = User.FindFirst("UsuarioId")?.Value;
            if (!int.TryParse(usuarioActualIdStr, out var usuarioActualId)) return Unauthorized();

            try
            {
                await _mediator.Send(new ToggleActivoPersonalCommand(usuarioId, usuarioActualId));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
