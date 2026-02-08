using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Commands.UpdateClase
{
    public record UpdateClaseCommand : IRequest
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Instructor { get; init; }
        public DateTime FechaHoraInicio { get; init; }
        public int DuracionMinutos { get; init; }
        public int CupoMaximo { get; init; }
        public bool Activa { get; init; }
        public decimal Precio { get; set; }
    }

    public class UpdateClaseCommandValidator : AbstractValidator<UpdateClaseCommand>
    {
        public UpdateClaseCommandValidator()
        {
            RuleFor(v => v.Id).GreaterThan(0);
            RuleFor(v => v.Nombre).NotEmpty();
            RuleFor(v => v.DuracionMinutos).GreaterThan(15);
            RuleFor(v => v.CupoMaximo).GreaterThan(0);
        }
    }

    public class UpdateClaseCommandHandler : IRequestHandler<UpdateClaseCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateClaseCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateClaseCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (entity == null) throw new KeyNotFoundException($"Clase {request.Id} no encontrada.");

            entity.Nombre = request.Nombre;
            entity.Instructor = request.Instructor;
            entity.FechaHoraInicio = request.FechaHoraInicio;
            entity.DuracionMinutos = request.DuracionMinutos;
            entity.CupoMaximo = request.CupoMaximo;
            entity.Activa = request.Activa;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}