using GymSaaS.Application.Clases.Commands.ReservarClase;
using GymSaaS.Application.Clases.Queries.GetClasesPortal;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Membresias.Commands.AsignarMembresia;
using GymSaaS.Application.Membresias.Commands.RenovarMembresia;
using GymSaaS.Application.Membresias.Queries.GetTiposMembresia;
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
                ViewBag.Error = "DNI no encontrado.";
                return View();
            }

            // Guardamos sesión
            HttpContext.Session.SetInt32("SocioId", socio.Id);
            HttpContext.Session.SetString("SocioNombre", $"{socio.Nombre} {socio.Apellido}");
            HttpContext.Session.SetString("TenantId", socio.TenantId);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // 2. Dashboard del Alumno
        public async Task<IActionResult> Index()
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            return View();
        }

        // 3. Ver Clases Disponibles
        public async Task<IActionResult> Clases()
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            // CORRECCIÓN: Pasamos el socioId al constructor de la Query
            var clases = await _mediator.Send(new GetClasesPortalQuery(socioId.Value));
            return View(clases);
        }

        [HttpPost]
        public async Task<IActionResult> Reservar(int claseId)
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            try
            {
                var resultado = await _mediator.Send(new ReservarClaseCommand
                {
                    ClaseId = claseId,
                    SocioId = socioId.Value
                });

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

        public IActionResult PagoReserva(int reservaId)
        {
            return View(reservaId);
        }

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

        public async Task<IActionResult> Acceso()
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == socioId.Value);

            return View(socio);
        }

        // ==========================================
        //  NUEVA FUNCIONALIDAD: RENOVAR MEMBRESÍA
        // ==========================================

        // GET: Muestra la tienda de planes
        public async Task<IActionResult> Renovar()
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            // Reutilizamos la Query existente para obtener los planes vigentes
            var planes = await _mediator.Send(new GetTiposMembresiaQuery());

            return View(planes);
        }

        // POST: Procesa la elección y redirige a MP
        [HttpPost]
        public async Task<IActionResult> LinkPago(int planId)
        {
            var socioId = HttpContext.Session.GetInt32("SocioId");
            if (!socioId.HasValue) return RedirectToAction(nameof(Login));

            try
            {
                // 1. Crear la intención de renovación (Lógica Stacking)
                var nuevaMembresiaId = await _mediator.Send(new RenovarMembresiaCommand
                {
                    SocioId = socioId.Value,
                    TipoMembresiaId = planId
                });

                // 2. Generar el link de pago para esa membresía específica
                var urlPago = await _mediator.Send(new CrearLinkPagoCommand(nuevaMembresiaId));

                // 3. Redirigir al usuario a pagar
                return Redirect(urlPago);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la renovación: " + ex.Message;
                return RedirectToAction(nameof(Renovar));
            }
        }
    }
}