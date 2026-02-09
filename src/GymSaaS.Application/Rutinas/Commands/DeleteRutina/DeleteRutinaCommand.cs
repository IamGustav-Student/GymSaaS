using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Rutinas.Commands.DeleteRutina
{
    public record DeleteRutinaCommand(int Id) : IRequest;

    public class DeleteRutinaCommandHandler : IRequestHandler<DeleteRutinaCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public DeleteRutinaCommandHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task Handle(DeleteRutinaCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentTenantService.TenantId;

            var entity = await _context.Rutinas
                .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken);

            if (entity == null) throw new KeyNotFoundException($"Rutina {request.Id} no encontrada.");

            // EF Core maneja el borrado en cascada de los ejercicios hijos automáticamente si está configurado,
            // pero es buena práctica asegurarse.
            _context.Rutinas.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}