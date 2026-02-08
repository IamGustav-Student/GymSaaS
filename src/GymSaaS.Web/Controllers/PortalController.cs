using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Queries.GetClasesPortal;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.RenovarMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Pagos.Commands.CrearLinkPagoReserva;
using GymSaaS.Application.Portal.Queries.GetGamificationStats;
using GymSaaS.Web.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GymSaaS.Web.Models;

namespace GymSaaS.Web.Controllers
{
    public class PortalController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly IHubContext<AccesoHub> _hubContext;
        private readonly ICurrentTenantService _currentTenantService;

        public PortalController(
            IApplicationDbContext context,
            IMediator mediator,
            IHubContext<AccesoHub> hubContext,
            ICurrentTenantService currentTenantService)
        {
            _context = context;
            _mediator = mediator;
            _hubContext = hubContext;
            _currentTenantService = currentTenantService;
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
                // Necesitamos los nombres de los gimnasios. 
                // Obtenemos los TenantIds únicos de la lista de socios
                var tenantIds = sociosEncontrados.Select(s => s.TenantId).Distinct().ToList();

                var gimnasios = await _context.Tenants
                    .AsNoTracking()
                    .Where(t => tenantIds.Contains(t.Code)) // Asumiendo que TenantId en Socio es el Code o Id
                    .ToDictionaryAsync(t => t.Code, t => t); // Mapa rápido

                // Preparamos un modelo simple para la vista
                var opciones = sociosEncontrados.Select(s => new OpcionGimnasioViewModel
                {
                    SocioId = s.Id,
                    NombreGimnasio = gimnasios.ContainsKey(s.TenantId) ? gimnasios[s.TenantId].Name : "Gimnasio Desconocido",
                    LogoUrl = gimnasios.ContainsKey(s.TenantId) ? gimnasios[s.TenantId].LogoUrl : null,
                    Rol = "Alumno" // O sacar de membresía si quisieras
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

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var socio = await GetSocioLogueado();
            if (socio == null) return RedirectToAction("Login");

            // Si falla la query de gamificación por filtros, usa IgnoreQueryFilters dentro del Handler también
            // Pero como aquí ya tenemos el TenantId en la cookie (GetSocioLogueado), debería funcionar.

            try
            {
                var stats = await _mediator.Send(new GetGamificationStatsQuery(socio.Id));
                return View(stats);
            }
            catch
            {
                // Fallback si la query falla (para que no rompa la vista)
                return View(new GamificationStatsDto { NombreSocio = socio.Nombre });
            }
        }

        // ============================================================
        // 3. PAGOS Y RESERVAS
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagoReserva(int tipoMembresiaId)
        {
            try
            {
                var socio = await GetSocioLogueado();
                if (socio == null) return RedirectToAction("Login");

                var urlPago = await _mediator.Send(new CrearLinkPagoReservaCommand(socio.Id, tipoMembresiaId));
                return Redirect(urlPago);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error iniciando pago: " + ex.Message;
                return RedirectToAction("Renovar");
            }
        }

        // ============================================================
        // 4. VISTAS SIMPLES
        // ============================================================

        [Authorize]
        public IActionResult Escanear() { return View(); }

        [Authorize]
        public IActionResult Clases() { return View(); }

        [Authorize]
        public IActionResult Renovar() { return View(); }

        [Authorize]
        public IActionResult MisRutinas() { return View(); }

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
        // ViewModel simple (puedes ponerlo en una clase aparte o aquí mismo si es pequeña)
        
    }
}