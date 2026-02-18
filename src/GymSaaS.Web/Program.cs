// ============================================================
// ¿QUÉ ES ESTE ARCHIVO?
// Program.cs es el punto de entrada de la aplicación ASP.NET Core.
// Es lo primero que se ejecuta cuando arranca el servidor.
// Tiene dos responsabilidades principales:
//   1. Registrar todos los servicios (inyección de dependencias)
//   2. Configurar el pipeline de middlewares (el orden en que se
//      procesan las requests HTTP)
//
// ¿QUÉ SE AGREGÓ EN ESTE ARCHIVO?
// Se agregó un bloque de AUTO-MIGRACIÓN justo después del app.Build().
// Este bloque revisa si hay migraciones pendientes y las aplica
// automáticamente al arrancar. Así no hay que acordarse de correr
// "dotnet ef database update" manualmente en cada despliegue.
//
// TODO LO DEMÁS ES IDÉNTICO AL ORIGINAL. No se modificó ni eliminó
// ninguna lógica existente.
// ============================================================

using GymSaaS.Application;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Web.Filters;
using GymSaaS.Web.Services;
using GymSaaS.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GymSaaS.Web.Middlewares;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore; // NUEVO: necesario para MigrateAsync() y GetPendingMigrationsAsync()
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. INYECCIÓN DE DEPENDENCIAS (CAPAS)
// ==========================================

// Persiste las claves de encriptación de cookies en el sistema de archivos del contenedor.
// Sin esto, al reiniciar el contenedor todas las cookies existentes se invalidan.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/app/keys"));

// A. Capas del Core
// Registra todos los servicios de la capa Application (MediatR, handlers, etc.)
builder.Services.AddApplicationServices();
// Registra todos los servicios de Infrastructure (DbContext, repositorios, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Servicios Web
// Muestra errores de migración amigables en el navegador (solo en desarrollo)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// Permite acceder a HttpContext desde cualquier servicio inyectado
builder.Services.AddHttpContextAccessor();

// *** CORRECCIÓN FASE 2: CACHÉ PARA MIDDLEWARE ***
// Necesario para que TenantResolutionMiddleware pueda cachear los tenants
// y no golpear la DB en cada request
builder.Services.AddMemoryCache();

// C. Tenant Service
// Registra el servicio que sabe qué gimnasio está usando en cada request.
// Se registra como Scoped porque depende del HttpContext (una instancia por request).
builder.Services.AddScoped<ICurrentTenantService, WebCurrentTenantService>();

// D. SignalR
// Necesario para el hub de acceso en tiempo real (escaneo QR)
builder.Services.AddSignalR();

// ==========================================
// 2. CONFIGURACIÓN DE SEGURIDAD Y MVC
// ==========================================

// Cookies: configura el sistema de autenticación basado en cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Rutas de redirección para login/logout/acceso denegado
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        // La cookie dura 8 horas y se renueva con cada request (SlidingExpiration)
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // HttpOnly: la cookie no es accesible desde JavaScript (protección XSS)
        options.Cookie.HttpOnly = true;
        // IsEssential: no requiere consentimiento de cookies del usuario
        options.Cookie.IsEssential = true;
    });

// Sesión: para datos temporales entre requests (ej: mensajes de error)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC con filtro global de excepciones
// ApiExceptionFilterAttribute captura errores no controlados y los formatea
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<ApiExceptionFilterAttribute>());

var app = builder.Build();

