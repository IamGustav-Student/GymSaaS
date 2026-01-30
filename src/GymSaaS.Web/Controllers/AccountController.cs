using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // 1. Cerramos la sesión usando el esquema de Cookies definido en Program.cs
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Limpiamos la sesión del portal de socios (por seguridad extra)
            HttpContext.Session.Clear();

            _logger.LogInformation("Usuario cerró sesión manualmente.");

            // 3. Redirigimos al Login (Auth/Login según tu Program.cs)
            return RedirectToAction("Login", "Auth");
        }
    }
}