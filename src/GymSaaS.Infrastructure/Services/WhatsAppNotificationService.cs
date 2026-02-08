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
            _logger.LogWarning($"[WHATSAPP MOCK] A: {telefono} | Mensaje: Hola {nombreUsuario}, tu pago falló. Reintentaremos el {fechaReintento:dd/MM}.");
            return Task.CompletedTask;
        }

        public Task EnviarConfirmacionPago(string nombreUsuario, string telefono, decimal monto)
        {
            _logger.LogInformation($"[WHATSAPP MOCK] A: {telefono} | Mensaje: Hola {nombreUsuario}, recibimos tu pago de ${monto}. ¡Gracias!");
            return Task.CompletedTask;
        }

        public Task EnviarNotificacion(string telefono, string mensaje)
        {
            _logger.LogInformation($"[WHATSAPP MOCK] A: {telefono} | Mensaje: {mensaje}");
            return Task.CompletedTask;
        }
    }
}