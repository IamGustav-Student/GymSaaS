using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Commands.DeleteTipoMembresia; // Nuevo
using GymSaaS.Application.Membresias.Commands.UpdateTipoMembresia; // Nuevo
using GymSaaS.Application.Membresias.Queries.GetTipoMembresiaById; // Nuevo
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
            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }

        // --- NUEVAS FUNCIONES DE EDICIÓN Y ELIMINACIÓN (PARTE 1) ---

        // GET: Membresias/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _mediator.Send(new GetTipoMembresiaByIdQuery(id));
            if (entity == null) return NotFound();

            // Mapeamos la entidad al comando para editar
            var command = new UpdateTipoMembresiaCommand
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Precio = entity.Precio,
                DuracionDias = entity.DuracionDias,
                CantidadClases = entity.CantidadClases
            };

            return View(command);
        }

        // POST: Membresias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateTipoMembresiaCommand command)
        {
            if (id != command.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    await _mediator.Send(command);
                    return RedirectToAction(nameof(Index));
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
            return View(command);
        }

        // POST: Membresias/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteTipoMembresiaCommand(id));
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------------------------------

        // GET: Membresias/Asignar
        [HttpGet]
        public async Task<IActionResult> Asignar(int? socioId)
        {
            var model = new AsignarMembresiaCommand();
            if (socioId.HasValue)
            {
                model.SocioId = socioId.Value;
            }

            await CargarListasAsignacion(socioId);
            return View(model);
        }

        // POST: Membresias/Asignar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(AsignarMembresiaCommand command)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var membresiaId = await _mediator.Send(command);

                    // Si es MercadoPago, lo llevamos a generar el link
                    if (command.MetodoPago == "MercadoPago")
                    {
                        return RedirectToAction(nameof(GenerarLink), new { membresiaId });
                    }

                    // Si fue Efectivo, volvemos al perfil del socio o al índice
                    return RedirectToAction("Index", "Socios");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al asignar: " + ex.Message);
                }
            }

            // Si falló, recargamos la lista
            await CargarListasAsignacion(command.SocioId);
            return View(command);
        }

        // POST: Generar Link Real con MP
        [HttpPost]
        public async Task<IActionResult> GenerarLink(int membresiaId)
        {
            try
            {
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