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
                .Select(RutinaDto.Projection)
                .ToListAsync(cancellationToken);
        }
    }
}