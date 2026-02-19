using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymSaaS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            // Servicios existentes
            services.AddTransient<IPasswordHasher, PasswordHasher>();
            services.AddTransient<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddTransient<INotificationService, WhatsAppNotificationService>();

            // NUEVO: Servicio de Encriptación (Singleton porque la llave no cambia por request)
            services.AddSingleton<IEncryptionService, EncryptionService>();

            // Servicio de MercadoPago actualizado
            services.AddTransient<IMercadoPagoService, MercadoPagoService>();

            // ----------------------------------------------------------------
            // NUEVO: HttpClient para el WhatsAppNotificationService
            // ----------------------------------------------------------------
            // ¿POR QUÉ IHttpClientFactory EN LUGAR DE new HttpClient()?
            // HttpClient tiene un bug famoso: si creás uno nuevo por request,
            // agotás los sockets del sistema operativo (socket exhaustion).
            // IHttpClientFactory maneja un pool de conexiones por nosotros.
            //
            // El cliente "WhatsApp" puede ser configurado con timeout, headers
            // base u otras políticas de resiliencia (Polly) si se necesita.
            services.AddHttpClient("WhatsApp", client =>
            {
                // Timeout de 10 segundos para no bloquear el hilo del servidor
                // si la API de WhatsApp está lenta
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Registramos el servicio de notificaciones con su implementación real
            // Transient: se crea una instancia nueva cada vez que se inyecta.
            // Es correcto porque WhatsAppNotificationService es stateless.
            services.AddTransient<INotificationService, WhatsAppNotificationService>();

            return services;
        }
    }
}
