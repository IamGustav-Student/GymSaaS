using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
using GymSaaS.Application.Clases.Commands.CancelarReserva;
using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Commands.UnirseListaEspera;
using GymSaaS.Application.Clases.Queries.GetClasesPortal;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.RenovarMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Pagos.Commands.CrearLinkPagoReserva;
using GymSaaS.Application.Portal.Queries.GetGamificationStats;
using GymSaaS.Application.Rutinas.Queries.GetRutinas;
using GymSaaS.Web.Hubs;
using GymSaaS.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymSaaS.Web.Controllers
{
    public class PortalController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly IHubContext<AccesoHub> _hubContext;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly ILogger<PortalController> _logger;

        public PortalController(
            IApplicationDbContext context,
            IMediator mediator,
            IHubContext<AccesoHub> hubContext,
            ICurrentTenantService currentTenantService,
            ILogger<PortalController> logger)
        {
            _context = context;
            _mediator = mediator;
            _hubContext = hubContext;
            _currentTenantService = currentTenantService;
            _logger = logger;
        }

        // ============================================================
        // 1. LOGIN POR DNI (CORREGIDO: BYPASS DE FILTROS)
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index");

            // ... (Tu lógica de SEO existente se mantiene igual) ...
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                ViewBag.Error = "Por favor ingresa tu DNI.";
                return View();
            }

            var dniLimpio = dni.Trim().Replace(".", "").Replace("-", "");

            // PASO 1: Buscar en TODOS los gimnasios (IgnoreQueryFilters)
            // Incluimos datos del Tenant para mostrar el nombre/logo si hay duplicados
            // NOTA: Como 'Socio' tal vez no tiene la propiedad de navegación 'Tenant' configurada, 
            // hacemos un join manual o asumimos que podemos obtener el nombre después.
            // Para simplificar y ser robustos, traemos la lista cruda primero.

            var sociosEncontrados = await _context.Socios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.Dni == dniLimpio && s.Activo)
                .ToListAsync();

            if (!sociosEncontrados.Any())
            {
                ViewBag.Error = "DNI no encontrado o cuenta inactiva.";
                return View();
            }

            // PASO 2: Decisiones
            if (sociosEncontrados.Count == 1)
            {
                // CASO A: Solo existe en un gimnasio -> Login Directo
                return await ProcesarIngreso(sociosEncontrados.First());
            }
            else
            {
                // CASO B: Existe en varios -> Mostrar pantalla de selección
                var tenantIds = sociosEncontrados.Select(s => s.TenantId).Distinct().ToList();

                // Socio.TenantId almacena el Id numérico del Tenant como string (ej: "1", "2").
                // Por eso convertimos a int y buscamos por Id, NO por Code.
                var tenantIdsNumericos = tenantIds
                    .Select(id => int.TryParse(id, out var n) ? n : -1)
                    .Where(n => n > 0)
                    .ToList();

                var gimnasios = await _context.Tenants
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(t => tenantIdsNumericos.Contains(t.Id))
                    .ToListAsync();

                _logger.LogInformation("[Login] TenantIds buscados: {Ids}. Tenants encontrados: {Count}. Nombres: {Names}",
                    string.Join(", ", tenantIds),
                    gimnasios.Count,
                    string.Join(", ", gimnasios.Select(g => $"{g.Code}={g.Name}")));

                // Mapa con clave = Id como string, igual a como está guardado en Socio.TenantId
                var gimnasiosMap = gimnasios.ToDictionary(
                    t => t.Id.ToString(),
                    t => t
                );

                var opciones = sociosEncontrados.Select(s => new OpcionGimnasioViewModel
                {
                    SocioId = s.Id,
                    NombreGimnasio = gimnasiosMap.TryGetValue(s.TenantId, out var gym)
                        ? gym.Name
                        : $"Gimnasio ({s.TenantId})",
                    LogoUrl = gimnasiosMap.TryGetValue(s.TenantId, out var gymLogo)
                        ? gymLogo.LogoUrl
                        : null,
                    Rol = "Alumno"
                }).ToList();

                return View("SeleccionarGimnasio", opciones);
            }
        }
        private async Task<IActionResult> ProcesarIngreso(GymSaaS.Domain.Entities.Socio socio)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, socio.Dni),
                new Claim("SocioId", socio.Id.ToString()),
                new Claim(ClaimTypes.Role, "Alumno"),
                new Claim("TenantId", socio.TenantId)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index");
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarSeleccion(int socioId)
        {
            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == socioId);

            if (socio == null) return RedirectToAction("Login");

            return await ProcesarIngreso(socio);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ============================================================
        // 2. DASHBOARD & GAMIFICACIÓN
        // ============================================================

        /// <summary>
        /// Dashboard principal del socio.
        /// NOTA JUNIOR: Se corrigió la consulta de Tenant para buscar por ID, 
        /// ya que el socio guarda el ID del gimnasio y no su código de texto.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            // CORRECCIÓN CRÍTICA: El log mostraba que se buscaba por .Code
            // Cambiamos a buscar por .Id que es lo que contiene socio.TenantId
            if (int.TryParse(socio.TenantId, out int tId))
            {
                var tenant = await _context.Tenants
                    .IgnoreQueryFilters() // Saltamos el filtro para poder leer el nombre del gimnasio padre
                    .FirstOrDefaultAsync(t => t.Id == tId);
                
                // Si el gimnasio no existe, ponemos un fallback seguro para que la vista no explote
                ViewBag.GymName = tenant?.Name ?? "Gimnasio Desconocido";
            }
            else 
            {
                ViewBag.GymName = "Gimnasio Desconocido";
            }

            var stats = await _mediator.Send(new GetGamificationStatsQuery { SocioId = socio.Id });
            return View(stats);
        }

        // ============================================================
        // 3. GESTIÓN DE CLASES Y WAITLIST (NUEVO)
        // ============================================================

        [Authorize]
        public async Task<IActionResult> Clases()
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            // Pasamos el SocioId para que la Query (si está bien hecha) sepa qué reservó el usuario
            // Ojo: Si GetClasesPortalQuery no filtra por usuario, la vista tendrá que inferirlo
            var clases = await _mediator.Send(new GetClasesPortalQuery(socio.Id));

            // Pasamos el ID del socio a la vista mediante ViewBag para lógica de botones
            ViewBag.CurrentSocioId = socio.Id;

            return View(clases);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Reservar(int claseId)
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            try
            {
                var resultado = await _mediator.Send(new ReservarClaseCommand
                {
                    ClaseId = claseId,
                    SocioId = socio.Id
                });

                if (resultado.RequierePago)
                {
                    return RedirectToAction("PagoReserva", new { reservaId = resultado.ReservaId });
                }

                TempData["Success"] = "¡Reserva confirmada!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Clases));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UnirseListaEspera(int claseId)
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            try
            {
                await _mediator.Send(new UnirseListaEsperaCommand { ClaseId = claseId, SocioId = socio.Id });
                TempData["Info"] = "Estás en lista de espera. Te avisaremos si se libera un lugar.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al unirse: " + ex.Message;
            }
            return RedirectToAction(nameof(Clases));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelarReserva(int reservaId)
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            try
            {
                // Nota: La vista debe enviarnos el ReservaId correcto.
                // Si la vista solo tiene ClaseId, tendríamos que buscar la reserva aquí primero.
                // Asumiremos que podemos buscar la reserva por ClaseId y SocioId si el comando lo permite,
                // o que la vista nos da el ID. 
                // Para robustez, modificamos el comando para aceptar ReservaId, pero aquí buscamos por contexto si hace falta.

                // Opción robusta: Buscar reserva activa del socio para esa clase (si el form manda claseId)
                // O usar el ID directo si la vista lo tiene. Asumamos que el form manda 'reservaId'.

                await _mediator.Send(new CancelarReservaCommand { ReservaId = reservaId, SocioId = socio.Id });
                TempData["Info"] = "Reserva cancelada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cancelar: " + ex.Message;
            }
            return RedirectToAction(nameof(Clases));
        }

        // ============================================================
        // 4. VISTAS SIMPLES
        // ============================================================

        [Authorize]
        public IActionResult Escanear() { return View(); }

        [Authorize]



        [Authorize]
        public async Task<IActionResult> MisRutinas()
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            // CORRECCIÓN: Buscamos las rutinas del socio y las proyectamos al DTO
            // Esto evita que el modelo sea null en la vista y cause ArgumentNullException
            var rutinas = await _context.Rutinas
                .AsNoTracking()
                .Where(r => r.SocioId == socio.Id)
                .Select(RutinaDto.Projection)
                .ToListAsync();

            return View(rutinas);
        }
        // ============================================================
        // MÓDULO DE RENOVACIÓN Y PAGOS (IMPLEMENTADO)
        // ============================================================

        [Authorize]
        public async Task<IActionResult> Renovar()
        {
            // 1. Obtenemos los planes disponibles (Query existente)
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());
            return View(planes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contratar(int tipoMembresiaId)
        {
            try
            {
                var socio = await GetSocioLogueado();
                if (socio == null) return RedirectToAction("Login");

                // 1. Validar datos mínimos del socio para Mercado Pago
                if (string.IsNullOrEmpty(socio.Email))
                {
                    TempData["Error"] = "Tu perfil no tiene un Email registrado. Es necesario para procesar pagos digitales.";
                    return RedirectToAction("Index");
                }

                // 2. Crear la membresía en estado pendiente
                var command = new ContratarMembresiaCommand(socio.Id, tipoMembresiaId);
                var membresiaSocioId = await _mediator.Send(command);

                // 3. Intentar generar el link de pago
                try
                {
                    var urlPago = await _mediator.Send(new CrearLinkPagoCommand(membresiaSocioId));

                    if (string.IsNullOrEmpty(urlPago))
                    {
                        TempData["Warning"] = "Membresía registrada. El gimnasio no tiene configurado Mercado Pago actualmente. Contacta a la administración.";
                        return RedirectToAction("Index");
                    }

                    return Redirect(urlPago);
                }
                catch (Exception ex)
                {
                    // La membresía ya se creó, pero el pago falló. Informamos al usuario.
                    TempData["Warning"] = "Membresía registrada, pero no pudimos conectar con Mercado Pago: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la solicitud: " + ex.Message;
                return RedirectToAction("Renovar");
            }
        }


        // ============================================================
        // HELPER PRIVADO (MODIFICADO PARA SEGURIDAD)
        // ============================================================
        private async Task<GymSaaS.Domain.Entities.Socio?> GetSocioLogueado()
        {
            // 1. Intentar por ID (Rápido)
            var claimId = User.FindFirst("SocioId")?.Value;
            if (int.TryParse(claimId, out int id))
            {
                return await _context.Socios
                    .IgnoreQueryFilters() // Aseguramos encontrarlo aunque el contexto de tenant falle
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);
            }

            // 2. Fallback por DNI
            var dniUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(dniUsuario)) return null;

            return await _context.Socios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Dni == dniUsuario);
        }
        /// <summary>
        /// PROCESO DE ASISTENCIA: Modificado para ser compatible con el Command que solo tiene SocioId.
        /// NOTA JUNIOR: Quitamos la asignación de QrCode, Latitud y Longitud al Command 
        /// para que no te tire error de compilación.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RegistrarAsistencia([FromBody] RegistrarAsistenciaRequest model)
        {
            try
            {
                var socioIdClaim = User.FindFirst("SocioId")?.Value;
                if (!int.TryParse(socioIdClaim, out int socioId))
                    return Json(new { success = false, message = "Sesión no válida" });

                // EJECUCIÓN DEL COMANDO ORIGINAL (Solo con las propiedades que existen)
                var result = await _mediator.Send(new RegistrarIngresoQrCommand
                {
                    SocioId = socioId
                    // Las propiedades que daban error (QrCode, Latitud, etc) se eliminan de aquí
                });

                // Mantenemos la compatibilidad con el objeto de retorno IngresoQrResult
                if (result.Exitoso)
                {
                    return Json(new
                    {
                        success = true,
                        socioNombre = result.NombreSocio,
                        message = result.Mensaje
                    });
                }

                return Json(new { success = false, message = result.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        // DTO simple para recibir los datos del JSON
        public class RegistrarAsistenciaRequest
    {
        public string QrCode { get; set; } = string.Empty;
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    }

    

    // ViewModel simple (puedes ponerlo en una clase aparte o aquí mismo si es pequeña)

}
