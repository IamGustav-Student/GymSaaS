using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Ejercicios.Queries.GetEjercicios;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Ejercicios.Queries.GetEjercicioById
{
    public record GetEjercicioByIdQuery(int Id) : IRequest<EjercicioDto?>;

    public class GetEjercicioByIdQueryHandler : IRequestHandler<GetEjercicioByIdQuery, EjercicioDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetEjercicioByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EjercicioDto?> Handle(GetEjercicioByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Ejercicios
                .Where(e => e.Id == request.Id)
                .Select(EjercicioDto.Projection)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
