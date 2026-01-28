using GymSaaS.Application.Common.Interfaces;
using MediatR;

namespace GymSaaS.Application.Socios.Commands.DeleteSocio
{
    public record DeleteSocioCommand(int Id) : IRequest;

    public class DeleteSocioCommandHandler : IRequestHandler<DeleteSocioCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteSocioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteSocioCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Socios.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null) throw new KeyNotFoundException("Socio no encontrado");

            // Aquí ejecutamos Remove, pero nuestro DbContext lo convertirá en Soft Delete mágicamente
            _context.Socios.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}