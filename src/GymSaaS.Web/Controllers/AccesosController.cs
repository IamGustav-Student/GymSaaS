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

        // GET: Accesos
        // Renombramos 'Monitor' a 'Index' para que coincida con Views/Accesos/Index.cshtml
        public IActionResult Index()
        {
            return View();
        }

        // POST: Accesos/Registrar
        // Cambiamos para devolver una VISTA en lugar de JSON
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(string socioId) // Recibimos string para ser flexibles (acepta QR o ID manual)
        {
            if (string.IsNullOrWhiteSpace(socioId))
            {
                ModelState.AddModelError("socioId", "Lectura inválida. Intente nuevamente.");
                return View("Index");
            }

            try
            {
                // Enviamos el comando. Asumimos que tu comando acepta un string en el constructor.
                // Si tu comando requiere int, usa: int.Parse(socioId)
                var result = await _mediator.Send(new RegistrarAccesoCommand(socioId));

                // Retornamos la misma vista Index, pero ahora con el modelo (result) cargado.
                // Esto hará que aparezca la tarjeta de "ACCESO PERMITIDO/DENEGADO".
                return View("Index", result);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error del sistema: {ex.Message}");
                return View("Index");
            }
        }
    }
}