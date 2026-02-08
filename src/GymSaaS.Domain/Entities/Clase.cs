using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    public class Clase : BaseEntity, IMustHaveTenant
    {
        public string Nombre { get; set; } = string.Empty; // Ej: "Yoga Matutino"
        public string? Instructor { get; set; }

        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }

        public int CupoMaximo { get; set; }
        public int CupoReservado { get; set; } // Contador desnormalizado para consultas rápidas

        public bool Activa { get; set; } = true;

        // --- NUEVA PROPIEDAD ---
        public decimal Precio { get; set; }
        // -----------------------

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

        // --- GESTIÓN DE DEMANDA (NUEVO) ---
        public ICollection<ListaEspera> ListaEspera { get; set; } = new List<ListaEspera>();

        public string TenantId { get; set; } = string.Empty;
    }
}