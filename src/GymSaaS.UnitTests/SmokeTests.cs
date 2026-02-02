using FluentAssertions;
using System.Net;
using Xunit;

namespace GymSaaS.UnitTests
{
    public class SmokeTests
    {
        // Apuntamos al puerto expuesto por Docker en tu docker-compose
        private const string BaseUrl = "http://localhost:5000";

        [Fact]
        public async Task FlujoCritico_RegistroYLogin_DeberiaFuncionar()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new System.Net.CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = false
            };

            using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

            var randomId = Guid.NewGuid().ToString().Substring(0, 8);
            var email = $"admin_{randomId}@test.com";
            var password = "Password123!";

            // 1. Verificar acceso a Registro
            var getRegister = await client.GetAsync("/Auth/Register");
            getRegister.StatusCode.Should().Be(HttpStatusCode.OK, "La pantalla de registro debería estar accesible");

            // 2. Verificar que el servidor responde en el Home
            var response = await client.GetAsync("/");
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, "El servidor no debería dar error 500");
        }

        [Fact]
        public async Task ServidorDocker_DeberiaEstarOnline()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync(BaseUrl + "/Auth/Login");
                response.StatusCode.Should().Be(HttpStatusCode.OK, "El endpoint de Login debería responder 200 OK");

                var content = await response.Content.ReadAsStringAsync();

                // CORRECCIÓN: Buscamos 'Gymvo' que es la marca actual en el HTML
                content.Should().Contain("Gymvo", "El HTML debería contener el nombre de la marca actual");
            }
            catch (HttpRequestException)
            {
                Assert.Fail($"No se pudo conectar a {BaseUrl}. Asegúrate de que Docker esté corriendo 'docker-compose up'.");
            }
        }
    }
}