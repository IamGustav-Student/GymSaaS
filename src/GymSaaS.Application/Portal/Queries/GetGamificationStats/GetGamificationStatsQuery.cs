using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Portal.Queries.GetGamificationStats
{
    public record GetGamificationStatsQuery(int SocioId) : IRequest<GamificationStatsDto>;

    public class GetGamificationStatsQueryHandler : IRequestHandler<GetGamificationStatsQuery, GamificationStatsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetGamificationStatsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GamificationStatsDto> Handle(GetGamificationStatsQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener Datos Base (Optimizada: Solo traemos fechas, no toda la entidad)
            var asistenciasFechas = await _context.Asistencias
                .AsNoTracking()
                .Where(a => a.SocioId == request.SocioId && a.Permitido)
                .OrderByDescending(a => a.FechaHora)
                .Select(a => a.FechaHora.Date)
                .ToListAsync(cancellationToken);

            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            var totalAsistencias = asistenciasFechas.Count;

            // 2. Algoritmo de Racha (Streak)
            int racha = 0;
            if (totalAsistencias > 0)
            {
                var fechasUnicas = asistenciasFechas.Distinct().ToList(); // Eliminamos duplicados del mismo día
                var ultimaFecha = fechasUnicas.First();
                var hoy = DateTime.UtcNow.Date; // Ojo: Idealmente usar TimeZone del Tenant

                // La racha sigue viva si fuiste hoy o ayer
                if (ultimaFecha == hoy || ultimaFecha == hoy.AddDays(-1))
                {
                    racha = 1;
                    for (int i = 0; i < fechasUnicas.Count - 1; i++)
                    {
                        // Si la diferencia entre fecha actual y siguiente es 1 día, suma
                        if ((fechasUnicas[i] - fechasUnicas[i + 1]).TotalDays == 1)
                        {
                            racha++;
                        }
                        else
                        {
                            break; // Se rompió la cadena
                        }
                    }
                }
            }

            // 3. Sistema de Niveles (Gamificación)
            string nivel = "Novato";
            string color = "secondary";
            int metaSiguiente = 10;

            if (totalAsistencias >= 500) { nivel = "Leyenda"; color = "info"; metaSiguiente = 1000; }
            else if (totalAsistencias >= 100) { nivel = "Elite"; color = "warning"; metaSiguiente = 500; }
            else if (totalAsistencias >= 50) { nivel = "Avanzado"; color = "danger"; metaSiguiente = 100; }
            else if (totalAsistencias >= 10) { nivel = "Intermedio"; color = "success"; metaSiguiente = 50; }

            // Cálculo de Progreso (Evitar división por cero)
            // Asumimos un sistema escalonado simple
            int baseNivelAnterior = nivel switch
            {
                "Intermedio" => 10,
                "Avanzado" => 50,
                "Elite" => 100,
                "Leyenda" => 500,
                _ => 0
            };

            double progreso = 0;
            if (metaSiguiente > baseNivelAnterior)
            {
                progreso = (double)(totalAsistencias - baseNivelAnterior) / (metaSiguiente - baseNivelAnterior) * 100;
            }
            if (progreso > 100) progreso = 100;

            return new GamificationStatsDto
            {
                RachaActual = racha,
                TotalAsistencias = totalAsistencias,
                NivelActual = nivel,
                ColorNivel = color,
                AsistenciasParaSiguienteNivel = metaSiguiente - totalAsistencias,
                PorcentajeProgreso = progreso,
                NombreSocio = socio?.Nombre ?? "Atleta",
                FotoUrl = socio?.FotoUrl
            };
        }
    }
}