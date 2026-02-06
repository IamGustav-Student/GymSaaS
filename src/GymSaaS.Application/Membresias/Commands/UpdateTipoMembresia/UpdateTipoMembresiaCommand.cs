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

        public bool AccesoLunes { get; init; }
        public bool AccesoMartes { get; init; }
        public bool AccesoMiercoles { get; init; }
        public bool AccesoJueves { get; init; }
        public bool AccesoViernes { get; init; }
        public bool AccesoSabado { get; init; }
        public bool AccesoDomingo { get; init; }
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

            // --- REGLA DE SEGURIDAD (FAIL-SAFE) ---
            bool ningunDiaSeleccionado = !request.AccesoLunes && !request.AccesoMartes &&
                                         !request.AccesoMiercoles && !request.AccesoJueves &&
                                         !request.AccesoViernes && !request.AccesoSabado &&
                                         !request.AccesoDomingo;

            entity.Nombre = request.Nombre;
            entity.Precio = request.Precio;
            entity.DuracionDias = request.DuracionDias;
            entity.CantidadClases = request.CantidadClases;

            // Actualización de Días con lógica de seguridad
            entity.AccesoLunes = ningunDiaSeleccionado ? true : request.AccesoLunes;
            entity.AccesoMartes = ningunDiaSeleccionado ? true : request.AccesoMartes;
            entity.AccesoMiercoles = ningunDiaSeleccionado ? true : request.AccesoMiercoles;
            entity.AccesoJueves = ningunDiaSeleccionado ? true : request.AccesoJueves;
            entity.AccesoViernes = ningunDiaSeleccionado ? true : request.AccesoViernes;
            entity.AccesoSabado = ningunDiaSeleccionado ? true : request.AccesoSabado;
            entity.AccesoDomingo = ningunDiaSeleccionado ? true : request.AccesoDomingo;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}