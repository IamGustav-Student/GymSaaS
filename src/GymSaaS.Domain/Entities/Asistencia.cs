using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Asistencia : BaseEntity, IMustHaveTenant
    {
        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public DateTime FechaHora { get; set; }
        public bool Permitido { get; set; } // True: Pasó, False: Rebotado
        public string Detalle { get; set; } = string.Empty; // "Membresía Vencida", "OK", etc.

        public string TenantId { get; set; } = string.Empty;
        public string Tipo { get; set; } = "QR"; // "QR", "Manual", "API", etc.
      
    }
}