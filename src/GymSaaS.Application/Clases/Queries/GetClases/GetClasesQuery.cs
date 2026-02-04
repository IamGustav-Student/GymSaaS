using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClases
{
    public record GetClasesQuery : IRequest<List<ClaseDto>>;

    public class GetClasesQueryHandler : IRequestHandler<GetClasesQuery, List<ClaseDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClasesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClaseDto>> Handle(GetClasesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Clases
                .AsNoTracking()
                .OrderByDescending(c => c.FechaHoraInicio)
                .Select(c => new ClaseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Instructor = c.Instructor,
                    FechaHoraInicio = c.FechaHoraInicio,
                    DuracionMinutos = c.DuracionMinutos,
                    CupoMaximo = c.CupoMaximo,
                    CupoReservado = c.CupoReservado,
                    Activa = c.Activa,

                    // Mapeamos el precio
                    Precio = c.Precio
                })
                .ToListAsync(cancellationToken);
        }
    }
}