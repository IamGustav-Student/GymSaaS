using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Rutinas.Queries.GetRutinas
{
    // Query para listar todas las rutinas (Dashboard)
    public record GetRutinasQuery : IRequest<List<RutinaDto>>;

    public class GetRutinasQueryHandler : IRequestHandler<GetRutinasQuery, List<RutinaDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetRutinasQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RutinaDto>> Handle(GetRutinasQuery request, CancellationToken cancellationToken)
        {
            return await _context.Rutinas
                .AsNoTracking()
                .Include(r => r.Socio)
                .Include(r => r.RutinaEjercicios)
                    .ThenInclude(re => re.Ejercicio)
                .OrderByDescending(r => r.FechaAsignacion)
                .Select(r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    SocioId = r.SocioId,
                    SocioNombre = r.Socio.Nombre + " " + r.Socio.Apellido,

                    // CORRECCIÓN: Usamos solo las fechas que existen en tu DTO y Entidad
                    FechaAsignacion = r.FechaAsignacion,
                    FechaFin = r.FechaFin,

                    // Mapeo manual de ejercicios para incluir VideoUrl
                    Ejercicios = r.RutinaEjercicios.Select(re => new RutinaEjercicioDto
                    {
                        EjercicioId = re.EjercicioId,
                        EjercicioNombre = re.Ejercicio.Nombre,
                        GrupoMuscular = re.Ejercicio.GrupoMuscular,

                        // Propiedad para el modal de video
                        VideoUrl = re.Ejercicio.VideoUrl,

                        Series = re.Series,
                        Repeticiones = re.Repeticiones,
                        PesoSugerido = re.PesoSugerido,
                        Notas = re.Notas
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
        }
    }
}