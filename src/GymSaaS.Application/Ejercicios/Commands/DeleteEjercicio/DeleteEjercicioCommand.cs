using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Ejercicios.Commands.DeleteEjercicio
{
    public record DeleteEjercicioCommand(int Id) : IRequest;

    public class DeleteEjercicioCommandHandler : IRequestHandler<DeleteEjercicioCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteEjercicioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteEjercicioCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (entity != null)
            {
                // Soft Delete: preserva el historial de rutinas que ya lo usaban
                entity.IsDeleted = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
