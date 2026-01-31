using GymSaaS.Application.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize] // SOLO dueños logueados pueden entrar aquí
    public class DashboardController : Controller
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            // Ejecutamos la consulta de estadísticas reales
            var stats = await _mediator.Send(new GetDashboardStatsQuery());
            return View(stats);
        }
    }
}