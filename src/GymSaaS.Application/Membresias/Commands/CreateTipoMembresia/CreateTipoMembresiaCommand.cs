using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;

namespace GymSaaS.Application.Membresias.Commands.CreateTipoMembresia
{
    public record CreateTipoMembresiaCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public decimal Precio { get; init; }
        public int DuracionDias { get; init; }
        public int? CantidadClases { get; init; }

        public bool AccesoLunes { get; init; } = true;
        public bool AccesoMartes { get; init; } = true;
        public bool AccesoMiercoles { get; init; } = true;
        public bool AccesoJueves { get; init; } = true;
        public bool AccesoViernes { get; init; } = true;
        public bool AccesoSabado { get; init; } = true;
        public bool AccesoDomingo { get; init; } = true;
    }

    public class CreateTipoMembresiaCommandValidator : AbstractValidator<CreateTipoMembresiaCommand>
    {
        public CreateTipoMembresiaCommandValidator()
        {
            RuleFor(v => v.Nombre).NotEmpty();
            RuleFor(v => v.Precio).GreaterThan(0);
            RuleFor(v => v.DuracionDias).GreaterThan(0);
        }
    }

    public class CreateTipoMembresiaCommandHandler : IRequestHandler<CreateTipoMembresiaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateTipoMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateTipoMembresiaCommand request, CancellationToken cancellationToken)
        {
            // --- REGLA DE SEGURIDAD (FAIL-SAFE) ---
            // Si el dueño desmarcó TODOS los días, asumimos que quiso decir "Todos los días"
            // en lugar de "Ningún día".
            bool ningunDiaSeleccionado = !request.AccesoLunes && !request.AccesoMartes &&
                                         !request.AccesoMiercoles && !request.AccesoJueves &&
                                         !request.AccesoViernes && !request.AccesoSabado &&
                                         !request.AccesoDomingo;

            var entity = new TipoMembresia
            {
                Nombre = request.Nombre,
                Precio = request.Precio,
                DuracionDias = request.DuracionDias,
                CantidadClases = request.CantidadClases,

                // Si ningunDiaSeleccionado es true, forzamos todo a true. Si no, usamos lo que vino.
                AccesoLunes = ningunDiaSeleccionado ? true : request.AccesoLunes,
                AccesoMartes = ningunDiaSeleccionado ? true : request.AccesoMartes,
                AccesoMiercoles = ningunDiaSeleccionado ? true : request.AccesoMiercoles,
                AccesoJueves = ningunDiaSeleccionado ? true : request.AccesoJueves,
                AccesoViernes = ningunDiaSeleccionado ? true : request.AccesoViernes,
                AccesoSabado = ningunDiaSeleccionado ? true : request.AccesoSabado,
                AccesoDomingo = ningunDiaSeleccionado ? true : request.AccesoDomingo
            };

            _context.TiposMembresia.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}