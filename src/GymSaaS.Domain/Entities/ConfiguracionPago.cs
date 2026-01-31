using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class ConfiguracionPago : BaseEntity, IMustHaveTenant
    {
        // El TenantId viene heredado o implícito por IMustHaveTenant según tu arquitectura
        public string TenantId { get; set; } = string.Empty;

        // Credenciales de MercadoPago del DUEÑO DEL GIMNASIO
        public string AccessToken { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty; // Opcional, para el frontend

        public bool Activo { get; set; } = true;
    }
}