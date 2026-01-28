using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class TipoMembresia : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty; // Ej: Pase Libre Mes
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; } // 30 dias
        public int? CantidadClases { get; set; } // Null = Ilimitado, numero = cupo limitado

        public string TenantId { get; set; } = string.Empty;
    }
}