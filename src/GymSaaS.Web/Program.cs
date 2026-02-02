using GymSaaS.Application;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure;
using GymSaaS.Infrastructure.Persistence; // Necesario para UseMigrationsEndPoint
using GymSaaS.Web.Filters; // Necesario para ApiExceptionFilterAttribute
using GymSaaS.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Necesario

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. INYECCIÓN DE DEPENDENCIAS (CAPAS)
// ==========================================

// A. Capas del Core
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Servicios Web
builder.Services.AddDatabaseDeveloperPageExceptionFilter(); // Útil para ver errores de BD en pantalla

builder.Services.AddHttpContextAccessor();

// C. Tenant Service
builder.Services.AddScoped<ICurrentTenantService, WebCurrentTenantService>();

// ==========================================
// 2. CONFIGURACIÓN DE SEGURIDAD Y MVC
// ==========================================

// Cookies (Tu configuración mejorada)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied"; // Agregado por seguridad
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// Sesión (Tu configuración)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC + FILTRO DE ERRORES (CRÍTICO: Agregado)
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<ApiExceptionFilterAttribute>());

var app = builder.Build();

// ==========================================
// 3. PIPELINE DE PETICIONES (MIDDLEWARE)
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint(); // Ayuda a aplicar migraciones desde el navegador si fallan
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// MIDDLEWARE DE TENANT (Orden Correcto)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();