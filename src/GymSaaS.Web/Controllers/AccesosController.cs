using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr; // Usamos el nuevo namespace
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
            return View();
        }

        // POST: Accesos/Registrar
        // Compatibilidad: El formulario web envía 'socioId' como string.
        // Adaptamos esto al nuevo Command que es más complejo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(string socioId)
        {
            if (string.IsNullOrWhiteSpace(socioId) || !int.TryParse(socioId, out int idNumerico))
            {
                ModelState.AddModelError("socioId", "ID de socio inválido. Intente nuevamente.");
                return View("Index");
            }

            try
            {
                // ADAPTADOR: Convertimos la entrada simple del formulario web
                // al Comando Robusto diseñado para la App Móvil.
                // Como es entrada manual por teclado, no hay coordenadas ni QR escaneado.
                var command = new RegistrarIngresoQrCommand
                {
                    SocioId = idNumerico,
                    CodigoQrEscaneado = "", // Indica entrada manual
                    LatitudUsuario = 0,
                    LongitudUsuario = 0
                };

                // Ejecutamos la lógica blindada (Anti-passback, Timezone, Membresía)
                var result = await _mediator.Send(command);

                // El nuevo result tiene propiedades: Exitoso, Mensaje, NombreSocio, FotoUrl.
                // Pasamos este objeto a la vista para mostrar la tarjeta verde/roja.
                return View("Index", result);
            }
            catch (Exception ex)
            {
                // En producción, loguear ex.
                ModelState.AddModelError("", $"Error procesando acceso: {ex.Message}");
                return View("Index");
            }
        }
    }
}