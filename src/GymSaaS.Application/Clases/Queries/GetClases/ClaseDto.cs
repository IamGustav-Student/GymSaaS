using GymSaaS.Domain.Entities;
using System.Linq.Expressions;

namespace GymSaaS.Application.Clases.Queries.GetClases
{
    public class ClaseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public int CupoMaximo { get; set; }
        public int CupoReservado { get; set; }
        public bool Activa { get; set; }
        public string Estado => Activa ? "Activa" : "Cancelada";

        // NUEVO: Lista de asistentes para la vista "Ver Listado"
        public List<AsistenteDto> Asistentes { get; set; } = new();

        public static Expression<Func<Clase, ClaseDto>> Projection
        {
            get
            {
                return c => new ClaseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Instructor = c.Instructor,
                    FechaHoraInicio = c.FechaHoraInicio,
                    DuracionMinutos = c.DuracionMinutos,
                    CupoMaximo = c.CupoMaximo,
                    CupoReservado = c.CupoReservado,
                    Activa = c.Activa,
                    // Nota: La proyección de asistentes se hace mejor en memoria o con un Select anidado en el Handler
                    // Para evitar complejidad aquí, lo dejamos vacío y lo llenamos en el Handler si es necesario.
                };
            }
        }
    }

    public class AsistenteDto
    {
        public int ReservaId { get; set; }
        public string SocioNombre { get; set; } = string.Empty;
        public DateTime FechaReserva { get; set; }
    }
}