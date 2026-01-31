using GymSaaS.Application.Auth.Commands.Login;
using GymSaaS.Application.Auth.Commands.RegisterTenant;
using GymSaaS.Web.Models; // Asegúrate de que aquí esté tu LoginViewModel
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymSaaS.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            // Si ya está logueado, lo mandamos directo al Dashboard (Panel Privado)
            // NO al Home (que ahora es público)
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // 1. Mapeamos del ViewModel (Web) al Command (Application)
                var command = new LoginUsuarioCommand
                {
                    Email = model.Email,
                    Password = model.Password
                };

                // 2. Enviamos a MediatR y esperamos el resultado
                var result = await _mediator.Send(command);

                // 3. Construimos la identidad (Tus Claims originales)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Email),
                    new Claim("UsuarioId", result.UsuarioId.ToString()),
                    new Claim("Nombre", result.Nombre),
                    new Claim("TenantId", result.TenantId) // ¡Vital para el Multi-Tenant!
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });

                // 4. CAMBIO CLAVE: Redirigir al Dashboard
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception)
            {
                // Si falla (contraseña mal), mostramos error en la misma vista
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
            }
        }

        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // Limpiamos sesión por seguridad

            // Redirigimos al Login nuevamente
            return RedirectToAction("Login");
        }

        // ============================================================
        // REGISTRO (Lo mantenemos para el flujo completo)
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterTenantViewModel command)
        {
            if (!ModelState.IsValid) return View(command);

            try
            {
                // Aquí iría await _mediator.Send(command);
                // Por ahora simulamos éxito
                TempData["SuccessMessage"] = "Cuenta creada exitosamente. Inicia sesión.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al registrar: " + ex.Message);
                return View(command);
            }
        }
    }
}