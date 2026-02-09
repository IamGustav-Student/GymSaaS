using GymSaaS.Application.Tenants.Commands.SelectPlan;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly IMediator _mediator;

        public SubscriptionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public IActionResult Pricing()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectPlan(PlanType plan)
        {
            try
            {
                // El comando devuelve una URL (interna o de MercadoPago)
                var urlDestino = await _mediator.Send(new SelectPlanCommand(plan));

                return Redirect(urlDestino);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al seleccionar el plan: " + ex.Message;
                return RedirectToAction(nameof(Pricing));
            }
        }

        // Callbacks simples para MercadoPago (Frontend feedback)
        public IActionResult Success() => View("Success"); // Puedes crear estas vistas simples luego
        public IActionResult Failure() => View("Failure");
        public IActionResult Pending() => View("Pending");
    }
}