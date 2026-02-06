using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Queries.GetTipoMembresiaById
{
    public record GetTipoMembresiaByIdQuery(int Id) : IRequest<TipoMembresia?>;

    public class GetTipoMembresiaByIdQueryHandler : IRequestHandler<GetTipoMembresiaByIdQuery, TipoMembresia?>
    {
        private readonly IApplicationDbContext _context;

        public GetTipoMembresiaByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TipoMembresia?> Handle(GetTipoMembresiaByIdQuery request, CancellationToken cancellationToken)
        {
            // Buscamos incluso si está borrado lógicamente por seguridad, 
            // pero para edición normal usaremos el Id directo.
            return await _context.TiposMembresia
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        }
    }
}