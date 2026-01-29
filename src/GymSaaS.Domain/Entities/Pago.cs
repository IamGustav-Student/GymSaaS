using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Pago : BaseEntity, IMustHaveTenant
    {
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; } // <--- AGREGADA (Soluciona error 'Fecha')
        public string MetodoPago { get; set; } = string.Empty; // Ej: "MercadoPago", "Efectivo"
        
        // Relaciones
        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public int? MembresiaSocioId { get; set; } // <--- AGREGADA (Soluciona error 'MembresiaSocioId')
        public MembresiaSocio? MembresiaSocio { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}