using GymSaaS.Application.Common.Interfaces;
using MercadoPago.Client.Payment; // Necesario para consultar pagos
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;

namespace GymSaaS.Infrastructure.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IConfiguration _configuration;

        public MercadoPagoService(IConfiguration configuration)
        {
            _configuration = configuration;
            MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];
        }

        public async Task<string> CrearPreferenciaAsync(PreferenceRequest request)
        {
            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request);
            return preference.InitPoint;
        }

        // --- NUEVA FUNCIONALIDAD ---
        public async Task<string> ObtenerEstadoPagoAsync(string paymentId)
        {
            if (string.IsNullOrEmpty(paymentId)) return "unknown";

            try
            {
                var client = new PaymentClient();
                // Parseamos el ID a long porque el SDK lo pide así
                if (long.TryParse(paymentId, out long idLong))
                {
                    var payment = await client.GetAsync(idLong);
                    return payment.Status; // "approved", "pending", "rejected"
                }
                return "error";
            }
            catch
            {
                return "error";
            }
        }

        public async Task<string> ObtenerExternalReferenceAsync(string paymentId)
        {
            if (string.IsNullOrEmpty(paymentId)) return "";

            try
            {
                var client = new PaymentClient();
                if (long.TryParse(paymentId, out long idLong))
                {
                    var payment = await client.GetAsync(idLong);
                    return payment.ExternalReference; // Aquí vendrá nuestro MembresiaId
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}