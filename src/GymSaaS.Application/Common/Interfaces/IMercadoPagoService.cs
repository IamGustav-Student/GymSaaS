using MercadoPago.Client.Preference;

namespace GymSaaS.Application.Common.Interfaces
{
    public interface IMercadoPagoService
    {
        Task<string> CrearPreferenciaAsync(PreferenceRequest request);

        // NUEVO MÉTODO: Consultar estado
        Task<string> ObtenerEstadoPagoAsync(string paymentId);
        Task<string> ObtenerExternalReferenceAsync(string paymentId);
    }
}