using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Necesario para SelectList

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class MembresiasController : Controller
    {
        private readonly IMediator _mediator;

        public MembresiasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            return View(planes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTipoMembresiaCommand command)
        {
            if (!ModelState.IsValid) return View(command);
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }

        // Vista para seleccionar socio y vender
        // CORRECCIÓN: Cambiado a async Task<IActionResult> para cargar datos de la BD
        public async Task<IActionResult> Asignar(int socioId)
        {
            // 1. Guardamos el ID del socio para que la vista lo use en el form oculto
            ViewBag.SocioId = socioId;

            // 2. Traemos los planes disponibles desde la Base de Datos
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());

            // 3. Creamos la lista desplegable para la vista (asp-items="ViewBag.Planes")
            // Mostramos "Nombre - Precio" para mejor experiencia de usuario
            ViewBag.Planes = new SelectList(
                planes.Select(p => new {
                    Id = p.Id,
                    Texto = $"{p.Nombre} - ${p.Precio:N0}"
                }),
                "Id",
                "Texto"
            );

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Asignar(AsignarMembresiaCommand command)
        {
            if (!ModelState.IsValid) return RedirectToAction("Index", "Socios");

            var membresiaId = await _mediator.Send(command);

            // Redirigir a la pantalla de pago o confirmación
            return RedirectToAction("LinkPago", new { membresiaId });
        }

        // Pantalla intermedia para generar el link
        public IActionResult LinkPago(int membresiaId)
        {
            return View(membresiaId);
        }

        [HttpPost]
        public async Task<IActionResult> GenerarLink(int membresiaId)
        {
            // Ahora el comando es mucho más simple y seguro
            var link = await _mediator.Send(new CrearLinkPagoCommand(membresiaId));

            return Redirect(link);
        }
    }
}