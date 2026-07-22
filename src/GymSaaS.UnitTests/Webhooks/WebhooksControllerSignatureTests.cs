using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.UnitTests.TestHelpers;
using GymSaaS.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GymSaaS.UnitTests.Webhooks
{
    // Cubre el fix de seguridad del Sprint 1: antes el webhook de MercadoPago no
    // verificaba ninguna firma, así que cualquiera podía POSTear y activar un pago.
    public class WebhooksControllerSignatureTests
    {
        private static WebhooksController CreateController(IApplicationDbContext context, string? webhookSecret, out IMercadoPagoService mpService)
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["MercadoPago:WebhookSecret"].Returns(webhookSecret);

            mpService = Substitute.For<IMercadoPagoService>();
            mpService.ObtenerExternalReferenceAsync(Arg.Any<string>()).Returns(string.Empty);

            var controller = new WebhooksController(context, mpService, configuration, Substitute.For<ILogger<WebhooksController>>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            return controller;
        }

        private static (string ts, string v1) FirmarManifest(string secret, string dataId, string requestId)
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
            var hash = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest))
            ).ToLowerInvariant();
            return (ts, hash);
        }

        private static JsonElement BuildPaymentPayload(string dataId) =>
            JsonDocument.Parse($$"""{ "type": "payment", "data": { "id": "{{dataId}}" } }""").RootElement;

        [Fact]
        public async Task MercadoPagoWebhook_SinSecretoConfigurado_DeberiaAceptarSinExigirFirma()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var controller = CreateController(context, webhookSecret: null, out _);

            var result = await controller.MercadoPagoWebhook(BuildPaymentPayload("12345"));

            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task MercadoPagoWebhook_ConSecretoYFirmaValida_DeberiaAceptar()
        {
            const string secret = "un-secreto-de-prueba";
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var controller = CreateController(context, webhookSecret: secret, out _);

            var (ts, v1) = FirmarManifest(secret, "12345", "req-1");
            controller.ControllerContext.HttpContext.Request.Headers["x-signature"] = $"ts={ts},v1={v1}";
            controller.ControllerContext.HttpContext.Request.Headers["x-request-id"] = "req-1";

            var result = await controller.MercadoPagoWebhook(BuildPaymentPayload("12345"));

            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task MercadoPagoWebhook_ConSecretoYFirmaInvalida_DeberiaRechazar()
        {
            const string secret = "un-secreto-de-prueba";
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var controller = CreateController(context, webhookSecret: secret, out _);

            controller.ControllerContext.HttpContext.Request.Headers["x-signature"] = "ts=1700000000,v1=firma-truchada";
            controller.ControllerContext.HttpContext.Request.Headers["x-request-id"] = "req-1";

            var result = await controller.MercadoPagoWebhook(BuildPaymentPayload("12345"));

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task MercadoPagoWebhook_ConSecretoYSinHeadersDeFirma_DeberiaRechazar()
        {
            const string secret = "un-secreto-de-prueba";
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var controller = CreateController(context, webhookSecret: secret, out _);

            var result = await controller.MercadoPagoWebhook(BuildPaymentPayload("12345"));

            result.Should().BeOfType<UnauthorizedResult>();
        }
    }
}
