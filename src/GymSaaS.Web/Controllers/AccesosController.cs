using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
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

        // POST: Accesos/Registrar (HÍBRIDO: Soporta HTML Form y API JSON)
        [HttpPost]
        [IgnoreAntiforgeryToken] // Permitimos llamadas desde JS/App sin el token del form MVC estricto
        public async Task<IActionResult> Registrar([FromBody] RegistrarIngresoQrCommand? apiCommand, [FromForm] string? socioId)
        {
            // 1. Unificar entrada (Sea JSON o Form)
            RegistrarIngresoQrCommand command;

            if (apiCommand != null && apiCommand.SocioId != 0)
            {
                // Viene por API (JSON body)
                command = apiCommand;
            }
            else if (!string.IsNullOrWhiteSpace(socioId) && int.TryParse(socioId, out int idNumerico))
            {
                // Viene por Formulario Web Clásico (Legacy)
                command = new RegistrarIngresoQrCommand
                {
                    SocioId = idNumerico,
                    CodigoQrEscaneado = "", // Manual
                    LatitudUsuario = 0,
                    LongitudUsuario = 0
                };
            }
            else
            {
                // Entrada inválida
                return Respond(false, "ID de socio inválido o datos incompletos.");
            }

            try
            {
                // 2. Ejecutar Lógica de Negocio
                var result = await _mediator.Send(command);

                // 3. Retornar respuesta adaptativa
                if (result.Exitoso)
                    return Respond(true, result);
                else
                    return Respond(false, result); // Envolvemos el error de negocio
            }
            catch (Exception ex)
            {
                return Respond(false, $"Error del sistema: {ex.Message}", statusCode: 500);
            }
        }

        // Helper para decidir si devolver JSON o VISTA
        private IActionResult Respond(bool success, object dataOrMessage, int statusCode = 200)
        {
            bool isJsonRequest = Request.Headers["Accept"].ToString().Contains("application/json") ||
                                 Request.ContentType?.Contains("application/json") == true;

            if (isJsonRequest)
            {
                // MODO APP: Retornamos JSON puro
                if (!success && dataOrMessage is string msg)
                {
                    return StatusCode(statusCode == 200 ? 400 : statusCode, new { Exitoso = false, Mensaje = msg });
                }
                return StatusCode(statusCode, dataOrMessage);
            }
            else
            {
                // MODO WEB CLÁSICA: Retornamos la Vista con el modelo
                if (!success)
                {
                    var msg = dataOrMessage is string s ? s : ((IngresoQrResult)dataOrMessage).Mensaje;
                    ModelState.AddModelError("socioId", msg);
                    // Si es un objeto de resultado fallido, lo pasamos a la vista
                    if (dataOrMessage is IngresoQrResult res) return View("Index", res);
                    return View("Index");
                }

                // Éxito vista
                return View("Index", dataOrMessage);
            }
        }
    }
}