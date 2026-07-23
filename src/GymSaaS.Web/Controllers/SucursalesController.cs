using System.Security.Claims;
using GymSaaS.Application.Sucursales.Commands.CrearSucursal;
using GymSaaS.Application.Sucursales.Queries.GetMisSucursales;
using GymSaaS.Application.Sucursales.Queries.GetResumenEmpresa;
using GymSaaS.Application.Sucursales.Queries.PrepararCambioDeSucursal;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class SucursalesController : Controller
    {
        private readonly IMediator _mediator;

        public SucursalesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public class SucursalesViewModel
        {
            public List<SucursalDto> Sucursales { get; set; } = new();
            public ResumenEmpresaDto Resumen { get; set; } = new();
        }

        // GET: Sucursales
        public async Task<IActionResult> Index()
        {
            var vm = new SucursalesViewModel
            {
                Sucursales = await _mediator.Send(new GetMisSucursalesQuery()),
                Resumen = await _mediator.Send(new GetResumenEmpresaQuery())
            };

            return View(vm);
        }

        // POST: Sucursales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombreSucursal)
        {
            var usuarioIdStr = User.FindFirst("UsuarioId")?.Value;
            if (string.IsNullOrEmpty(usuarioIdStr) || !int.TryParse(usuarioIdStr, out var usuarioId))
                return Unauthorized();

            try
            {
                await _mediator.Send(new CrearSucursalCommand
                {
                    UsuarioActualId = usuarioId,
                    NombreSucursal = nombreSucursal
                });

                TempData["SuccessMessage"] = "Sucursal creada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo crear la sucursal: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Sucursales/Cambiar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cambiar(int tenantId)
        {
            var tenantActualStr = User.FindFirst("TenantId")?.Value;
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(tenantActualStr) || string.IsNullOrEmpty(email) ||
                !int.TryParse(tenantActualStr, out var tenantActualId))
            {
                return Unauthorized();
            }

            var datos = await _mediator.Send(new PrepararCambioDeSucursalQuery(tenantActualId, tenantId, email));
            if (datos == null)
            {
                TempData["Error"] = "No tenés acceso a esa sucursal.";
                return RedirectToAction(nameof(Index));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, datos.Email),
                new Claim("UsuarioId", datos.UsuarioId.ToString()),
                new Claim("Nombre", datos.Nombre),
                new Claim("TenantId", datos.TenantId)
            };

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
