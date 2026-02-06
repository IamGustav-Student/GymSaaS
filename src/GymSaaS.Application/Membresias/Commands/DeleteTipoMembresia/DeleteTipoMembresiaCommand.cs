using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.DeleteTipoMembresia
{
    public record DeleteTipoMembresiaCommand(int Id) : IRequest;

    public class DeleteTipoMembresiaCommandHandler : IRequestHandler<DeleteTipoMembresiaCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteTipoMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteTipoMembresiaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.TiposMembresia
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (entity != null)
            {
                // Soft Delete (Borrado Lógico)
                entity.IsDeleted = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}