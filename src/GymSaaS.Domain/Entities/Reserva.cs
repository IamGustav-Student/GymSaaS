using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Reserva : BaseEntity, IMustHaveTenant
    {
        // --- Propiedades Existentes ---
        public int ClaseId { get; set; }
        public Clase Clase { get; set; } = null!;

        public int SocioId { get; set; }
        public Socio Socio { get; set; } = null!;

        public DateTime FechaReserva { get; set; } = DateTime.UtcNow;
        public bool Asistio { get; set; } = false;

        public string TenantId { get; set; } = string.Empty;

        // --- NUEVAS PROPIEDADES (Para Pagos) ---
        // Estados: "PendientePago", "Confirmada", "Cancelada"
        public string Estado { get; set; } = "Confirmada";

        // Guardamos el precio histórico (por si la clase cambia de precio mañana)
        public decimal Monto { get; set; }

        // ID de referencia de MercadoPago
        public string? PaymentId { get; set; }
    }
}