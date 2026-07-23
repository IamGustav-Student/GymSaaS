using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Personal.Queries.GetPersonal
{
    public class PersonalDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public record GetPersonalQuery : IRequest<List<PersonalDto>>;

    public class GetPersonalQueryHandler : IRequestHandler<GetPersonalQuery, List<PersonalDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPersonalQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PersonalDto>> Handle(GetPersonalQuery request, CancellationToken cancellationToken)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .Select(u => new PersonalDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Role = u.Role,
                    Activo = u.Activo
                })
                .ToListAsync(cancellationToken);
        }
    }
}
