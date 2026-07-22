using GymSaaS.Application.Accesos.Commands.RegistrarAcceso;
using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class AccesosController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public AccesosController(IMediator mediator, IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _mediator = mediator;
            _context = context;
            _currentTenantService = currentTenantService;
        }

        // GET: Accesos
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ConfiguracionQr()
        {
            var tenantId = _currentTenantService.TenantId;

            // Buscamos los datos del gimnasio para mostrar en el impreso
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null) return NotFound();

            // El contenido del QR será el ID o el Código del gimnasio
            // El Portal de Socios usará esto para saber a qué gym le está "pegando"
            ViewBag.QrContent = tenant.Id.ToString();
            ViewBag.GymName = tenant.Name;

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

        // GET: Accesos/Molinete
        // Pantalla de recepción para gimnasios grandes: identifica al socio por
        // DNI o código de acceso (tarjeta/QR estático leído como teclado por un
        // lector de código de barras/RFID en el molinete). No requiere GPS ni
        // cámara porque el dispositivo ya está físicamente en el gimnasio.
        public IActionResult Molinete()
        {
            return View();
        }

        // POST: Accesos/RegistrarMolinete
        [HttpPost]
        public async Task<IActionResult> RegistrarMolinete([FromBody] RegistrarMolineteDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Input))
            {
                return BadRequest(new { Mensaje = "Ingresá un DNI o código de acceso válido." });
            }

            var resultado = await _mediator.Send(new RegistrarAccesoCommand(dto.Input.Trim()));
            return Ok(resultado);
        }

        // --- PWA OFFLINE SYNC ---
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RegistrarAsistenciaOffline([FromBody] AsistenciaOfflineDto dto)
        {
            if (dto == null || dto.SocioId == 0) return BadRequest("Datos inválidos");

            // Creamos la asistencia directamente en la DB
            // En un sistema real, quizás se validaría nuevamente, pero aquí confiamos en la validación local
            var asistencia = new Asistencia
            {
                SocioId = dto.SocioId,
                FechaHora = DateTime.Parse(dto.FechaHora),
                Detalle = "Registro Sincronizado Offline (PWA)",
                Permitido = true,
                Tipo = "QR Offline"
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync(CancellationToken.None);

            return Ok(new { success = true });
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

    public class AsistenciaOfflineDto
    {
        public int SocioId { get; set; }
        public string FechaHora { get; set; } = string.Empty;
    }

    public class RegistrarMolineteDto
    {
        public string Input { get; set; } = string.Empty;
    }
}