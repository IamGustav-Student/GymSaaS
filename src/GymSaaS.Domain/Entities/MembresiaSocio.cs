using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class MembresiaSocio : BaseEntity, IMustHaveTenant
    {
        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public int TipoMembresiaId { get; set; }
        public TipoMembresia? TipoMembresia { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        // Control de consumo
        public int? ClasesRestantes { get; set; }
        public string Estado { get; set; } = "Pendiente";

        public decimal PrecioPagado { get; set; } // Guardamos el precio histórico
        public bool Activa { get; set; } = true;

        public string TenantId { get; set; } = string.Empty;
    }
}