using GymSaaS.Application.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApiDashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApiDashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var result = await _mediator.Send(new GetDashboardStatsQuery());
            return Ok(result);
        }
    }
}
