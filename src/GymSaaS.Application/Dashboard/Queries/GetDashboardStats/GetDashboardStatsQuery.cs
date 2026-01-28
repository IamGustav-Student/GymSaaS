using MediatR;

namespace GymSaaS.Application.Dashboard.Queries.GetDashboardStats
{
    // Una petición simple que devuelve nuestro DTO
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
}