// ============================================================
// NUEVO: AUTO-MIGRACIÓN AL ARRANCAR LA APLICACIÓN
// ============================================================
// ¿POR QUÉ ESTÁ AQUÍ?
// Debe ejecutarse DESPUÉS de app.Build() (para que el contenedor de
// servicios esté listo) y ANTES de app.Run() (para que la DB esté
// al día antes de atender cualquier request).
//
// ¿CÓMO FUNCIONA INTERNAMENTE?
// EF Core mantiene una tabla llamada __EFMigrationsHistory en la DB.
// Cada vez que se aplica una migración, EF anota su nombre en esa tabla.
// GetPendingMigrationsAsync() compara las migraciones en el código .cs
// contra esa tabla, y devuelve las que todavía no están registradas.
// MigrateAsync() ejecuta el Up() de cada migración pendiente en orden.
//
// ¿POR QUÉ USAR UN SCOPE?
// ApplicationDbContext es un servicio "Scoped" (una instancia por request HTTP).
// Fuera de un request (como aquí, al arrancar), tenemos que crear
// manualmente un scope para poder resolver servicios Scoped.
// El using() garantiza que el scope se libere cuando terminemos.
//
// ¿QUÉ PASA CON EL TENANT?
// Durante la auto-migración, ICurrentTenantService tiene TenantId = null.
// Eso está bien: las migraciones modifican el ESQUEMA de la DB (estructura
// de tablas), no los DATOS. No necesitan saber de qué tenant se trata.
//
// ¿QUÉ PASA SI FALLA?
// Logueamos el error pero NO detenemos la app (no hacemos throw).
// Si la columna ya existe (porque la creaste con ALTER TABLE manualmente),
// la migración no se vuelve a ejecutar gracias a __EFMigrationsHistory.
// ============================================================
using (var scope = app.Services.CreateScope())
{
    // Obtenemos el logger para registrar qué pasa durante la migración
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Resolvemos ApplicationDbContext (la clase concreta, no la interfaz)
        // porque necesitamos acceder a Database.MigrateAsync() y
        // Database.GetPendingMigrationsAsync() que son propios de DbContext
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Preguntamos qué migraciones hay en el código pero no en la DB todavía
        var migracionesPendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (migracionesPendientes.Any())
        {
            // Registramos cuáles son para poder verlas en los logs del servidor
            logger.LogInformation(
                "Aplicando {Count} migracion(es) pendiente(s): {Nombres}",
                migracionesPendientes.Count,
                string.Join(", ", migracionesPendientes));

            // Ejecuta el Up() de cada migración pendiente en orden cronológico.
            // Equivale a correr "dotnet ef database update" desde la terminal,
            // pero de forma automática sin intervención manual.
            await db.Database.MigrateAsync();

            logger.LogInformation("Migraciones aplicadas correctamente.");
        }
        else
        {
            logger.LogInformation("Base de datos al dia. No hay migraciones pendientes.");
        }
    }
    catch (Exception ex)
    {
        // Si algo falla (DB no disponible, error de red, permisos, etc.)
        // lo registramos pero permitimos que la app arranque de todas formas.
        // Así el desarrollador puede ver el error en los logs y corregirlo,
        // sin que el contenedor quede en un loop de reinicios.
        logger.LogError(ex,
            "Error al aplicar migraciones al iniciar. " +
            "La base de datos puede estar desactualizada. " +
            "Verificar conexion y estado de la DB.");
    }
}
// ============================================================
// FIN DEL BLOQUE DE AUTO-MIGRACIÓN
// ============================================================

// ==========================================
// 3. PIPELINE DE PETICIONES (MIDDLEWARE)
// ==========================================
// IMPORTANTE: El orden de los middlewares importa muchísimo.
// Cada request HTTP pasa por todos estos middlewares en este orden exacto.

if (app.Environment.IsDevelopment())
{
    // En desarrollo: muestra la página de error detallada con el stack trace completo
    app.UseDeveloperExceptionPage();
    // Muestra errores de migración de EF en el navegador (muy útil en desarrollo)
    app.UseMigrationsEndPoint();
}
else
{
    // En producción: muestra una página de error genérica (no expone detalles internos)
    app.UseExceptionHandler("/Home/Error");
    // HSTS: le dice al navegador que solo use HTTPS para este sitio
    app.UseHsts();
}

// Redirige automáticamente todas las requests HTTP a HTTPS
app.UseHttpsRedirection();

// Sirve archivos estáticos desde la carpeta wwwroot/ (CSS, JS, imágenes, fonts)
app.UseStaticFiles();

// A. Tenant Resolution: determina qué gimnasio corresponde a este request.
// Lo hace por subdominio (ej: power-gym.misitio.com) o por claim del usuario.
// DEBE ir antes de Authentication para que cuando EF Core aplique los filtros
// globales de tenant, ya sepa qué TenantId usar.
app.UseMiddleware<TenantResolutionMiddleware>();

// B. Autenticación: ¿Quién es el usuario?
// Lee la cookie de sesión y carga los claims (SocioId, TenantId, Role, etc.)
app.UseAuthentication();

// C. El Muro de Pago: verifica si el tenant tiene la suscripción vigente.
// Se coloca aquí porque ya sabemos quién es el tenant (paso A)
// y ya sabemos quién es el usuario (paso B).
// Si el plan expiró, bloquea el acceso y redirige a la página de renovación.
app.UseMiddleware<SubscriptionCheckMiddleware>();

// Routing: analiza la URL y determina qué Controller y Action deben manejar el request
app.UseRouting();

// D. Autorización: ¿Tiene permiso para hacer esto?
// Verifica roles ([Authorize], [Authorize(Roles = "Admin")], etc.)
// DEBE ir DESPUÉS de UseAuthentication() (necesita saber quién es)
// y DESPUÉS de UseRouting() (necesita saber qué ruta es)
app.UseAuthorization();

// Sesión: carga y guarda datos temporales asociados al usuario entre requests
app.UseSession();

// ==========================================
// 4. ENDPOINTS
// ==========================================

// Mapea las URLs a los Controllers y Actions según el patrón:
// /Socios/Index ? SociosController.Index()
// /Portal/Login ? PortalController.Login()
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Hub de SignalR para notificaciones en tiempo real del control de acceso por QR
app.MapHub<AccesoHub>("/accesoHub");

// Inicia el servidor web y empieza a escuchar requests entrantes
app.Run();