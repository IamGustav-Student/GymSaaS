using GymSaaS.Application.Auth.Commands.Login;
using GymSaaS.Application.Auth.Commands.RegisterTenant;
using GymSaaS.Web.Models;
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
                var command = new LoginUsuarioCommand
                {
                    Email = model.Email,
                    Password = model.Password
                };

                var result = await _mediator.Send(command);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Email),
                    new Claim("UsuarioId", result.UsuarioId.ToString()),
                    new Claim("Nombre", result.Nombre),
                    new Claim("TenantId", result.TenantId) // El GUID del Tenant
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });

                return RedirectToAction("Index", "Dashboard");
            }
            catch (UnauthorizedAccessException)
            {
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Ocurrió un error al iniciar sesión.";
                // Loguear ex
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
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ============================================================
        // REGISTRO (IMPLEMENTACIÓN REAL)
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterTenantViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // 1. Mapeo ViewModel -> Command
                var command = new RegisterTenantCommand
                {
                    GymName = model.GymName,
                    AdminName = model.AdminName,
                    AdminEmail = model.AdminEmail,
                    Password = model.Password
                };

                // 2. EJECUCIÓN REAL (Esto faltaba)
                // Esto llamará al RegisterTenantCommandHandler que arreglamos antes
                await _mediator.Send(command);

                // 3. Éxito
                TempData["SuccessMessage"] = "Cuenta creada exitosamente. Por favor inicia sesión.";
                return RedirectToAction(nameof(Login));
            }
            catch (FluentValidation.ValidationException valEx)
            {
                // Errores de regla de negocio (ej: Email ya existe, password corto)
                foreach (var error in valEx.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                return View(model);
            }
            catch (Exception ex)
            {
                // Error genérico de base de datos u otro
                ModelState.AddModelError("", "Error crítico al registrar: " + ex.Message);
                return View(model);
            }
        }
    }
}