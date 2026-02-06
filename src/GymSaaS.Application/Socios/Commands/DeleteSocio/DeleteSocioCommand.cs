using GymSaaS.Application.Common.Interfaces;
using MediatR;
using System.Collections.Generic; // Aseguramos referencias básicas
using System.Threading;
using System.Threading.Tasks;

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
            // Buscamos el socio por ID
            var entity = await _context.Socios.FindAsync(new object[] { request.Id }, cancellationToken);

            // Si no existe, lanzamos excepción (o podrías retornar y no hacer nada)
            if (entity == null) throw new KeyNotFoundException("Socio no encontrado");

            // --- CORRECCIÓN SOFT DELETE ---
            // En lugar de borrarlo físicamente (lo que causaba el error de SQL),
            // simplemente lo marcamos como eliminado.
            entity.IsDeleted = true;

            // Opcional: Desactivar acceso inmediatamente
            entity.Activo = false;

            // Guardamos los cambios. EF Core detectará la modificación del campo IsDeleted.
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}