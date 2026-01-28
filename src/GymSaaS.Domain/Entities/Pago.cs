using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Pago : BaseEntity, IMustHaveTenant
    {
        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, MercadoPago, Transferencia
        public string? ComprobanteExterno { get; set; } // ID de pago de MercadoPago

        public string TenantId { get; set; } = string.Empty;
    }
}