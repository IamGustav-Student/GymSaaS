using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Socios.Queries.GetSocios;
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

        // GET: Membresias (Listado de Planes)
        public async Task<IActionResult> Index()
        {
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            return View(planes);
        }

        // GET: Membresias/Create (Nuevo Plan)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Membresias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTipoMembresiaCommand command)
        {
            if (!ModelState.IsValid) return View(command);
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }

        // GET: Membresias/Asignar?socioId=1 (Venta)
        [HttpGet]
        public async Task<IActionResult> Asignar(int? socioId)
        {
            // 1. Cargamos las listas con los nombres CORRECTOS
            await CargarListasAsignacion(socioId);

            // 2. Preparamos el modelo
            var modelo = new AsignarMembresiaCommand();
            if (socioId.HasValue)
            {
                modelo.SocioId = socioId.Value;
            }

            return View(modelo);
        }

        // POST: Membresias/Asignar (Procesar Venta)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(AsignarMembresiaCommand command)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasAsignacion(command.SocioId);
                return View(command);
            }

            try
            {
                var membresiaId = await _mediator.Send(command);

                if (command.MetodoPago == "Efectivo")
                {
                    TempData["SuccessMessage"] = "¡Venta en Efectivo registrada correctamente!";
                    return RedirectToAction("Details", "Socios", new { id = command.SocioId });
                }

                // Si es MercadoPago u otro, vamos a confirmar Link
                return RedirectToAction("LinkPago", new { membresiaId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar la venta: {ex.Message}");
                await CargarListasAsignacion(command.SocioId);
                return View(command);
            }
        }

        // GET: Confirmación Link de Pago
        public IActionResult LinkPago(int membresiaId)
        {
            return View(membresiaId);
        }

        // POST: Generar Link Real con MP
        [HttpPost]
        public async Task<IActionResult> GenerarLink(int membresiaId)
        {
            try
            {
                // CORRECCIÓN AQUÍ: Usamos paréntesis (constructor) en lugar de llaves
                var urlPago = await _mediator.Send(new CrearLinkPagoCommand(membresiaId));

                return Redirect(urlPago);
            }
            catch (Exception ex)
            {
                return Content($"Error al conectar con MercadoPago: {ex.Message}");
            }
        }

        // --- MÉTODOS AUXILIARES ---

        private async Task CargarListasAsignacion(int? socioIdSeleccionado = null)
        {
            // 1. Lista de Planes (Tipos de Membresía) -> ViewBag.ListaTipos
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            ViewBag.ListaTipos = new SelectList(
                planes.Select(p => new { Id = p.Id, Texto = $"{p.Nombre} - ${p.Precio:N0}" }),
                "Id",
                "Texto"
            );

            // 2. Lista de Socios -> ViewBag.ListaSocios
            var socios = await _mediator.Send(new GetSociosQuery());
            ViewBag.ListaSocios = new SelectList(
                socios.Select(s => new { Id = s.Id, Texto = $"{s.NombreCompleto} - DNI: {s.Dni}" }),
                "Id",
                "Texto",
                socioIdSeleccionado
            );
        }
    }
}