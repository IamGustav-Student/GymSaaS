using MercadoPago.Client.Preference;
using System.Threading.Tasks;

namespace GymSaaS.Application.Common.Interfaces
{
    public interface IMercadoPagoService
    {
        // Carril Tenant (Gimnasio cobra al alumno)
        Task<string> CrearPreferenciaAsync(PreferenceRequest request);
        Task<string> ObtenerEstadoPagoAsync(string paymentId);
        Task<string> ObtenerExternalReferenceAsync(string paymentId);
        Task<string> ProcesarPago(decimal monto, string numeroTarjeta, string titular);

        // NUEVO: Carril Master (SaaS cobra al gimnasio)
        // Usa credenciales de Variables de Entorno
        Task<string> CrearPreferenciaSaaS(string titulo, decimal precio, string emailGimnasio, string externalReference);
    }
}