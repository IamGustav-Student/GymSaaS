using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        // Identidad
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Slug/Subdominio

        // Estado
        public string SubscriptionPlan { get; set; } = "Basic";
        public bool IsActive { get; set; } = true;

        // Configuración Visual
        public string? LogoUrl { get; set; }
        public string? WebSiteUrl { get; set; }

        // Integraciones
        public string? MercadoPagoAccessToken { get; set; }

        // --- FASE 3: SELF CHECK-IN & GEO ---
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public int RadioPermitidoMetros { get; set; } = 100;
        public string CodigoQrGym { get; set; } = Guid.NewGuid().ToString();

        // --- FASE 3: INTERNACIONALIZACIÓN ---
        // Vital para validar horarios correctamente según el país del gimnasio
        public string TimeZoneId { get; set; } = "Argentina Standard Time";
    }
}