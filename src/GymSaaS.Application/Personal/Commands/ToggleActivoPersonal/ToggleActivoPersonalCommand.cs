using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Personal.Commands.ToggleActivoPersonal
{
    public record ToggleActivoPersonalCommand(int UsuarioId, int UsuarioActualId) : IRequest;

    public class ToggleActivoPersonalCommandHandler : IRequestHandler<ToggleActivoPersonalCommand>
    {
        private readonly IApplicationDbContext _context;

        public ToggleActivoPersonalCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(ToggleActivoPersonalCommand request, CancellationToken cancellationToken)
        {
            if (request.UsuarioId == request.UsuarioActualId)
                throw new InvalidOperationException("No podés desactivar tu propia cuenta.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == request.UsuarioId, cancellationToken);

            if (usuario == null) throw new KeyNotFoundException("Usuario no encontrado.");

            if (usuario.Activo && usuario.Role == Roles.Admin)
            {
                var otrosAdminsActivos = await _context.Usuarios
                    .CountAsync(u => u.Id != usuario.Id && u.Role == Roles.Admin && u.Activo, cancellationToken);

                if (otrosAdminsActivos == 0)
                    throw new InvalidOperationException("No podés desactivar al único administrador del gimnasio.");
            }

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
