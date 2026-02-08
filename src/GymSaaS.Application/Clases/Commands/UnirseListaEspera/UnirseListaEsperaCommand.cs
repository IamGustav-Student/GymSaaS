using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Commands.UnirseListaEspera
{
    public record UnirseListaEsperaCommand : IRequest<bool>
    {
        public int ClaseId { get; init; }
        public int SocioId { get; init; }
    }

    public class UnirseListaEsperaCommandHandler : IRequestHandler<UnirseListaEsperaCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UnirseListaEsperaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UnirseListaEsperaCommand request, CancellationToken cancellationToken)
        {
            // 1. Validar que la clase existe
            var clase = await _context.Clases.FindAsync(new object[] { request.ClaseId }, cancellationToken);
            if (clase == null) return false;

            // 2. Validar que no esté ya inscrito o en lista de espera
            bool yaEnLista = await _context.ListasEspera
                .AnyAsync(l => l.ClaseId == request.ClaseId && l.SocioId == request.SocioId, cancellationToken);

            bool yaReservado = await _context.Reservas
                .AnyAsync(r => r.ClaseId == request.ClaseId && r.SocioId == request.SocioId, cancellationToken);

            if (yaEnLista || yaReservado)
            {
                // Ya está procesado, retornamos true para no dar error, pero no hacemos nada
                return true;
            }

            // 3. Agregar a la lista
            var entrada = new ListaEspera
            {
                ClaseId = request.ClaseId,
                SocioId = request.SocioId,
                FechaRegistro = DateTime.UtcNow
            };

            _context.ListasEspera.Add(entrada);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}