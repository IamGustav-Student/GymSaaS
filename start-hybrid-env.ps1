# =============================================================================
# Gymvo Hybrid Local Orchestrator
# Proposito: Iniciar la API Central y la App Desktop simultaneamente.
# =============================================================================

Write-Host "--- Iniciando Entorno Hibrido Gymvo ---" -ForegroundColor Cyan

# 1. Verificar Docker (Base de Datos)
Write-Host "Checking Database Container..." -ForegroundColor Yellow
$dbContainer = docker ps -q -f name=gymsaas_db
if (-not $dbContainer) {
    Write-Host "DB container is not running. Starting it..." -ForegroundColor Red
    docker-compose up -d db
    Start-Sleep -Seconds 5
} else {
    Write-Host "Database is ready." -ForegroundColor Green
}

# 2. Iniciar API Central (Hub) en segundo plano
Write-Host "Starting API Central (Hub) on http://localhost:5000..." -ForegroundColor Yellow
$hubJob = Start-Job -ScriptBlock {
    cd "c:\Users\iamgu\source\repos\IamGustav-Student\GymSaaS\src\GymSaaS.Web"
    dotnet run --urls="http://localhost:5000"
}

# Esperar a que el Hub este listo
Write-Host "Waiting for Hub to warm up..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# 3. Iniciar Aplicacion Desktop
Write-Host "Starting Desktop Client..." -ForegroundColor Yellow
dotnet run --project "c:\Users\iamgu\source\repos\IamGustav-Student\GymSaaS\src\GymSaaS.Client.Desktop\GymSaaS.Client.Desktop.csproj"

# Limpieza al cerrar
Write-Host "--- Closing Environment ---" -ForegroundColor Cyan
Stop-Job $hubJob
