using GymSaaS.Application.Common.Interfaces;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GymSaaS.Infrastructure.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly IConfiguration _configuration;

        public MercadoPagoService(IApplicationDbContext context, ICurrentTenantService tenantService, IConfiguration configuration)
        {
            _context = context;
            _tenantService = tenantService;
            _configuration = configuration;
        }

        private async Task ConfigurarCredencialesAsync()
        {
            var tenantId = _tenantService.TenantId;

            // 1. Buscamos si este gimnasio cargó sus claves
            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Activo);

            if (config != null && !string.IsNullOrEmpty(config.AccessToken))
            {
                MercadoPagoConfig.AccessToken = config.AccessToken;
            }
            else
            {
                // Para desarrollo, permitimos continuar sin credenciales reales
                // En producción aquí lanzaríamos excepción o usaríamos credenciales fallback
            }
        }

        public async Task<string> CrearPreferenciaAsync(PreferenceRequest request)
        {
            await ConfigurarCredencialesAsync();

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

        // --- MÉTODO AGREGADO PARA CORREGIR ERROR CS0535 ---
        public async Task<string> ProcesarPago(decimal monto, string numeroTarjeta, string titular)
        {
            await ConfigurarCredencialesAsync();

            // MOCK / SIMULACIÓN DE COBRO
            // Aquí iría la llamada real al PaymentClient de MercadoPago SDK

            await Task.Delay(500); // Simulamos latencia de red

            // Retornamos un ID de transacción simulado
            return "mp_trans_" + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}