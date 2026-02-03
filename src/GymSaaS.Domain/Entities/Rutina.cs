using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Rutina : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty; // Ej: "Hipertrofia Fase 1"

        public int SocioId { get; set; }
        public Socio Socio { get; set; } = null!;

        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaFin { get; set; }

        // Relación con los ejercicios (detalles)
        public ICollection<RutinaEjercicio> RutinaEjercicios { get; set; } = new List<RutinaEjercicio>();

        public string TenantId { get; set; } = string.Empty;
    }
}