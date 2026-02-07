using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Pago : BaseEntity, IMustHaveTenant
    {
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; } = string.Empty; // Ej: "MercadoPago", "Efectivo"

        // Estado del Pago
        public bool Pagado { get; set; } = false;
        public string? EstadoTransaccion { get; set; } // "approved", "rejected", etc.
        public string? IdTransaccionExterna { get; set; } // ID de MercadoPago

        // --- DUNNING MANAGEMENT (RECUPERACIÓN) ---
        public bool EsReintento { get; set; } = false;
        public int IntentosFallidos { get; set; } = 0;
        public DateTime? ProximoReintento { get; set; }
        public string? TokenTarjeta { get; set; } // Token seguro para reintentar sin pedir CVV

        // Relaciones
        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public int? MembresiaSocioId { get; set; }
        public MembresiaSocio? MembresiaSocio { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}