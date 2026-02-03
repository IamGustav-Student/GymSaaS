using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Rutinas.Queries.GetRutinas;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Rutinas.Queries.GetRutinaById
{
    // Query para ver el detalle de UNA rutina específica
    public record GetRutinaByIdQuery(int Id) : IRequest<RutinaDto?>;

    public class GetRutinaByIdQueryHandler : IRequestHandler<GetRutinaByIdQuery, RutinaDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetRutinaByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RutinaDto?> Handle(GetRutinaByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Rutinas
                .AsNoTracking()
                .Where(r => r.Id == request.Id)
                .Select(RutinaDto.Projection)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}