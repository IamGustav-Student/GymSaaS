using GymSaaS.Application;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Web.Filters;
using GymSaaS.Web.Services;
using GymSaaS.Web.Hubs; // Importante: Namespace del Hub
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. INYECCIÓN DE DEPENDENCIAS (CAPAS)
// ==========================================

// A. Capas del Core
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Servicios Web
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();

// C. Tenant Service
builder.Services.AddScoped<ICurrentTenantService, WebCurrentTenantService>();

// D. SignalR (NUEVO)
builder.Services.AddSignalR();

// ==========================================
// 2. CONFIGURACIÓN DE SEGURIDAD Y MVC
// ==========================================

// Cookies (Tu configuración mejorada)
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

// Sesión (Tu configuración)
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

// ==========================================
// 3. PIPELINE DE PETICIONES (MIDDLEWARE)
// ==========================================

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

app.UseRouting();

// MIDDLEWARE DE TENANT (Orden Correcto)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseSession(); // Sesión antes de Auth

app.UseAuthentication();
app.UseAuthorization();

// ENDPOINTS
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default al Portal

// Endpoint del Hub (NUEVO)
app.MapHub<AccesoHub>("/accesoHub");

app.Run();