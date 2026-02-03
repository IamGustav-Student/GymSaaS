using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GymSaaS.Application.Clases.Queries.GetClases
{
    public record GetClasesQuery : IRequest<List<ClaseDto>>;

    public class GetClasesQueryHandler : IRequestHandler<GetClasesQuery, List<ClaseDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClasesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClaseDto>> Handle(GetClasesQuery request, CancellationToken cancellationToken)
        {
            // Traemos las clases futuras y recientes (ej: últimos 7 días en adelante)
            // Ordenadas por fecha
            return await _context.Clases
                .AsNoTracking()
                .OrderByDescending(c => c.FechaHoraInicio)
                .Select(ClaseDto.Projection)
                .ToListAsync(cancellationToken);
        }
    }
}