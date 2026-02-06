using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Queries.GetClasesPortal;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Pagos.Commands.CrearLinkPago;
using GymSaaS.Application.Pagos.Commands.CrearLinkPagoReserva;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Controllers
{
    public class PortalController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;

        public PortalController(IApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        // 1. Pantalla de Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                ViewBag.Error = "Por favor ingrese su DNI.";
                return View();
            }

            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Dni == dni && !s.IsDeleted);

            if (socio == null)
            {
                ViewBag.Error = "No encontramos un socio con ese DNI.";
                return View();
            }

            // CORRECCIÓN 1: Guardamos TenantId como STRING para evitar conflicto de tipos
            HttpContext.Session.SetInt32("SocioPortalId", socio.Id);
            HttpContext.Session.SetString("SocioTenantId", socio.TenantId);

            return RedirectToAction(nameof(Index));
        }

        // 2. Dashboard del Socio
        public async Task<IActionResult> Index()
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            var socio = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .FirstOrDefaultAsync(s => s.Id == socioId.Value);

            if (socio == null) return RedirectToAction(nameof(Login));

            var membresiaActiva = socio.Membresias
                .Where(m => m.Activa && m.FechaFin >= DateTime.Now)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefault();

            ViewBag.Estado = membresiaActiva != null ? "Activo" : "Vencido";
            ViewBag.Vencimiento = membresiaActiva?.FechaFin.ToString("dd/MM/yyyy") ?? "-";
            ViewBag.DiasRestantes = membresiaActiva != null ? (membresiaActiva.FechaFin - DateTime.Now).Days : 0;

            return View(socio);
        }

        // 3. Salir
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // 4. Iniciar Renovación
        public async Task<IActionResult> Renovar()
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            // CORRECCIÓN 2: Recuperamos TenantId como STRING
            var tenantId = HttpContext.Session.GetString("SocioTenantId");

            if (string.IsNullOrEmpty(tenantId)) return RedirectToAction(nameof(Login));

            // CORRECCIÓN 3: Eliminamos '&& t.Activo' y comparamos strings correctamente
            var planes = await _context.TiposMembresia
                .IgnoreQueryFilters()
                .Where(t => t.TenantId == tenantId)
                .ToListAsync();

            return View(planes);
        }

        // 5. Generar Pago
        [HttpPost]
        public async Task<IActionResult> ConfirmarRenovacion(int planId)
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            var command = new AsignarMembresiaCommand
            {
                SocioId = socioId.Value,
                TipoMembresiaId = planId,
                MetodoPago = "MercadoPago"
            };

            var membresiaId = await _mediator.Send(command);
            var link = await _mediator.Send(new CrearLinkPagoCommand(membresiaId));

            return Redirect(link);
        }
        // GET: Portal/MisRutinas
        public async Task<IActionResult> MisRutinas()
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            // 1. Obtenemos TODAS las rutinas (Idealmente deberíamos tener un Query GetRutinasBySocio, 
            // pero para no romper nada, traemos todas y filtramos aquí)
            var todasLasRutinas = await _mediator.Send(new GymSaaS.Application.Rutinas.Queries.GetRutinas.GetRutinasQuery());

            // 2. Filtramos solo las de este socio y las ordenamos por nombre 
            // (Así si el coach pone "1. Lunes", "2. Martes", salen en orden)
            var misRutinas = todasLasRutinas
                .Where(r => r.SocioId == socioId.Value)
                .OrderBy(r => r.Nombre)
                .ToList();

            return View(misRutinas);
        }
        // GET: Portal/Clases
        public async Task<IActionResult> Clases()
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            var clases = await _mediator.Send(new GetClasesPortalQuery(socioId.Value));
            return View(clases);
        }

        // POST: Portal/Reservar
        [HttpPost]
        public async Task<IActionResult> Reservar(int claseId)
        {
            var socioId = HttpContext.Session.GetInt32("SocioPortalId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            try
            {
                var resultado = await _mediator.Send(new ReservarClaseCommand
                {
                    ClaseId = claseId,
                    SocioId = socioId.Value
                });

                // Si hay que pagar, vamos a la pantalla de confirmación (reutilizando lógica visual)
                if (resultado.RequierePago)
                {
                    return RedirectToAction("PagoReserva", new { reservaId = resultado.ReservaId });
                }

                TempData["Success"] = "¡Reserva confirmada!";
                return RedirectToAction(nameof(Clases));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Clases));
            }
        }

        // GET: Portal/PagoReserva/5
        public IActionResult PagoReserva(int reservaId)
        {
            return View(reservaId);
        }

        // POST: Portal/IrAMercadoPago
        [HttpPost]
        public async Task<IActionResult> IrAMercadoPago(int reservaId)
        {
            try
            {
                var url = await _mediator.Send(new CrearLinkPagoReservaCommand(reservaId));
                return Redirect(url);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error MercadoPago: " + ex.Message;
                return RedirectToAction(nameof(Clases));
            }
        }

    }
}