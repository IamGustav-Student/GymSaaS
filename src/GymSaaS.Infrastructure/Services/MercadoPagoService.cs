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
        private readonly IConfiguration _configuration; // Fallback opcional

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
                // USAMOS LA BILLETERA DEL GIMNASIO
                MercadoPagoConfig.AccessToken = config.AccessToken;
            }
            else
            {
                // ERROR: El dueño no configuró MP.
                // Opcional: Usar credenciales maestras del SaaS si quieres cobrar tú y luego transferirles (No recomendado para empezar).
                throw new Exception("El gimnasio no tiene configurado MercadoPago. Contacte al administrador.");
            }
        }

        public async Task<string> CrearPreferenciaAsync(PreferenceRequest request)
        {
            // Paso vital: Configurar SDK con la clave del cliente actual
            await ConfigurarCredencialesAsync();

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.InitPoint; // Retorna el Link de Pago
        }

        // ... Implementa los otros métodos (ObtenerEstadoPagoAsync, etc) llamando siempre a ConfigurarCredencialesAsync() al principio.

        public async Task<string> ObtenerEstadoPagoAsync(string paymentId)
        {
            await ConfigurarCredencialesAsync();
            // ... lógica de consultar pago (PaymentClient)
            // Nota: Necesitarás implementar la lógica de consulta real de MP aquí
            return "approved"; // Simplificado para el ejemplo
        }

        public async Task<string> ObtenerExternalReferenceAsync(string paymentId)
        {
            await ConfigurarCredencialesAsync();
            // ... lógica
            return "123"; // Simplificado
        }
    }
}