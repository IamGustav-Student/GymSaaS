using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    // Permite renovar el access token JWT (de vida corta) sin pedir contraseña
    // de nuevo. Solo se persiste el hash del token, nunca el valor en texto plano.
    public class RefreshToken : BaseEntity
    {
        public string TokenHash { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public bool EstaActivo => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    }
}
