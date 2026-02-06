using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.CreateTipoMembresia;
using GymSaaS.Application.Membresias.Commands.DeleteTipoMembresia;
using GymSaaS.Application.Membresias.Commands.UpdateTipoMembresia;
using GymSaaS.Application.Membresias.Queries.GetTipoMembresiaById;
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

        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _mediator.Send(new GetTipoMembresiaByIdQuery(id));
            if (entity == null) return NotFound();

            var command = new UpdateTipoMembresiaCommand
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Precio = entity.Precio,
                DuracionDias = entity.DuracionDias,
                CantidadClases = entity.CantidadClases,
                // Mapeo manual para la vista
                AccesoLunes = entity.AccesoLunes,
                AccesoMartes = entity.AccesoMartes,
                AccesoMiercoles = entity.AccesoMiercoles,
                AccesoJueves = entity.AccesoJueves,
                AccesoViernes = entity.AccesoViernes,
                AccesoSabado = entity.AccesoSabado,
                AccesoDomingo = entity.AccesoDomingo
            };

            return View(command);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteTipoMembresiaCommand(id));
            return RedirectToAction(nameof(Index));
        }

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
    }
}