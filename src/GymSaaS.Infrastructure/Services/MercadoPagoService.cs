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
    /// <summary>
    /// Implementación de Mercado Pago con soporte Híbrido:
    /// 1. Cobros de Gimnasio a Socios (Usa credenciales del Tenant).
    /// 2. Cobros de Plataforma a Dueños (Usa credenciales maestras).
    /// </summary>
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

        /// <summary>
        /// Configura el SDK con el token del gimnasio actual para cobros internos.
        /// </summary>
        private async Task ConfigurarCredencialesAsync()
        {
            var tenantIdStr = _tenantService.TenantId;
            if (string.IsNullOrEmpty(tenantIdStr)) return;

            // Buscamos la configuración de pago activa para este gimnasio específico
            var config = await _context.ConfiguracionesPagos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantIdStr && c.Activo);

            if (config != null && !string.IsNullOrEmpty(config.AccessToken))
            {
                // Desencriptamos la llave por seguridad
                string tokenReal = _encryptionService.Decrypt(config.AccessToken);

                if (!string.IsNullOrEmpty(tokenReal))
                {
                    MercadoPagoConfig.AccessToken = tokenReal;
                    return;
                }
            }
            // Si no hay configuración, limpiamos para evitar cobrar con credenciales equivocadas
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

        /// <summary>
        /// MÉTODO CRÍTICO: Crea el link para que el dueño del gimnasio pague su suscripción SaaS.
        /// </summary>
        public async Task<string> CrearPreferenciaSaaS(string titulo, decimal precio, string emailGimnasio, string externalReference)
        {
            // Obtenemos el Token Maestro de Programador GS desde appsettings.json o variables de entorno
            var masterToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrEmpty(masterToken))
            {
                throw new Exception("Error Crítico SaaS: Credenciales maestras no configuradas en el entorno.");
            }

            // Forzamos el uso del token maestro para este proceso
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
                    // URLs de retorno configuradas en la Capa Web
                    Success = $"{_configuration["App:BaseUrl"]}/Subscription/Success",
                    Failure = $"{_configuration["App:BaseUrl"]}/Subscription/Failure",
                    Pending = $"{_configuration["App:BaseUrl"]}/Subscription/Pending"
                },
                AutoReturn = "approved",
                // Usamos el ExternalReference para identificar al Tenant cuando Mercado Pago nos avise del éxito
                ExternalReference = externalReference
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            // Devolvemos el link de pago (InitPoint)
            return preference.InitPoint;
        }

        public async Task<string> ObtenerEstadoPagoAsync(string paymentId)
        {
            try
            {
                // Primero intentamos con credenciales del tenant, si falla, el flujo de webhook debe manejarlo
                await ConfigurarCredencialesAsync();

                var client = new PaymentClient();
                Payment payment = await client.GetAsync(long.Parse(paymentId));

                return payment.Status;
            }
            catch (Exception ex)
            {
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
                // Simulación de creación de pago directo
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