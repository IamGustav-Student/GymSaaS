using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.UpdateTipoMembresia
{
    public record UpdateTipoMembresiaCommand : IRequest
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public decimal Precio { get; init; }
        public int DuracionDias { get; init; }
        public int? CantidadClases { get; init; }
    }

    public class UpdateTipoMembresiaCommandHandler : IRequestHandler<UpdateTipoMembresiaCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateTipoMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateTipoMembresiaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.TiposMembresia
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (entity == null) throw new KeyNotFoundException($"Membresia {request.Id} no encontrada");

            entity.Nombre = request.Nombre;
            entity.Precio = request.Precio;
            entity.DuracionDias = request.DuracionDias;
            entity.CantidadClases = request.CantidadClases;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}