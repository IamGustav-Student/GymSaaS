using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class ListaEspera : BaseEntity, IMustHaveTenant
    {
        public int ClaseId { get; set; }
        public Clase? Clase { get; set; }

        public int SocioId { get; set; }
        public Socio? Socio { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public string TenantId { get; set; } = string.Empty;
    }
}