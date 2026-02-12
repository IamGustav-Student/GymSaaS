using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Tenant : BaseEntity
    {

        // Identidad
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Slug/Subdominio

        // Estado
        public string SubscriptionPlan { get; set; }
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

        // ==========================================
        // PROPIEDADES SAAS (MODELO DE SUSCRIPCIÓN)
        // ==========================================
        public PlanType Plan { get; set; } = PlanType.PruebaGratuita;

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

        /// <summary>
        /// Límite de socios activos permitidos. Null indica ilimitado.
        /// </summary>
        public int? MaxSocios { get; set; }

        public DateTime? TrialEndsAt { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }

        /// <summary>
        /// ID de referencia para la suscripción recurrente (Preapproval ID de MercadoPago)
        /// </summary>
        public string? MercadoPagoSubscriptionId { get; set; }

        // --- MERCADOPAGO CONNECT (Cobro del Gimnasio) ---
        // Estos campos guardan las credenciales propias del dueño del gym obtenidas vía OAuth
        public string? MercadoPagoPublicKey { get; set; }
        public string? MercadoPagoUserId { get; set; }
    }
}