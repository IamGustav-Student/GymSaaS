using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    // 1. El DTO Específico para Edición (Separamos Nombre y Apellido)
    public class SocioDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Dni { get; set; } = string.Empty; // Útil para mostrarlo (aunque no se edite)
    }

    // 2. La Petición
    public record GetSocioByIdQuery(int Id) : IRequest<SocioDetailDto>;

    // 3. El Manejador (Handler)
    public class GetSocioByIdQueryHandler : IRequestHandler<GetSocioByIdQuery, SocioDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetSocioByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SocioDetailDto> Handle(GetSocioByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Socios
                .FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new KeyNotFoundException($"El socio con ID {request.Id} no existe.");
            }

            return new SocioDetailDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Apellido = entity.Apellido,
                Email = entity.Email,
                Telefono = entity.Telefono,
                Dni = entity.Dni
            };
        }
    }
}