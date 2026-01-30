using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    // La Query ahora devuelve nuestro SocioDto potenciado
    public record GetSocioByIdQuery(int Id) : IRequest<SocioDto>;

    public class GetSocioByIdQueryHandler : IRequestHandler<GetSocioByIdQuery, SocioDto>
    {
        private readonly IApplicationDbContext _context;

        public GetSocioByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SocioDto> Handle(GetSocioByIdQuery request, CancellationToken cancellationToken)
        {
            // Buscamos socio con sus membresías
            var entity = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (entity == null) return null; // O lanzar excepción NotFound

            // Mapeamos a SocioDto
            var dto = new SocioDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Apellido = entity.Apellido,
                NombreCompleto = $"{entity.Nombre} {entity.Apellido}",
                Dni = entity.Dni,
                Email = entity.Email,
                Telefono = entity.Telefono,
                Estado = entity.Activo ? "Activo" : "Inactivo",
                // Mapeamos las membresías para el timeline
                Membresias = entity.Membresias.Select(m => new MembresiaDto
                {
                    Id = m.Id,
                    NombrePlan = m.TipoMembresia.Nombre,
                    FechaInicio = m.FechaInicio,
                    FechaFin = m.FechaFin,
                    Activa = m.Activa,
                    PrecioPagado = m.PrecioPagado,
                    Estado = CalcularEstado(m.Activa, m.FechaInicio, m.FechaFin)
                }).OrderByDescending(m => m.FechaFin).ToList()
            };

            return dto;
        }

        // Pequeña lógica para etiquetar visualmente
        private string CalcularEstado(bool activa, DateTime inicio, DateTime fin)
        {
            if (!activa) return "Inactiva";
            if (DateTime.Now > fin) return "Vencida";
            if (DateTime.Now < inicio) return "Futura"; // Aquí se ve el Stacking
            return "En Curso";
        }
    }
}