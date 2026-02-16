using MercadoPago.Client.Preference;
using System.Threading.Tasks;

namespace GymSaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Interfaz para el manejo de pagos. 
    /// Define los contratos para cobrar a los socios y para la suscripción SaaS.
    /// </summary>
    public interface IMercadoPagoService
    {
        /// <summary>
        /// Crea una preferencia de pago estándar (generalmente para socios del gimnasio).
        /// </summary>
        Task<string> CrearPreferenciaAsync(PreferenceRequest request);

        /// <summary>
        /// Crea una preferencia de pago para el dueño del gimnasio (Suscripción al sistema).
        /// Utiliza las credenciales maestras de la plataforma.
        /// </summary>
        Task<string> CrearPreferenciaSaaS(string titulo, decimal precio, string emailGimnasio, string externalReference);

        /// <summary>
        /// Consulta el estado actual de un pago en Mercado Pago.
        /// </summary>
        Task<string> ObtenerEstadoPagoAsync(string paymentId);

        /// <summary>
        /// Obtiene la referencia externa para identificar a qué entidad pertenece el pago.
        /// </summary>
        Task<string> ObtenerExternalReferenceAsync(string paymentId);

        /// <summary>
        /// Procesa un pago directo (opcional según implementación).
        /// </summary>
        Task<string> ProcesarPago(decimal monto, string numeroTarjeta, string titular);
    }
}