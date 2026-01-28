using GymSaaS.Application.Common.Interfaces;
using MercadoPago.Client.Preference;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Configuration;

namespace GymSaaS.Infrastructure.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly string _defaultToken;

        public MercadoPagoService(IConfiguration configuration)
        {
            _defaultToken = configuration["MercadoPago:AccessToken"] ?? "";
        }

        public async Task<string> CrearPreferenciaAsync(string titulo, decimal precio, string accessToken)
        {
            // Usar token del gimnasio o el default
            MercadoPagoConfig.AccessToken = !string.IsNullOrEmpty(accessToken) ? accessToken : _defaultToken;

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = titulo,
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = precio,
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = "https://tu-gym-saas.railway.app/Pagos/Exito",
                    Failure = "https://tu-gym-saas.railway.app/Pagos/Fallo",
                    Pending = "https://tu-gym-saas.railway.app/Pagos/Pendiente"
                },
                AutoReturn = "approved",
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.InitPoint;
        }

        public async Task<DatosPagoMP> ConsultarPago(string paymentId)
        {
            if (string.IsNullOrEmpty(MercadoPagoConfig.AccessToken))
            {
                MercadoPagoConfig.AccessToken = _defaultToken;
            }

            try
            {
                var client = new PaymentClient();
                var payment = await client.GetAsync(long.Parse(paymentId));

                return new DatosPagoMP(payment.Status, payment.ExternalReference);
            }
            catch
            {
                return new DatosPagoMP("unknown", "");
            }
        }
    }
}