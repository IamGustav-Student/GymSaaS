using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClasesPortal
{
    public class ClasePortalDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }

        // Estado de cupos
        public int CuposTotales { get; set; }
        public int CuposReservados { get; set; }
        public int CuposDisponibles => CuposTotales - CuposReservados;

        // Estado personal del alumno
        public bool YaReservada { get; set; }
        public string EstadoReserva { get; set; } = string.Empty;
    }

    public record GetClasesPortalQuery(int SocioId) : IRequest<List<ClasePortalDto>>;

    public class GetClasesPortalQueryHandler : IRequestHandler<GetClasesPortalQuery, List<ClasePortalDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClasesPortalQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClasePortalDto>> Handle(GetClasesPortalQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener al Socio SIN filtro global para conocer su TenantId
            // (Necesitamos saber a qué gimnasio pertenece este alumno)
            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) return new List<ClasePortalDto>();

            // 2. Traer Clases usando el TenantId del Socio
            // Usamos IgnoreQueryFilters() porque el usuario del Portal no tiene el contexto del Tenant inyectado
            var clases = await _context.Clases
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(c => c.TenantId == socio.TenantId && c.Activa && c.FechaHoraInicio > DateTime.Now)
                .OrderBy(c => c.FechaHoraInicio)
                .ToListAsync(cancellationToken);

            // 3. Traer reservas de ESTE socio
            var misReservas = await _context.Reservas
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(r => r.TenantId == socio.TenantId && r.SocioId == request.SocioId && r.Estado != "Cancelada")
                .ToListAsync(cancellationToken);

            // 4. Cruzar la información
            var resultado = clases.Select(c =>
            {
                var reserva = misReservas.FirstOrDefault(r => r.ClaseId == c.Id);
                return new ClasePortalDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Instructor = c.Instructor,
                    FechaHoraInicio = c.FechaHoraInicio,
                    DuracionMinutos = c.DuracionMinutos,
                    Precio = c.Precio,
                    CuposTotales = c.CupoMaximo,
                    CuposReservados = c.CupoReservado,
                    YaReservada = reserva != null,
                    EstadoReserva = reserva?.Estado ?? string.Empty
                };
            }).ToList();

            return resultado;
        }
    }
}