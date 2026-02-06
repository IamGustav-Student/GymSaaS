using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    // CAMBIO CLAVE: Ahora devolvemos 'SocioDto?' en lugar de la entidad 'Socio?'
    public record GetSocioByIdQuery(int Id) : IRequest<SocioDto?>;

    public class GetSocioByIdQueryHandler : IRequestHandler<GetSocioByIdQuery, SocioDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetSocioByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SocioDto?> Handle(GetSocioByIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Buscamos la entidad con sus relaciones
            var entity = await _context.Socios
                .Include(s => s.Membresias)
                .ThenInclude(m => m.TipoMembresia)
                .AsNoTracking() // Optimización: Solo lectura
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (entity == null) return null;

            // 2. Mapeamos Manualmente Entidad -> DTO (La Solución Definitiva)
            return new SocioDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Apellido = entity.Apellido,
                NombreCompleto = $"{entity.Nombre} {entity.Apellido}",
                Dni = entity.Dni,
                Email = entity.Email,
                Telefono = entity.Telefono,
                Estado = entity.Activo ? "Activo" : "Inactivo",

                // Mapeamos el historial de membresías
                Membresias = entity.Membresias
                    .OrderByDescending(m => m.FechaFin)
                    .Select(m => new MembresiaDto
                    {
                        Id = m.Id,
                        NombrePlan = m.TipoMembresia.Nombre,
                        FechaInicio = m.FechaInicio,
                        FechaFin = m.FechaFin,
                        Activa = m.Activa,
                        PrecioPagado = m.PrecioPagado,
                        // Calculamos el estado visual
                        Estado = m.Activa ? "Vigente" : (DateTime.UtcNow > m.FechaFin ? "Vencida" : "Cancelada")
                    })
                    .ToList()
            };
        }
    }
}