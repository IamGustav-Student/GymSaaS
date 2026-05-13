using GymSaaS.Application.Auth.Commands.Login;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public ApiAuthController(IMediator mediator, IJwtTokenGenerator jwtTokenGenerator)
        {
            _mediator = mediator;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUsuarioCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);

                var token = _jwtTokenGenerator.GenerateToken(
                    result.UsuarioId,
                    result.Nombre,
                    result.Email,
                    result.TenantId);

                return Ok(new
                {
                    Token = token,
                    result.Nombre,
                    result.TenantId,
                    result.Email
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult GetMe()
        {
            return Ok(new
            {
                Name = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
