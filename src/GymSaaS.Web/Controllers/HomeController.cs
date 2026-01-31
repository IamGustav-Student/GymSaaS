using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    // Este controlador NO debe tener [Authorize] porque es la página pública de aterrizaje.
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Muestra la nueva pantalla de bienvenida con los 3 botones
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Acción para manejar errores (por defecto en la plantilla)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}