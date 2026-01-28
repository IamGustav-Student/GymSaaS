using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    public class GetSociosQueryHandler : IRequestHandler<GetSociosQuery, List<SocioDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetSociosQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SocioDto>> Handle(GetSociosQuery request, CancellationToken cancellationToken)
        {
            // Entity Framework aplica el filtro de Tenant automáticamente aquí
            return await _context.Socios
                .OrderBy(s => s.Apellido)
                .Select(s => new SocioDto
                {
                    Id = s.Id,
                    NombreCompleto = $"{s.Nombre} {s.Apellido}",
                    Dni = s.Dni,
                    Email = s.Email,
                    Estado = s.Activo ? "Activo" : "Inactivo",
                    UltimoAcceso = "-" // Lo implementaremos en la fase de Accesos
                })
                .ToListAsync(cancellationToken);
        }
    }
}