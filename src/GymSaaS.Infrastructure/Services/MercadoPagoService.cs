using GymSaaS.Application.Common.Interfaces;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GymSaaS.Infrastructure.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly IConfiguration _configuration;
        private readonly IEncryptionService _encryptionService;

        public MercadoPagoService(
            IApplicationDbContext context,
            ICurrentTenantService tenantService,
            IConfiguration configuration,
            IEncryptionService encryptionService)
        {
            _context = context;
            _tenantService = tenantService;
            _configuration = configuration;
            _encryptionService = encryptionService;
        }

        private async Task ConfigurarCredencialesAsync()
        {
            var tenantId = _tenantService.TenantId;

            var config = await _context.ConfiguracionesPagos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Activo);

            if (config != null && !string.IsNullOrEmpty(config.AccessToken))
            {
                string tokenReal = _encryptionService.Decrypt(config.AccessToken);

                if (!string.IsNullOrEmpty(tokenReal))
                {
                    MercadoPagoConfig.AccessToken = tokenReal;
                    return;
                }
            }
            // Si llegamos aquí, no hay token configurado para el gimnasio
            MercadoPagoConfig.AccessToken = string.Empty;
        }

        public async Task<string> CrearPreferenciaAsync(PreferenceRequest request)
        {
            await ConfigurarCredencialesAsync();

            if (string.IsNullOrEmpty(MercadoPagoConfig.AccessToken))
                return string.Empty;

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.InitPoint;
        }

        public async Task<string> CrearPreferenciaSaaS(string titulo, decimal precio, string emailGimnasio, string externalReference)
        {
            var masterToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrEmpty(masterToken))
            {
                throw new Exception("Error Crítico SaaS: Credenciales maestras no configuradas en el entorno.");
            }

            MercadoPagoConfig.AccessToken = masterToken;

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
                Payer = new PreferencePayerRequest
                {
                    Email = emailGimnasio
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{_configuration["App:BaseUrl"]}/Subscription/Success",
                    Failure = $"{_configuration["App:BaseUrl"]}/Subscription/Failure",
                    Pending = $"{_configuration["App:BaseUrl"]}/Subscription/Pending"
                },
                AutoReturn = "approved",
                ExternalReference = externalReference
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.InitPoint;
        }

        public async Task<string> ObtenerEstadoPagoAsync(string paymentId)
        {
            try
            {
                await ConfigurarCredencialesAsync();

                var client = new PaymentClient();
                Payment payment = await client.GetAsync(long.Parse(paymentId));

                return payment.Status;
            }
            catch (Exception ex)
            {
                // Log de error (En producción usar un ILogger)
                Console.WriteLine($"Error al obtener estado de pago {paymentId}: {ex.Message}");
                return "error";
            }
        }

        public async Task<string> ObtenerExternalReferenceAsync(string paymentId)
        {
            try
            {
                await ConfigurarCredencialesAsync();

                var client = new PaymentClient();
                Payment payment = await client.GetAsync(long.Parse(paymentId));

                return payment.ExternalReference;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener referencia externa de pago {paymentId}: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string> ProcesarPago(decimal monto, string numeroTarjeta, string titular)
        {
            try
            {
                await ConfigurarCredencialesAsync();
                var client = new PaymentClient();
                var payment = await client.CreateAsync(new PaymentCreateRequest { TransactionAmount = monto });
                return payment.Id.ToString();
            }
            catch
            {
                return "error";
            }
        }
    }
}