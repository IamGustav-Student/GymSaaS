using GymSaaS.SharedUI.Services;
using GymSaaS.SharedUI.Models;
using GymSaaS.Client.Desktop.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;

namespace GymSaaS.Client.Desktop.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private string? _token;
        private string? _tenantId;
        public bool IsOffline { get; private set; }

        public ApiService(IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClientFactory.CreateClient("HubAPI");
            _scopeFactory = scopeFactory;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    _token = result?.Token;
                    _tenantId = result?.TenantId;

                    if (!string.IsNullOrEmpty(_token))
                    {
                        await SaveSessionAsync(_token, _tenantId ?? "");
                    }

                    return true;
                }
            }
            catch { /* Fallback to false */ }
            return false;
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            try 
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                await db.Database.EnsureCreatedAsync(); // Asegurar esquema

                var tokenSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "SessionToken");
                var tenantSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "TenantId");

                if (tokenSetting != null && !string.IsNullOrEmpty(tokenSetting.Value))
                {
                    _token = tokenSetting.Value;
                    _tenantId = tenantSetting?.Value;
                    Console.WriteLine("[SESSION] Sesión restaurada desde disco.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SESSION] Fallo al restaurar sesión: {ex.Message}");
            }
            return false;
        }

        private async Task SaveSessionAsync(string token, string tenantId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

            var tokenSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "SessionToken") ?? new LocalSetting { Key = "SessionToken" };
            tokenSetting.Value = token;
            if (db.Entry(tokenSetting).State == EntityState.Detached) db.Settings.Add(tokenSetting);

            var tenantSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "TenantId") ?? new LocalSetting { Key = "TenantId" };
            tenantSetting.Value = tenantId;
            if (db.Entry(tenantSetting).State == EntityState.Detached) db.Settings.Add(tenantSetting);

            await db.SaveChangesAsync();
        }

        public async Task<List<T>?> GetAsync<T>(string endpoint)
        {
            // 1. Intentar obtener de la API
            try
            {
                if (string.IsNullOrEmpty(_token)) return await GetLocalDataAsync<T>();

                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Add("X-Tenant-Id", _tenantId);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    IsOffline = false;
                    var data = await response.Content.ReadFromJsonAsync<List<T>>();
                    
                    // 2. Si son Socios, sincronizar con SQLite de forma asíncrona pero segura
                    if (data != null && typeof(T) == typeof(SocioDto))
                    {
                        _ = Task.Run(async () => {
                            try { await SyncSociosToLocalAsync(data as List<SocioDto>); }
                            catch (Exception ex) { Console.WriteLine($"[SYNC_LOG] Fallo en sincronización de fondo: {ex.Message}"); }
                        });
                    }
                    
                    return data;
                }
            }
            catch (Exception ex)
            {
                IsOffline = true;
                Console.WriteLine($"[OFFLINE] Error de conexión API: {ex.Message}. Intentando carga local...");
            }

            // 3. Fallback a datos locales si la API falla o no hay conexión
            return await GetLocalDataAsync<T>();
        }

        public async Task<T?> GetSingleAsync<T>(string endpoint)
        {
            try
            {
                if (string.IsNullOrEmpty(_token)) return default;

                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Add("X-Tenant-Id", _tenantId);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API_GET_ERROR] Endpoint: {endpoint} | Status: {response.StatusCode} | Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE] Error en GetSingleAsync ({endpoint}): {ex.Message}");
            }
            return default;
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                if (string.IsNullOrEmpty(_token)) return default;

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Add("X-Tenant-Id", _tenantId);
                request.Content = JsonContent.Create(data);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API_POST_ERROR] Endpoint: {endpoint} | Status: {response.StatusCode} | Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API_POST_ERROR] Error crítico en PostAsync ({endpoint}): {ex.Message}");
            }
            return default;
        }

        public async Task PutAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                if (string.IsNullOrEmpty(_token)) return;

                var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Add("X-Tenant-Id", _tenantId);
                request.Content = JsonContent.Create(data);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API_PUT_ERROR] Endpoint: {endpoint} | Status: {response.StatusCode} | Error: {error}");
                    throw new Exception($"Error API (PUT): {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API_PUT_ERROR] Error crítico en PutAsync ({endpoint}): {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string endpoint)
        {
            try
            {
                if (string.IsNullOrEmpty(_token)) return;

                var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Add("X-Tenant-Id", _tenantId);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error API (DELETE): {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API_DELETE_ERROR] Error en DeleteAsync: {ex.Message}");
                throw;
            }
        }

        private async Task<List<T>?> GetLocalDataAsync<T>()
        {
            if (typeof(T) != typeof(SocioDto)) return new List<T>();

            try 
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                var localSocios = await db.Socios.ToListAsync();

                var dtos = localSocios.Select(s => new SocioDto
                {
                    Id = s.Id,
                    NombreCompleto = s.NombreCompleto,
                    Dni = s.Dni,
                    Email = s.Email ?? "",
                    Estado = s.Estado,
                    UltimoAcceso = s.UltimoAcceso
                }).ToList();

                IsOffline = dtos.Any(); // Estamos en offline si logramos cargar algo localmente
                return dtos as List<T>;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE] Error crítico al leer de SQLite: {ex.Message}");
                return new List<T>(); // Nunca retornar null para evitar crash en UI
            }
        }

        private async Task SyncSociosToLocalAsync(List<SocioDto>? dtos)
        {
            if (dtos == null || !dtos.Any()) return;

            try 
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

                // Asegurar que la DB esté lista (por si acaso el borrado previo)
                await db.Database.EnsureCreatedAsync();

                // Estrategia simple: Reemplazar caché local
                var currentLocal = await db.Socios.ToListAsync();
                if (currentLocal.Any())
                {
                    db.Socios.RemoveRange(currentLocal);
                    await db.SaveChangesAsync();
                }

                var newLocals = dtos.Select(d => new LocalSocio
                {
                    Id = d.Id,
                    NombreCompleto = d.NombreCompleto,
                    Dni = d.Dni,
                    Email = d.Email,
                    Estado = d.Estado,
                    UltimoAcceso = d.UltimoAcceso
                }).ToList();

                await db.Socios.AddRangeAsync(newLocals);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Un error en la sincronización no debe tumbar la App
                Console.WriteLine($"[SYNC_ERROR] Error al persistir datos en SQLite: {ex.Message}");
            }
        }

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
        }
    }
}
