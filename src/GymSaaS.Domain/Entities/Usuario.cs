using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Usuario : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Se guardará Hasheada
        public bool Activo { get; set; } = true;

        // Multi-Tenancy
        public string TenantId { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }
}