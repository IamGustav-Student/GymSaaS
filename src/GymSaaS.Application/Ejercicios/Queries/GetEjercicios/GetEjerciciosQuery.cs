using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Ejercicios.Queries.GetEjercicios
{
    public record GetEjerciciosQuery : IRequest<List<EjercicioDto>>;

    public class GetEjerciciosQueryHandler : IRequestHandler<GetEjerciciosQuery, List<EjercicioDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetEjerciciosQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EjercicioDto>> Handle(GetEjerciciosQuery request, CancellationToken cancellationToken)
        {
            return await _context.Ejercicios
                .AsNoTracking()
                .OrderBy(e => e.GrupoMuscular)
                .ThenBy(e => e.Nombre)
                .Select(EjercicioDto.Projection)
                .ToListAsync(cancellationToken);
        }
    }
}