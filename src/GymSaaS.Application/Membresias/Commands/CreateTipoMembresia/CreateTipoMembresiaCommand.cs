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
            var entity = new TipoMembresia
            {
                Nombre = request.Nombre,
                Precio = request.Precio,
                DuracionDias = request.DuracionDias,
                CantidadClases = request.CantidadClases
                // TenantId automático
            };

            _context.TiposMembresia.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}