using GymSaaS.Application.Ejercicios.Queries.GetEjercicios;
using GymSaaS.Application.Rutinas.Commands.CreateRutina;
using GymSaaS.Application.Rutinas.Commands.UpdateRutina;
using GymSaaS.Application.Rutinas.Queries.GetRutinaById;
using GymSaaS.Application.Rutinas.Queries.GetRutinas;
using GymSaaS.Application.Socios.Queries.GetSocios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class RutinasController : Controller
    {
        private readonly IMediator _mediator;

        public RutinasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: Rutinas
        public async Task<IActionResult> Index(string busqueda)
        {
            // 1. Obtenemos todas las rutinas (tu query original)
            var rutinas = await _mediator.Send(new GetRutinasQuery());

            // 2. Filtramos si el usuario escribió algo
            if (!string.IsNullOrEmpty(busqueda))
            {
                rutinas = rutinas.Where(r =>
                    (r.SocioNombre != null && r.SocioNombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Nombre != null && r.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // 3. Guardamos la búsqueda para mostrarla en el input (UX)
            ViewData["BusquedaActual"] = busqueda;

            return View(rutinas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var rutina = await _mediator.Send(new GetRutinaByIdQuery(id));
            if (rutina == null) return NotFound();
            return View(rutina);
        }

        // === CREACIÓN ===
        public async Task<IActionResult> Create()
        {
            await CargarListas();
            return View(new CreateRutinaCommand());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRutinaCommand command)
        {
            if (ModelState.IsValid)
            {
                if (command.Ejercicios == null || !command.Ejercicios.Any())
                {
                    ModelState.AddModelError("", "La rutina debe tener ejercicios.");
                }
                else
                {
                    await _mediator.Send(command);
                    TempData["SuccessMessage"] = "Rutina creada con éxito.";
                    return RedirectToAction(nameof(Index));
                }
            }
            await CargarListas();
            return View(command);
        }

        // === EDICIÓN ===
        public async Task<IActionResult> Edit(int id)
        {
            var rutinaDto = await _mediator.Send(new GetRutinaByIdQuery(id));
            if (rutinaDto == null) return NotFound();

            await CargarListas();

            // Mapeo manual de DTO -> Command para pre-llenar el formulario
            var command = new UpdateRutinaCommand
            {
                Id = rutinaDto.Id,
                Nombre = rutinaDto.Nombre,
                SocioId = rutinaDto.SocioId,
                FechaFin = rutinaDto.FechaFin,
                Ejercicios = rutinaDto.Ejercicios.Select(e => new RutinaEjercicioCommandDto
                {
                    EjercicioId = e.EjercicioId,
                    Series = e.Series,
                    Repeticiones = e.Repeticiones,
                    PesoSugerido = e.PesoSugerido,
                    Notas = e.Notas
                }).ToList()
            };

            return View(command);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateRutinaCommand command)
        {
            if (id != command.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                if (command.Ejercicios == null || !command.Ejercicios.Any())
                {
                    ModelState.AddModelError("", "La rutina debe tener ejercicios.");
                }
                else
                {
                    await _mediator.Send(command);
                    TempData["SuccessMessage"] = "Rutina actualizada.";
                    return RedirectToAction(nameof(Index));
                }
            }
            await CargarListas();
            return View(command);
        }

        // Helper para cargar desplegables
        private async Task CargarListas()
        {
            var socios = await _mediator.Send(new GetSociosQuery());
            var ejercicios = await _mediator.Send(new GetEjerciciosQuery());

            // Usamos "ListaSocios" en el ViewData.
            // La lista ya viene filtrada y ordenada desde la Query.
            ViewData["ListaSocios"] = new SelectList(socios, "Id", "NombreCompleto");

            // Catálogo completo de ejercicios para JS
            ViewData["ListaEjercicios"] = ejercicios.OrderBy(e => e.GrupoMuscular).ThenBy(e => e.Nombre);
        }
    }
}