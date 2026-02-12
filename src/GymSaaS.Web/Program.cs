using GymSaaS.Application;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Web.Filters;
using GymSaaS.Web.Services;
using GymSaaS.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GymSaaS.Web.Middlewares; // Importante

var builder = WebApplication.CreateBuilder(args);

// ==========================================\
// 1. INYECCIÓN DE DEPENDENCIAS (CAPAS)
// ==========================================\

// A. Capas del Core
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Servicios Web
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpContextAccessor();

// *** CORRECCIÓN FASE 2: CACHÉ PARA MIDDLEWARE ***
builder.Services.AddMemoryCache(); // <--- IMPORTANTE: Habilita IMemoryCache

// C. Tenant Service
builder.Services.AddScoped<ICurrentTenantService, WebCurrentTenantService>();

// D. SignalR
builder.Services.AddSignalR();

// ==========================================\
// 2. CONFIGURACIÓN DE SEGURIDAD Y MVC
// ==========================================\

// Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// Sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC + FILTRO DE ERRORES 
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<ApiExceptionFilterAttribute>());

var app = builder.Build();

// ==========================================\
// 3. PIPELINE DE PETICIONES (MIDDLEWARE)
// ==========================================\

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();



// MIDDLEWARE DE TENANT (Orden Correcto)
app.UseMiddleware<TenantResolutionMiddleware>();

// B. Autenticación (¿Quién es el usuario?)
app.UseAuthentication();
// C. EL MURO DE PAGO (¿Pagó su cuota?) - NUEVO
// Se coloca aquí para saber ya quién es el Tenant, pero antes de procesar la ruta.
app.UseMiddleware<SubscriptionCheckMiddleware>();

app.UseRouting();

// D. Autorización (¿Tiene permiso de Admin?)
app.UseAuthorization();


app.UseSession();


// ==========================================\
// 4. ENDPOINTS
// ==========================================\

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<AccesoHub>("/accesoHub");

app.Run();