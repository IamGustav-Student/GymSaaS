using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        // El ID del Tenant (GUID) se manejará externamente o como string en la implementación
        public string Name { get; set; } = string.Empty;

        // NUEVO FASE 0: Identificador único lógico para relacionar usuarios y datos (UUID/GUID)
        public string Code { get; set; } = string.Empty;

        public string SubscriptionPlan { get; set; } = "Basic"; // Basic, Pro, Enterprise
        public bool IsActive { get; set; } = true;

        // Datos de Configuración
        public string? LogoUrl { get; set; }
        public string? WebSiteUrl { get; set; }

        // Integraciones (Encriptadas en BD)
        public string? MercadoPagoAccessToken { get; set; }
    }
}