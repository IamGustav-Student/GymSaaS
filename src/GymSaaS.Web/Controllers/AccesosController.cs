using GymSaaS.Application.Accesos.Commands.RegistrarAcceso;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class AccesosController : Controller
    {
        private readonly IMediator _mediator;

        public AccesosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Pantalla Principal (El "Kiosco")
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Validar(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni)) return RedirectToAction("Index");

            var resultado = await _mediator.Send(new RegistrarAccesoCommand { Dni = dni });

            // Enviamos el resultado a la vista para mostrar VERDE o ROJO
            return View("Index", resultado);
        }
    }
}