using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Socios.Queries.GetSocios; // Para obtener nombre del socio al vender
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // 1. Listado de Planes (Configuración)
        public async Task<IActionResult> Index()
        {
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            return View(planes);
        }

        // 2. Crear Nuevo Plan
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

        // 3. Vender/Asignar Membresía a un Socio
        [HttpGet]
        public async Task<IActionResult> Asignar(int socioId)
        {
            // Buscamos los planes para llenar el Dropdown
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());

            ViewBag.Planes = new SelectList(planes, "Id", "Nombre", null, "Precio");
            ViewBag.SocioId = socioId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Asignar(AsignarMembresiaCommand command)
        {
            if (!ModelState.IsValid)
            {
                var planes = await _mediator.Send(new GetTiposMembresiaQuery());
                ViewBag.Planes = new SelectList(planes, "Id", "Nombre");
                ViewBag.SocioId = command.SocioId;
                return View(command);
            }

            // 1. Ejecutar la Venta (Se crea la membresía y el registro de pago)
            var membresiaId = await _mediator.Send(command);

            // 2. Si eligió MercadoPago, generamos el link
            if (command.MetodoPago == "MercadoPago")
            {
                try
                {
                    // Llamamos al comando que acabamos de crear
                    var linkPago = await _mediator.Send(new CrearLinkPagoCommand(membresiaId));

                    // Redirigimos a una vista especial con el QR y el Link
                    return RedirectToAction("LinkPago", new { url = linkPago });
                }
                catch (Exception ex)
                {
                    // Si falla MP, avisamos pero la venta ya quedó registrada localmente
                    TempData["Error"] = "Venta registrada, pero falló MercadoPago: " + ex.Message;
                    return RedirectToAction("Index", "Socios");
                }
            }

            // Si fue Efectivo, volvemos a la lista de Socios
            return RedirectToAction("Index", "Socios");
        }

        // Nueva Acción para mostrar el Link
        public IActionResult LinkPago(string url)
        {
            return View("LinkPago", url); // Pasamos el string URL como modelo
        }
    }
}
