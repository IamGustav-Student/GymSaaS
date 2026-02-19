using System.Threading.Tasks;

namespace GymSaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de mensajería de WhatsApp.
    /// </summary>
    public interface IWhatsAppService
    {
        Task<bool> SendTextMessageAsync(string phoneNumber, string message);
        Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, string languageCode, object[] parameters);
    }
}