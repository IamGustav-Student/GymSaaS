using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        // Identidad
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Slug/Subdominio

        // Estado y Configuración Base
        public string SubscriptionPlan { get; set; } = "Free";
        public bool IsActive { get; set; } = true;

        // NUEVA IMPLEMENTACIÓN: Control de Abuso de Trial
        public bool HasUsedTrial { get; set; } = false;

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
        public string TimeZoneId { get; set; } = "Argentina Standard Time";

        // ==========================================
        // PROPIEDADES SAAS (MODELO DE SUSCRIPCIÓN)
        // ==========================================

        // NUEVA IMPLEMENTACIÓN: Tipificación fuerte del plan
        public PlanType Plan { get; set; } = PlanType.PruebaGratuita;

        // NUEVA IMPLEMENTACIÓN: Estado de la suscripción en el ciclo de vida
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

        /// <summary>
        /// NUEVA IMPLEMENTACIÓN: Límite de socios activos permitidos. Null indica ilimitado.
        /// </summary>
        public int? MaxSocios { get; set; }

        public DateTime? TrialEndsAt { get; set; }

        // NUEVA IMPLEMENTACIÓN: Fecha crítica para el Middleware de acceso
        public DateTime? SubscriptionEndsAt { get; set; }

        /// <summary>
        /// ID de referencia para la suscripción recurrente (Preapproval ID de MercadoPago)
        /// </summary>
        public string? MercadoPagoSubscriptionId { get; set; }

        // --- MERCADOPAGO CONNECT (Cobro del Gimnasio) ---
        public string? MercadoPagoPublicKey { get; set; }
        public string? MercadoPagoUserId { get; set; }
    }
}