using GymSaaS.Domain.Entities;
using System.Linq.Expressions;

namespace GymSaaS.Application.Rutinas.Queries.GetRutinas
{
    public class RutinaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int SocioId { get; set; }
        public string SocioNombre { get; set; } = string.Empty; // Nombre completo del socio
        public DateTime FechaAsignacion { get; set; }
        public DateTime? FechaFin { get; set; }

        // Detalle de ejercicios
        public List<RutinaEjercicioDto> Ejercicios { get; set; } = new();

        // Proyección EF Core
        public static Expression<Func<Rutina, RutinaDto>> Projection
        {
            get
            {
                return r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    SocioId = r.SocioId,
                    SocioNombre = r.Socio.Nombre + " " + r.Socio.Apellido,
                    FechaAsignacion = r.FechaAsignacion,
                    FechaFin = r.FechaFin,
                    Ejercicios = r.RutinaEjercicios.Select(re => new RutinaEjercicioDto
                    {
                        EjercicioId = re.EjercicioId,
                        EjercicioNombre = re.Ejercicio.Nombre,
                        GrupoMuscular = re.Ejercicio.GrupoMuscular,
                        Series = re.Series,
                        Repeticiones = re.Repeticiones,
                        PesoSugerido = re.PesoSugerido,
                        Notas = re.Notas
                    }).ToList()
                };
            }
        }
    }

    public class RutinaEjercicioDto
    {
        public int EjercicioId { get; set; }
        public string EjercicioNombre { get; set; } = string.Empty;
        public string? GrupoMuscular { get; set; }
        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public string? PesoSugerido { get; set; }
        public string? Notas { get; set; }
    }
}