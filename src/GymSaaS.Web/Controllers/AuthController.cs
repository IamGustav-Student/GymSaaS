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

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Envio el comando a la capa Application
                var query = new LoginUsuarioCommand
                {
                    Email = model.Email,
                    Password = model.Password
                };

                // Recibo el DTO con los datos seguros
                var result = await _mediator.Send(query);

                // Construyo la identidad del usuario para la Cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Email),
                    new Claim("UsuarioId", result.UsuarioId.ToString()),
                    new Claim("Nombre", result.Nombre),
                    new Claim("TenantId", result.TenantId) // <--- ¡LA CLAVE DEL MULTI-TENANT!
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
            }
        }

        // --- REGISTRO DE GIMNASIO ---
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterTenantViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterTenantViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var command = new RegisterTenantCommand
                {
                    GymName = model.GymName,
                    AdminName = model.AdminName,
                    AdminEmail = model.AdminEmail,
                    Password = model.Password
                };

                await _mediator.Send(command);

                // Auto-login o redirigir a login
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Error al registrar: " + ex.Message;
                return View(model);
            }
        }

        // --- LOGOUT ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}