using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Ejercicio : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty;
        public string? GrupoMuscular { get; set; } // Pecho, Espalda, Piernas...
        public string? VideoUrl { get; set; }      // Link a video explicativo
        public string? Descripcion { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}