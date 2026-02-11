using GymSaaS.Application.Tenants.Commands.SelectPlan;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize] // Aseguramos que solo el admin compre
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
            // Verificamos si viene redirigido por expiración (Middleware)
            if (Request.Query.ContainsKey("reason") && Request.Query["reason"] == "expired")
            {
                ViewBag.ErrorMessage = "Tu periodo de prueba o suscripción ha vencido. Por favor selecciona un plan para continuar.";
            }

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
                // Usamos ViewBag para consistencia con la vista Pricing.cshtml
                ViewBag.ErrorMessage = $"Error al procesar el plan: {ex.Message}";
                return View("Pricing");
            }
        }

        // Callbacks simples para MercadoPago
        public IActionResult Success() => View("Success");
        public IActionResult Failure() => View("Failure");
        public IActionResult Pending() => View("Pending");
    }
}