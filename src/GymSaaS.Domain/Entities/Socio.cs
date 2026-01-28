using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Socio : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public bool Activo { get; set; } = true;
        

        // Foto de perfil para seguridad visual en acceso
        public string? FotoUrl { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string TenantId { get; set; } = string.Empty;

        // Relación: Un socio puede tener muchas membresías (historial)
        public ICollection<MembresiaSocio> Membresias { get; set; } = new List<MembresiaSocio>();
        public IList<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
        public IList<Pago> Pagos { get; set; } = new List<Pago>();
    }
}