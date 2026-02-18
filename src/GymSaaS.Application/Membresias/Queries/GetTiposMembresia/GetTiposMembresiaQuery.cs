using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Queries.GetTiposMembresia
{
    public record GetTiposMembresiaQuery : IRequest<List<TipoMembresiaDto>>;

    public class GetTiposMembresiaQueryHandler : IRequestHandler<GetTiposMembresiaQuery, List<TipoMembresiaDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetTiposMembresiaQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TipoMembresiaDto>> Handle(GetTiposMembresiaQuery request, CancellationToken cancellationToken)
        {
            return await _context.TiposMembresia
                .AsNoTracking()
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Precio)
                .Select(t => new TipoMembresiaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Precio = t.Precio,
                    DuracionDias = t.DuracionDias,
                    CantidadClases = t.CantidadClases,
                    IsDeleted = t.IsDeleted,

                    // Mapeo de Días Permitidos (Nuevo)
                    AccesoLunes = t.AccesoLunes,
                    AccesoMartes = t.AccesoMartes,
                    AccesoMiercoles = t.AccesoMiercoles,
                    AccesoJueves = t.AccesoJueves,
                    AccesoViernes = t.AccesoViernes,
                    AccesoSabado = t.AccesoSabado,
                    AccesoDomingo = t.AccesoDomingo
                })
                .ToListAsync(cancellationToken);
        }
    }
}