# ==========================================
# ETAPA 1: BASE (Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ==========================================
# ETAPA 2: BUILD (SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copiar primero los .csproj para aprovechar el caché de capas de Docker
COPY ["src/GymSaaS.Web/GymSaaS.Web.csproj", "src/GymSaaS.Web/"]
COPY ["src/GymSaaS.Application/GymSaaS.Application.csproj", "src/GymSaaS.Application/"]
COPY ["src/GymSaaS.Domain/GymSaaS.Domain.csproj", "src/GymSaaS.Domain/"]
COPY ["src/GymSaaS.Infrastructure/GymSaaS.Infrastructure.csproj", "src/GymSaaS.Infrastructure/"]

# 2. Restaurar dependencias
RUN dotnet restore "./src/GymSaaS.Web/GymSaaS.Web.csproj"

# 3. Copiar todo el resto del código fuente
COPY . .
WORKDIR "/src/src/GymSaaS.Web"

# 4. Compilar
RUN dotnet build "./GymSaaS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ==========================================
# ETAPA 3: PUBLISH
# ==========================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GymSaaS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ==========================================
# ETAPA 4: FINAL
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymSaaS.Web.dll"]