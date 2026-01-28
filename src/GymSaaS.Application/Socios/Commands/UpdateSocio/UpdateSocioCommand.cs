using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using MediatR;

namespace GymSaaS.Application.Socios.Commands.UpdateSocio
{
    public record UpdateSocioCommand : IRequest
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Telefono { get; init; }
    }

    public class UpdateSocioCommandHandler : IRequestHandler<UpdateSocioCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateSocioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateSocioCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Socios.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null) throw new KeyNotFoundException("Socio no encontrado");

            entity.Nombre = request.Nombre;
            entity.Apellido = request.Apellido;
            entity.Email = request.Email;
            entity.Telefono = request.Telefono;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}