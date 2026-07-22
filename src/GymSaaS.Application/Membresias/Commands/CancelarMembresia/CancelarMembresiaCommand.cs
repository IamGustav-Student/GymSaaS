using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.CancelarMembresia
{
    // Cancela la membresía ACTIVA de un socio antes de su vencimiento natural
    // (a diferencia de DeleteTipoMembresiaCommand, que da de baja un PLAN, no
    // una membresía puntual ya asignada a un socio).
    public record CancelarMembresiaCommand(int MembresiaSocioId) : IRequest;

    public class CancelarMembresiaCommandHandler : IRequestHandler<CancelarMembresiaCommand>
    {
        private readonly IApplicationDbContext _context;

        public CancelarMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(CancelarMembresiaCommand request, CancellationToken cancellationToken)
        {
            var membresia = await _context.MembresiasSocios
                .FirstOrDefaultAsync(m => m.Id == request.MembresiaSocioId, cancellationToken);

            if (membresia == null) throw new KeyNotFoundException("Membresía no encontrada.");

            if (!membresia.Activa)
            {
                // Ya estaba vencida o cancelada: no hay nada que hacer.
                return;
            }

            membresia.Activa = false;
            membresia.Estado = "Cancelada";

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
