using GymSaaS.Application.Common.Interfaces;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
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
        private readonly IEncryptionService _encryptionService; // Inyección de seguridad

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

            // 1. Buscamos configuración encriptada en DB
            var config = await _context.ConfiguracionesPagos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Activo);

            if (config != null && !string.IsNullOrEmpty(config.AccessToken))
            {
                // 2. DESENCRIPTAR AL VUELO
                string tokenReal = _encryptionService.Decrypt(config.AccessToken);

                if (!string.IsNullOrEmpty(tokenReal))
                {
                    MercadoPagoConfig.AccessToken = tokenReal;
                    return;
                }
            }

            // Fallback o manejo de error en producción
            // throw new Exception("El gimnasio no tiene credenciales de pago válidas.");
        }

        public async Task<string> CrearPreferenciaAsync(PreferenceRequest request)
        {
            await ConfigurarCredencialesAsync();

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.InitPoint;
        }

        // =========================================================
        // CARRIL MASTER (SaaS): Uso de Variables de Entorno
        // =========================================================
        public async Task<string> CrearPreferenciaSaaS(string titulo, decimal precio, string emailGimnasio, string externalReference)
        {
            // 1. Leer Token Maestro desde IConfiguration (Appsettings o Variables de Entorno)
            // En producción, esto viene de ENV: MercadoPago__AccessToken
            var masterToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrEmpty(masterToken))
            {
                throw new Exception("Error Crítico SaaS: Credenciales maestras no configuradas en el entorno.");
            }

            // 2. Usar credenciales maestras (Sobrescribe contexto)
            MercadoPagoConfig.AccessToken = masterToken;

            // 3. Crear preferencia
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
                // Las URLs de retorno deberían ser configurables también
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
            await ConfigurarCredencialesAsync();
            return "approved"; // Mock para desarrollo
        }

        public async Task<string> ObtenerExternalReferenceAsync(string paymentId)
        {
            await ConfigurarCredencialesAsync();
            return "REF_MOCK";
        }

        public async Task<string> ProcesarPago(decimal monto, string numeroTarjeta, string titular)
        {
            await ConfigurarCredencialesAsync();

            // MOCK / SIMULACIÓN DE COBRO
            await Task.Delay(500);
            return "mp_trans_" + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}