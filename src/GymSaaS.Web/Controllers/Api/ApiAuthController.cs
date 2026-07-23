using GymSaaS.Application.Auth.Commands.Login;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers.Api
{
    public record RefreshRequestDto(string RefreshToken);

    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenService _refreshTokenService;

        public ApiAuthController(
            IMediator mediator,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenService refreshTokenService)
        {
            _mediator = mediator;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUsuarioCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);

                var token = _jwtTokenGenerator.GenerateToken(
                    result.UsuarioId,
                    result.Nombre,
                    result.Email,
                    result.TenantId);

                var refreshToken = await _refreshTokenService.GenerarAsync(result.UsuarioId, cancellationToken);

                return Ok(new
                {
                    Token = token,
                    RefreshToken = refreshToken,
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

        // Cambia un access token vencido por uno nuevo sin pedir contraseña,
        // usando un refresh token todavía vigente. El refresh token se rota
        // (el usado queda revocado y se entrega uno nuevo) para limitar el daño
        // si alguna vez se filtra.
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { Message = "RefreshToken es requerido." });

            var resultado = await _refreshTokenService.RotarAsync(request.RefreshToken, cancellationToken);

            if (!resultado.EsValido || resultado.Usuario == null)
                return Unauthorized(new { Message = "Refresh token inválido, vencido o revocado." });

            var usuario = resultado.Usuario;
            var token = _jwtTokenGenerator.GenerateToken(usuario.Id, usuario.Nombre, usuario.Email, usuario.TenantId);

            return Ok(new
            {
                Token = token,
                RefreshToken = resultado.NuevoRefreshToken,
                usuario.Nombre,
                usuario.TenantId,
                usuario.Email
            });
        }

        // Logout: revoca el refresh token para que no pueda usarse de nuevo,
        // aunque el access token JWT actual siga siendo válido hasta que expire solo.
        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
                await _refreshTokenService.RevocarAsync(request.RefreshToken, cancellationToken);

            return Ok();
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
