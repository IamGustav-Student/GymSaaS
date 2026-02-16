using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class TipoMembresia : BaseEntity, IMustHaveTenant, ISoftDelete
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public int? CantidadClases { get; set; }

        // --- NUEVO: Control de Días de Acceso ---
        public bool AccesoLunes { get; set; } = true;
        public bool AccesoMartes { get; set; } = true;
        public bool AccesoMiercoles { get; set; } = true;
        public bool AccesoJueves { get; set; } = true;
        public bool AccesoViernes { get; set; } = true;
        public bool AccesoSabado { get; set; } = true;
        public bool AccesoDomingo { get; set; } = true;
        // ----------------------------------------

        public string TenantId { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}