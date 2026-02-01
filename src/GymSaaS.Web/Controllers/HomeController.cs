using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    public class HomeController : Controller
    {
        // Pantalla Pública de Bienvenida (Landing)
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}