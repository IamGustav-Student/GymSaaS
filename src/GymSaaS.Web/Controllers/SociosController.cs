using GymSaaS.Application.Socios.Commands.CreateSocio;
using GymSaaS.Application.Socios.Commands.UpdateSocio; // Agregar
using GymSaaS.Application.Socios.Commands.DeleteSocio; // Agregar
using GymSaaS.Application.Socios.Queries.GetSocios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class SociosController : Controller
    {
        private readonly IMediator _mediator;

        public SociosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: Socios (Lista)
        public async Task<IActionResult> Index()
        {
            var socios = await _mediator.Send(new GetSociosQuery());
            return View(socios);
        }

        // GET: Socios/Create (Formulario)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Socios/Create (Guardar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSocioCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            try
            {
                await _mediator.Send(command);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(command);
            }
        }
        // GET: Socios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            // Reutilizamos la query de lista, o idealmente haríamos una GetSocioByIdQuery
            // Para el MVP, buscaremos directo (o puedes crear la Query si prefieres pureza)

            // HACK MVP: Usar el DbContext directo solo para LEER en el GET y llenar el formulario
            // (En Clean Architecture estricto, deberías hacer un Query Handler, pero para avanzar rápido:)
            // var socio = await _mediator.Send(new GetSocioDetailQuery(id)); 

            // Vamos a asumir que tienes una Query o usamos un truco rápido:
            // Te recomiendo crear "GetSocioByIdQuery" si quieres ser puro, 
            // pero si quieres probar YA, implementemos la Query rápida aquí:

            var socio = await _mediator.Send(new GymSaaS.Application.Socios.Queries.GetSocios.GetSocioByIdQuery(id));

            // Mapeamos al comando para editar
            var command = new UpdateSocioCommand
            {
                Id = socio.Id,
                Nombre = socio.Nombre,
                Apellido = socio.Apellido,
                Email = socio.Email,
                Telefono = socio.Telefono
            };

            return View(command);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateSocioCommand command)
        {
            if (id != command.Id) return BadRequest();
            if (!ModelState.IsValid) return View(command);

            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost] // Delete siempre debe ser POST para seguridad
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteSocioCommand(id));
            return RedirectToAction(nameof(Index));
        }

        // GET: Socios/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Usamos una Query existente o creamos un DTO rápido aquí para visualizar
            // Por simplicidad y potencia visual, traemos todo lo necesario.

            var socio = await _mediator.Send(new GetSocioByIdQuery(id)); // Asegúrate de tener esta Query o usa _context si prefieres rapidez MVP

            // NOTA: Si GetSocioByIdQuery no trae membresias, necesitamos cargarlas.
            // Para asegurar "Integridad de Archivos", asumimos que usarás el DbContext directo o expandirás la Query después.
            // Aquí te dejo la versión "Clean Architecture Strict" asumiendo que expandiremos la Query,
            // pero si te da error, avísame para pasarte la Query expandida.

            // Opción MVP Robusta: Pasamos el ID a la vista y dejamos que la vista o un ViewComponent cargue los datos,
            // PERO para SEO/Performance, mejor cargar datos aquí.

            // TRUCO: Si no tienes la Query 'GetSocioById' con includes, podemos usar el contexto temporalmente (solo lectura)
            // Si prefieres mantener Clean Architecture puro, deberíamos crear 'GetSocioDetailsQuery'.
            // Vamos a asumir que quieres ver el resultado YA.

            return View(socio);
        }
    }
}