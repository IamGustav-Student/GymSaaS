using GymSaaS.Application;    // Extensiones de Application
using GymSaaS.Infrastructure; // Extensiones de Infrastructure
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. INYECCIÓN DE DEPENDENCIAS (CAPAS)
// ==========================================

// A. Capas del Core (Limpia)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Servicios Web
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor(); // Vital para leer Cookies

// C. Implementación Web del Tenant Service
// Sobrescribimos cualquier implementación previa para usar la versión que lee Cookies
builder.Services.AddScoped<ICurrentTenantService, WebCurrentTenantService>();

// ==========================================
// 2. CONFIGURACIÓN DE SEGURIDAD (AUTH)
// ==========================================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true; // Renueva cookie si el usuario está activo
        options.Cookie.HttpOnly = true;   // Seguridad contra XSS
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

// ==========================================
// 3. PIPELINE DE PETICIONES (MIDDLEWARE)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // ¿Quién sos?
app.UseAuthorization();  // ¿Qué podés hacer?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"); // Arrancamos en Login por defecto

app.Run();
