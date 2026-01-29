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
        public IActionResult Index()
        {
            // Mostramos la pantalla vacía (sin modelo) al entrar por primera vez
            return View();
        }

        // POST: Accesos/Registrar
        [HttpPost]
        public async Task<IActionResult> Registrar(int socioId)
        {
            // CORRECCIÓN: Usamos el constructor posicional (con paréntesis) 
            // porque RegistrarAccesoCommand ahora es un 'record' que pide (int SocioId).
            // Antes fallaba porque intentaba usar { Dni = ... } que ya no existe.
            
            var resultado = await _mediator.Send(new RegistrarAccesoCommand(socioId));

            // Devolvemos la misma vista Index, pero ahora con el modelo (resultado) lleno
            // para que se muestre el semáforo.
            return View(nameof(Index), resultado);
        }
    }
}