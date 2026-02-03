using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Reserva : BaseEntity, IMustHaveTenant
    {
        public int ClaseId { get; set; }
        public Clase Clase { get; set; } = null!;

        public int SocioId { get; set; }
        public Socio Socio { get; set; } = null!;

        public DateTime FechaReserva { get; set; } = DateTime.UtcNow;
        public bool Asistio { get; set; } = false;

        public string TenantId { get; set; } = string.Empty;
    }
}