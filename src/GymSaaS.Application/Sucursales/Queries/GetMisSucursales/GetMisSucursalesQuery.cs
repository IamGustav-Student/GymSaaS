using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Sucursales.Queries.GetMisSucursales
{
    public class SucursalDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsLaActual { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public record GetMisSucursalesQuery : IRequest<List<SucursalDto>>;

    public class GetMisSucursalesQueryHandler : IRequestHandler<GetMisSucursalesQuery, List<SucursalDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public GetMisSucursalesQueryHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<List<SucursalDto>> Handle(GetMisSucursalesQuery request, CancellationToken cancellationToken)
        {
            var tenantIdStr = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out var tenantId))
                return new List<SucursalDto>();

            var tenantActual = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenantActual == null) return new List<SucursalDto>();

            var query = _context.Tenants.IgnoreQueryFilters().AsNoTracking().AsQueryable();

            query = tenantActual.EmpresaId.HasValue
                ? query.Where(t => t.EmpresaId == tenantActual.EmpresaId.Value)
                : query.Where(t => t.Id == tenantActual.Id);

            var sucursales = await query
                .OrderBy(t => t.Name)
                .Select(t => new SucursalDto
                {
                    Id = t.Id,
                    Nombre = t.Name,
                    EsLaActual = t.Id == tenantActual.Id,
                    Estado = t.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            return sucursales;
        }
    }
}
