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
        public async Task<IActionResult> SelectPlan(PlanType plan)
        {
            // 1. Obtenemos el link de pago desde tu servicio
            // IMPORTANTE: Asegúrate que CrearPreferenciaSaaS devuelva el InitPoint (URL)
            var paymentUrl = await _mercadoPagoService.CrearPreferenciaSaaS(plan, _currentTenantService.TenantId);

            if (string.IsNullOrEmpty(paymentUrl))
            {
                TempData["Error"] = "No se pudo generar el link de pago.";
                return RedirectToAction("Pricing");
            }

            // 2. REDIRECCIÓN MANUAL: Forzamos al navegador a salir de tu sitio hacia MP
            return Redirect(paymentUrl);
        }

        // Callbacks simples para MercadoPago
        public IActionResult Success() => View("Success");
        public IActionResult Failure() => View("Failure");
        public IActionResult Pending() => View("Pending");
    }
}