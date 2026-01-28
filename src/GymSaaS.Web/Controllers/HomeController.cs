using GymSaaS.Application.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize] // ¡Solo usuarios logueados pueden ver el dashboard!
    public class HomeController : Controller
    {
        private readonly IMediator _mediator;

        public HomeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            // Lanzamos la query a la capa Application
            var stats = await _mediator.Send(new GetDashboardStatsQuery());

            return View(stats);
        }

        [AllowAnonymous] // La página de error debe ser pública por si falla el login
        public IActionResult Privacy()
        {
            return View();
        }
    }
}