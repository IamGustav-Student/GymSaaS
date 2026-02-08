using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClaseById
{
    // DTO Principal para el Detalle
    public class ClaseDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public int CupoMaximo { get; set; }
        public int CupoReservado { get; set; }
        public decimal Precio { get; set; }
        public bool Activa { get; set; }

        public List<AsistenteDto> Reservas { get; set; } = new();
        public List<AsistenteDto> ListaEspera { get; set; } = new();
    }

    // DTO Ligero para mostrar personas
    public class AsistenteDto
    {
        public int SocioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public record GetClaseByIdQuery(int Id) : IRequest<ClaseDetailDto?>;

    public class GetClaseByIdQueryHandler : IRequestHandler<GetClaseByIdQuery, ClaseDetailDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetClaseByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClaseDetailDto?> Handle(GetClaseByIdQuery request, CancellationToken cancellationToken)
        {
            var clase = await _context.Clases
                .Include(c => c.Reservas).ThenInclude(r => r.Socio)
                .Include(c => c.ListaEspera).ThenInclude(l => l.Socio)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (clase == null) return null;

            return new ClaseDetailDto
            {
                Id = clase.Id,
                Nombre = clase.Nombre,
                Instructor = clase.Instructor,
                FechaHoraInicio = clase.FechaHoraInicio,
                DuracionMinutos = clase.DuracionMinutos,
                CupoMaximo = clase.CupoMaximo,
                CupoReservado = clase.Reservas.Count(r => r.Activa), // Contamos reales
                Precio = clase.Precio,
                Activa = clase.Activa,

                // Mapeo de Confirmados
                Reservas = clase.Reservas
                    .Where(r => r.Activa)
                    .Select(r => new AsistenteDto
                    {
                        SocioId = r.SocioId,
                        NombreCompleto = $"{r.Socio?.Nombre} {r.Socio?.Apellido}",
                        Dni = r.Socio?.Dni ?? "N/A",
                        FotoUrl = r.Socio?.FotoUrl,
                        FechaRegistro = r.FechaReserva
                    }).ToList(),

                // Mapeo de Lista de Espera (Ordenados por antigüedad)
                ListaEspera = clase.ListaEspera
                    .OrderBy(l => l.FechaRegistro)
                    .Select(l => new AsistenteDto
                    {
                        SocioId = l.SocioId,
                        NombreCompleto = $"{l.Socio?.Nombre} {l.Socio?.Apellido}",
                        Dni = l.Socio?.Dni ?? "N/A",
                        FotoUrl = l.Socio?.FotoUrl,
                        FechaRegistro = l.FechaRegistro
                    }).ToList()
            };
        }
    }
}