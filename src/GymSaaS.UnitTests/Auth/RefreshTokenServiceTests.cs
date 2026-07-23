using FluentAssertions;
using GymSaaS.Domain.Entities;
using GymSaaS.Infrastructure.Services;
using GymSaaS.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymSaaS.UnitTests.Auth
{
    public class RefreshTokenServiceTests
    {
        private static RefreshTokenService CreateService(GymSaaS.Infrastructure.Persistence.ApplicationDbContext context)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:RefreshTokenDays"] = "30" })
                .Build();

            return new RefreshTokenService(context, config);
        }

        private static async Task<(GymSaaS.Infrastructure.Persistence.ApplicationDbContext Context, Usuario Usuario)> SeedUsuarioAsync()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService("tenant-1"));
            var usuario = new Usuario
            {
                Nombre = "Test",
                Email = "test@test.com",
                Password = "hash",
                Activo = true,
                Role = "Admin",
                TenantId = "tenant-1"
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync(CancellationToken.None);
            return (context, usuario);
        }

        [Fact]
        public async Task Rotar_ConTokenValido_DeberiaDevolverElUsuarioYUnTokenNuevo()
        {
            var (context, usuario) = await SeedUsuarioAsync();
            var service = CreateService(context);

            var token = await service.GenerarAsync(usuario.Id, CancellationToken.None);
            var resultado = await service.RotarAsync(token, CancellationToken.None);

            resultado.EsValido.Should().BeTrue();
            resultado.Usuario!.Id.Should().Be(usuario.Id);
            resultado.NuevoRefreshToken.Should().NotBeNullOrEmpty();
            resultado.NuevoRefreshToken.Should().NotBe(token);
        }

        [Fact]
        public async Task Rotar_ElMismoTokenDosVeces_LaSegundaDeberiaFallar()
        {
            // Un refresh token usado una vez queda revocado (rotación) — reusarlo
            // (por ejemplo si fue robado y el atacante y el dueño legítimo pelean
            // por usarlo) debe fallar la segunda vez.
            var (context, usuario) = await SeedUsuarioAsync();
            var service = CreateService(context);

            var token = await service.GenerarAsync(usuario.Id, CancellationToken.None);
            await service.RotarAsync(token, CancellationToken.None);
            var segundoIntento = await service.RotarAsync(token, CancellationToken.None);

            segundoIntento.EsValido.Should().BeFalse();
            segundoIntento.Usuario.Should().BeNull();
        }

        [Fact]
        public async Task Rotar_TokenInexistente_DeberiaFallar()
        {
            var (context, _) = await SeedUsuarioAsync();
            var service = CreateService(context);

            var resultado = await service.RotarAsync("token-que-nunca-existio", CancellationToken.None);

            resultado.EsValido.Should().BeFalse();
        }

        [Fact]
        public async Task Revocar_UnTokenValido_DeberiaInvalidarloParaFuturosRefresh()
        {
            var (context, usuario) = await SeedUsuarioAsync();
            var service = CreateService(context);

            var token = await service.GenerarAsync(usuario.Id, CancellationToken.None);
            await service.RevocarAsync(token, CancellationToken.None);

            var resultado = await service.RotarAsync(token, CancellationToken.None);
            resultado.EsValido.Should().BeFalse();
        }

        [Fact]
        public async Task Rotar_ConUsuarioDesactivado_DeberiaFallar()
        {
            var (context, usuario) = await SeedUsuarioAsync();
            var service = CreateService(context);

            var token = await service.GenerarAsync(usuario.Id, CancellationToken.None);
            usuario.Activo = false;
            await context.SaveChangesAsync(CancellationToken.None);

            var resultado = await service.RotarAsync(token, CancellationToken.None);
            resultado.EsValido.Should().BeFalse();
        }
    }
}
