using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Sucursales.Queries.PrepararCambioDeSucursal
{
    public class DatosSesionSucursalDto
    {
        public int UsuarioId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    // Verifica que el admin logueado en TenantIdOrigen tenga permiso para
    // cambiarse a TenantIdDestino: ambas sucursales deben pertenecer a la misma
    // Empresa, y debe existir un Usuario Admin con el mismo email en el destino.
    public record PrepararCambioDeSucursalQuery(int TenantIdOrigen, int TenantIdDestino, string EmailActual)
        : IRequest<DatosSesionSucursalDto?>;

    public class PrepararCambioDeSucursalQueryHandler
        : IRequestHandler<PrepararCambioDeSucursalQuery, DatosSesionSucursalDto?>
    {
        private readonly IApplicationDbContext _context;

        public PrepararCambioDeSucursalQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DatosSesionSucursalDto?> Handle(PrepararCambioDeSucursalQuery request, CancellationToken cancellationToken)
        {
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.Id == request.TenantIdOrigen || t.Id == request.TenantIdDestino)
                .ToListAsync(cancellationToken);

            var origen = tenants.FirstOrDefault(t => t.Id == request.TenantIdOrigen);
            var destino = tenants.FirstOrDefault(t => t.Id == request.TenantIdDestino);

            if (origen == null || destino == null) return null;

            var mismaEmpresa = origen.EmpresaId.HasValue && origen.EmpresaId == destino.EmpresaId;
            if (!mismaEmpresa) return null;

            var usuarioDestino = await _context.Usuarios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.TenantId == destino.Id.ToString() &&
                    u.Email == request.EmailActual &&
                    u.Role == "Admin" &&
                    u.Activo,
                    cancellationToken);

            if (usuarioDestino == null) return null;

            return new DatosSesionSucursalDto
            {
                UsuarioId = usuarioDestino.Id,
                Email = usuarioDestino.Email,
                Nombre = usuarioDestino.Nombre,
                TenantId = usuarioDestino.TenantId
            };
        }
    }
}
