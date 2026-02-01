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

        // Vista Monitor
        public IActionResult Monitor()
        {
            return View();
        }

        // API: Procesa el código escaneado (AJAX)
        [HttpPost]
        public async Task<IActionResult> RegistrarAcceso([FromBody] string codigoQr)
        {
            if (string.IsNullOrWhiteSpace(codigoQr))
                return Json(new { success = false, message = "Lectura inválida" });

            var result = await _mediator.Send(new RegistrarAccesoCommand(codigoQr));
            return Json(result);
        }
    }
}