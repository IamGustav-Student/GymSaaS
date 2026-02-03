using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class RutinaEjercicio : BaseEntity, IMustHaveTenant
    {
        public int RutinaId { get; set; }
        public Rutina Rutina { get; set; } = null!;

        public int EjercicioId { get; set; }
        public Ejercicio Ejercicio { get; set; } = null!;

        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public string? PesoSugerido { get; set; } // Ej: "20kg", "Fallo"
        public string? Notas { get; set; }       // Ej: "Descanso 1 min"

        public string TenantId { get; set; } = string.Empty;
    }
}