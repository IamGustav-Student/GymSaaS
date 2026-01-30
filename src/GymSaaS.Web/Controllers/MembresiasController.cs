using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Socios.Queries.GetSocios; // Necesario para obtener socios
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

        // GET: Asignar (Venta)
        // CAMBIO: Ahora socioId es nullable (int?) para permitir entrar sin pre-selección
        public async Task<IActionResult> Asignar(int? socioId)
        {
            // 1. Cargar Planes (Como antes)
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            ViewBag.Planes = new SelectList(
                planes.Select(p => new {
                    Id = p.Id,
                    Texto = $"{p.Nombre} - ${p.Precio:N0}"
                }), "Id", "Texto");

            // 2. NUEVO: Cargar Socios para el desplegable
            var socios = await _mediator.Send(new GetSociosQuery());

            // Creamos una lista con formato "Nombre Apellido - DNI" para facilitar la búsqueda
            var listaSocios = socios.Select(s => new {
                Id = s.Id,
                Texto = $"{s.NombreCompleto} - {s.Dni}"
            }).OrderBy(s => s.Texto); // Ordenados alfabéticamente

            // Si venimos con un ID, lo pre-seleccionamos
            ViewBag.Socios = new SelectList(listaSocios, "Id", "Texto", socioId);

            // Pasamos el ID al modelo si existe, para casos de validación inicial
            var model = new AsignarMembresiaCommand();
            if (socioId.HasValue)
            {
                model.SocioId = socioId.Value;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Asignar(AsignarMembresiaCommand command)
        {
            if (!ModelState.IsValid)
            {
                // Si falla, hay que recargar las listas (Planes y Socios)
                var planes = await _mediator.Send(new GetTiposMembresiaQuery());
                ViewBag.Planes = new SelectList(planes.Select(p => new { Id = p.Id, Texto = $"{p.Nombre} - ${p.Precio:N0}" }), "Id", "Texto");

                var socios = await _mediator.Send(new GetSociosQuery());
                ViewBag.Socios = new SelectList(socios.Select(s => new { Id = s.Id, Texto = $"{s.NombreCompleto} - {s.Dni}" }), "Id", "Texto", command.SocioId);

                return View(command);
            }

            var membresiaId = await _mediator.Send(command);

            if (command.MetodoPago == "Efectivo")
            {
                TempData["SuccessMessage"] = "¡Venta en Efectivo registrada correctamente!";
                return RedirectToAction("Details", "Socios", new { id = command.SocioId });
            }

            return RedirectToAction("LinkPago", new { membresiaId });
        }

        public IActionResult LinkPago(int membresiaId)
        {
            return View(membresiaId);
        }

        [HttpPost]
        public async Task<IActionResult> GenerarLink(int membresiaId)
        {
            var link = await _mediator.Send(new CrearLinkPagoCommand(membresiaId));
            return Redirect(link);
        }
    }
}