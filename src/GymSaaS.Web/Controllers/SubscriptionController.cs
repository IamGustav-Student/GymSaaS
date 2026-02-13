using GymSaaS.Application.Tenants.Commands.SelectPlan;
using GymSaaS.Application.Common.Interfaces;
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
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICurrentTenantService _currentTenantService;

        public SubscriptionController(
            IMediator mediator,
            IMercadoPagoService mercadoPagoService,
            ICurrentTenantService currentTenantService)
        {
            _mediator = mediator;
            _mercadoPagoService = mercadoPagoService;
            _currentTenantService = currentTenantService;
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
            try
            {
                // Ejecutamos el comando para obtener la URL de MercadoPago
                var command = new SelectPlanCommand(plan);
                var paymentUrl = await _mediator.Send(command);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    TempData["Error"] = "No se pudo generar el link de pago.";
                    return RedirectToAction("Pricing");
                }

                // Si es un plan gratuito, el Command devuelve la ruta local del Dashboard
                if (paymentUrl.StartsWith("/"))
                {
                    return Redirect(paymentUrl);
                }

                // Para planes pagos, redirigimos a MercadoPago
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la selección del plan: " + ex.Message;
                return RedirectToAction("Pricing");
            }
        }

        // Callbacks simples para MercadoPago
        public IActionResult Success() => View("Success");
        public IActionResult Failure() => View("Failure");
        public IActionResult Pending() => View("Pending");
    }
}