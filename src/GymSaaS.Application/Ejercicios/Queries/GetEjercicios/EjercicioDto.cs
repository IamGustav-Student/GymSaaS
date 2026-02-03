using GymSaaS.Domain.Entities;
using System.Linq.Expressions;

namespace GymSaaS.Application.Ejercicios.Queries.GetEjercicios
{
    public class EjercicioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? GrupoMuscular { get; set; }
        public string? VideoUrl { get; set; }
        public string? Descripcion { get; set; }

        public static Expression<Func<Ejercicio, EjercicioDto>> Projection
        {
            get
            {
                return e => new EjercicioDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular,
                    VideoUrl = e.VideoUrl,
                    Descripcion = e.Descripcion
                };
            }
        }
    }
}