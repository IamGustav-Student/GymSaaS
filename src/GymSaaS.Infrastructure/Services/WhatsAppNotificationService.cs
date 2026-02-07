using GymSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Infrastructure.Services
{
    public class WhatsAppNotificationService : INotificationService
    {
        private readonly ILogger<WhatsAppNotificationService> _logger;

        public WhatsAppNotificationService(ILogger<WhatsAppNotificationService> logger)
        {
            _logger = logger;
        }

        public Task EnviarAlertaPagoFallido(string nombreUsuario, string telefono, DateTime fechaReintento)
        {
            // FASE 1: Simulación en Log
            // FASE 2: Aquí integraríamos Twilio o Meta API
            _logger.LogWarning($"[WHATSAPP MOCK] A: {telefono} | Mensaje: Hola {nombreUsuario}, tu pago ha fallado por fondos insuficientes. No te preocupes, el sistema intentará procesarlo nuevamente el {fechaReintento:dd/MM/yyyy}. Por favor asegura tener saldo.");
            return Task.CompletedTask;
        }

        public Task EnviarConfirmacionPago(string nombreUsuario, string telefono, decimal monto)
        {
            _logger.LogInformation($"[WHATSAPP MOCK] A: {telefono} | Mensaje: Hola {nombreUsuario}, recibimos tu pago de ${monto}. ¡Gracias!");
            return Task.CompletedTask;
        }
    }
}