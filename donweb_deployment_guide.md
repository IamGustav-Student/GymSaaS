# Guía de Despliegue: DonWeb (Gymvo)

Esta guía explica el proceso para desplegar **Gymvo** en DonWeb utilizando sus planes específicos de hosting para .NET.

---

## Paso 1: Selección del Plan
En DonWeb, no elijas el hosting Linux clásico. Debes ir a la sección de **"Hosting ASP.NET"**.

1. **Plan Standard o Pro:**
   - Asegúrate de que incluya soporte para **.NET Core (últimas versiones)** y **MS SQL Server**.
   - DonWeb factura en ARS (Pesos), lo cual es ideal para evitar recargos de tarjeta en Argentina.

---

## Paso 2: Configuración de la Base de Datos
1. Entra a tu Panel de Control (Plesk).
2. Ve a **Bases de Datos** > **Añadir base de datos**.
3. Elige un nombre (ej: `gymvo_db`), crea un usuario y contraseña.
4. Anota los datos de conexión (el host suele ser `localhost` o una IP específica que te dará el panel).

---

## Paso 3: Configuración del Entorno
DonWeb (Plesk) permite configurar variables de entorno o usar el archivo `web.config`.

1. **Variables de Entorno:**
   - En Plesk, busca el ícono de **Variables de Entorno** para tu dominio.
   - Añade `MercadoPago__AccessToken`, `Security__EncryptionKey`, etc.
2. **Web.config:** Visual Studio generará uno automáticamente al publicar, pero asegúrate de que use las cadenas de producción.

---

## Paso 4: Despliegue (Dos Métodos)

### Método A: Publicar desde Visual Studio (Recomendado)
1. Clic derecho en el proyecto `GymSaaS.Web` > **Publicar**.
2. Selecciona **Servidor Web (IIS)** > **Web Deploy**.
3. DonWeb te enviará los datos de "Publicación mediante Web Deploy" (Servidor, Nombre del sitio, Usuario y Contraseña).
4. Haz clic en **Publicar**. Esto subirá solo los archivos necesarios y actualizará tu sitio.

### Método B: FTP Tradicional
1. En Visual Studio, publica en una **Carpeta local**.
2. Usa un cliente como **FileZilla** para subir el contenido de esa carpeta a la carpeta `httpdocs` de tu sitio en DonWeb.

---

## Paso 5: SSL y Dominio
1. En Plesk, ve a **Let's Encrypt** (suele estar en la pestaña de Seguridad).
2. Sigue los pasos para instalar el certificado gratuito. DonWeb lo renueva automáticamente cada 3 meses.
3. Asegúrate de forzar el tráfico a **HTTPS** desde la configuración de Hosting en el panel.

---

> [!CAUTION]
> **Compatibilidad de Versiones:** Antes de contratar, confirma que el servidor de DonWeb soporte la versión exacta de .NET que estás usando (ej: .NET 8.0). Si usas una versión muy nueva, podrías necesitar un **Cloud Server Windows** en lugar de un hosting compartido.
