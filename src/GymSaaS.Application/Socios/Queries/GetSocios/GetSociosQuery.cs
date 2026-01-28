using MediatR;

namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    public record GetSociosQuery : IRequest<List<SocioDto>>;
}