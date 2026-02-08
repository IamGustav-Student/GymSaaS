using GymSaaS.Application.Clases.Commands.CreateClase;
using GymSaaS.Application.Clases.Commands.ReservarClase; // Necesario para reservar manual
using GymSaaS.Application.Clases.Commands.UpdateClase; // Necesario para editar
using GymSaaS.Application.Clases.Queries.GetClaseById;
using GymSaaS.Application.Clases.Queries.GetClases;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectList
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class ClasesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;

        public ClasesController(IMediator mediator, IApplicationDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        // GET: Clases (Dashboard)
        public async Task<IActionResult> Index()
        {
            try
            {
                var clases = await _mediator.Send(new GetClasesQuery());
                return View(clases);
            }
            catch
            {
                var clasesDirectas = await _context.Clases
                    .Include(c => c.Reservas)
                    .Include(c => c.ListaEspera)
                    .OrderBy(c => c.FechaHoraInicio)
                    .Select(c => new ClaseDto
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Instructor = c.Instructor,
                        FechaHoraInicio = c.FechaHoraInicio,
                        CupoMaximo = c.CupoMaximo,
                        CupoActual = c.Reservas.Count(r => r.Activa),
                        CantidadEnEspera = c.ListaEspera.Count
                    })
                    .ToListAsync();
                return View(clasesDirectas);
            }
        }

        // GET: Clases/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var clase = await _mediator.Send(new GetClaseByIdQuery(id));
            if (clase == null) return NotFound();
            return View(clase);
        }

        // GET: Clases/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClaseCommand command)
        {
            if (ModelState.IsValid)
            {
                await _mediator.Send(command);
                TempData["Success"] = "Clase programada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }

        // ==========================================
        // NUEVAS FUNCIONES (EDITAR Y RESERVAR MANUAL)
        // ==========================================

        // GET: Clases/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            // 1. Obtener la clase actual con todos sus detalles
            var clase = await _context.Clases.FindAsync(id);
            if (clase == null) return NotFound();

            // 2. Mapear a UpdateClaseCommand para pre-llenar el formulario
            var command = new UpdateClaseCommand
            {
                Id = clase.Id,
                Nombre = clase.Nombre,
                Instructor = clase.Instructor,
                FechaHoraInicio = clase.FechaHoraInicio,
                DuracionMinutos = clase.DuracionMinutos,
                CupoMaximo = clase.CupoMaximo,
                Precio = clase.Precio,
                Activa = clase.Activa
            };

            return View(command);
        }

        // POST: Clases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateClaseCommand command)
        {
            if (id != command.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    await _mediator.Send(command);
                    TempData["Success"] = "Clase actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                }
            }
            return View(command);
        }

        // GET: Clases/Reservar/5 (Vista para que el Admin elija un Socio)
        public async Task<IActionResult> Reservar(int id)
        {
            var clase = await _context.Clases.FindAsync(id);
            if (clase == null) return NotFound();

            // Cargar lista de socios para el dropdown
            var socios = await _context.Socios
                .Where(s => s.Activo && !s.IsDeleted)
                .OrderBy(s => s.Apellido)
                .Select(s => new { s.Id, NombreCompleto = $"{s.Apellido}, {s.Nombre} ({s.Dni})" })
                .ToListAsync();

            ViewBag.SocioId = new SelectList(socios, "Id", "NombreCompleto");
            ViewBag.ClaseNombre = clase.Nombre;
            ViewBag.ClaseFecha = clase.FechaHoraInicio;
            ViewBag.ClaseId = clase.Id; // Importante para el POST

            return View();
        }

        // POST: Clases/Reservar (Acción Manual del Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int claseId, int socioId)
        {
            try
            {
                // Usamos el mismo comando que usa el alumno, pero inyectamos los IDs manualmente
                var resultado = await _mediator.Send(new ReservarClaseCommand
                {
                    ClaseId = claseId,
                    SocioId = socioId
                });

                TempData["Success"] = $"Socio inscrito correctamente. Reserva ID: {resultado.ReservaId}";
                return RedirectToAction(nameof(Details), new { id = claseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo realizar la reserva manual: " + ex.Message;
                return RedirectToAction(nameof(Reservar), new { id = claseId });
            }
        }

        // ==========================================
        // FIN NUEVAS FUNCIONES
        // ==========================================

        // POST: Clases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clase = await _context.Clases
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clase != null)
            {
                if (clase.Reservas.Any())
                {
                    clase.Activa = false; // Soft Delete
                    TempData["Info"] = "La clase tiene reservas, se ha marcado como Inactiva.";
                }
                else
                {
                    _context.Clases.Remove(clase); // Hard Delete
                    TempData["Success"] = "Clase eliminada.";
                }
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}