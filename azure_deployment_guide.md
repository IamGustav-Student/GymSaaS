# Guía de Despliegue: Microsoft Azure (Gymvo)

Esta guía detalla el proceso completo para llevar **Gymvo** a producción utilizando la infraestructura de Microsoft.

---

## Paso 1: Selección del Plan
Para un SaaS como Gymvo, el enfoque recomendado es **PaaS (Platform as a Service)** para evitar gestionar servidores.

1. **App Service (Para el WebApp):**
   - Elige el plan **Basic (B1)** para empezar (~$13 USD/mes). Permite dominios personalizados y SSL.
   - Si esperas mucho tráfico, escala a **Standard (S1)**.
2. **Azure SQL Database (Para la Base de Datos):**
   - Elige el modelo de compra **DTU**.
   - Nivel **Basic** (5 DTUs, 2GB) es suficiente para el lanzamiento (~$5 USD/mes).

---

## Paso 2: Configuración en el Portal de Azure
1. **Crear un Grupo de Recursos:** Agrupa todos los servicios (ej: `rg-gymvo-prod`).
2. **Crear el App Service:**
   - Nombre: `gymvo-app` (será `gymvo-app.azurewebsites.net`).
   - Stack: `.NET 8` (o la versión que uses).
   - Sistema Operativo: **Windows** (recomendado para máxima compatibilidad con SQL Server).
3. **Crear Azure SQL Database:**
   - Configura el usuario administrador y contraseña.
   - **IMPORTANTE:** En la pestaña "Redes", activa la opción *"Permitir que los servicios de Azure accedan a este servidor"*.

---

## Paso 3: Configuración de Variables (Producción)
En el Portal de Azure, ve a tu App Service > **Configuración** > **Variables de entorno**:

1. **ConnectionStrings:** Añade `DefaultConnection` con la cadena que te da Azure SQL.
2. **App Settings:** Añade las variables de tu archivo [.env](file:///c:/Users/iamgu/source/repos/IamGustav-Student/GymSaaS/.env):
   - `MercadoPago__AccessToken`
   - `Security__EncryptionKey`
   - `ASPNETCORE_ENVIRONMENT` = `Production`

---

## Paso 4: Despliegue desde Visual Studio
1. Haz clic derecho en el proyecto `GymSaaS.Web` > **Publicar**.
2. Selecciona **Azure** > **Azure App Service (Windows)**.
3. Inicia sesión y selecciona tu suscripción y el App Service creado.
4. Haz clic en **Publicar**. Visual Studio compilará y subirá todo automáticamente.

---

## Paso 5: Dominio y SSL
1. En el App Service, ve a **Dominios personalizados**.
2. Agrega tu dominio (ej: `app.gymvo.com`) siguiendo las instrucciones de DNS (registros CNAME y TXT).
3. Azure ofrece **Certificados SSL Gratuitos** (App Service Managed Certificates). Actívalo una vez vinculado el dominio.

---

> [!TIP]
> **Migraciones de Base de Datos:** Si usas Entity Framework, puedes activar "Apply Migrations on Publish" en los ajustes de publicación de Visual Studio para que la base de datos se cree/actualice automáticamente al subir el código.
