# Documentación Técnica: Gymvo SaaS

Gymvo es una plataforma SaaS (Software as a Service) diseñada para la gestión integral de gimnasios, con un enfoque en la automatización del acceso (Self Check-in) y la fidelización de socios (Gamificación).

---

## 🏗️ Arquitectura del Sistema
El proyecto sigue los principios de **Arquitectura Limpia (Clean Architecture)**, dividida en cuatro capas principales:

1.  **GymSaaS.Domain**: Contiene las entidades base, enums e interfaces de dominio. Es el núcleo del sistema y no tiene dependencias externas.
2.  **GymSaaS.Application**: Implementa la lógica de negocio utilizando el patrón **CQRS (MediatR)**. Define los casos de uso (Commands/Queries).
3.  **GymSaaS.Infrastructure**: Implementa el acceso a datos (Entity Framework Core) y servicios externos (MercadoPago, WhatsApp, JWT).
4.  **GymSaaS.Web**: Capa de presentación (ASP.NET Core MVC/API), Middlewares y WebSockets (SignalR).

---

## 🔒 Multitenancy (Multi-inquilino)
Gymvo utiliza un modelo de **Base de Datos Compartida** con aislamiento lógico:
- Cada entidad que pertenece a un gimnasio implementa `IMustHaveTenant`.
- **Filtros Globales**: El [ApplicationDbContext](file:///c:/Users/iamgu/source/repos/IamGustav-Student/GymSaaS/src/GymSaaS.Infrastructure/Persistence/ApplicationDbContext.cs#13-20) aplica un filtro automático `e.TenantId == _currentTenantService.TenantId` a todas las consultas.
- **Middleware**: Un `TenantMiddleware` identifica al gimnasio actual a través del subdominio o código de acceso.

---

## ⚡ Componentes Core

### 1. Sistema de Acceso (Self Check-in)
Ubicado en [RegistrarIngresoQrCommand](file:///c:/Users/iamgu/source/repos/IamGustav-Student/GymSaaS/src/GymSaaS.Application/Asistencias/Commands/RegistrarIngresoQr/RegistrarIngresoQrCommand.cs#37-44).
- **Validación Geográfica**: Utiliza el algoritmo de **Haversine** para verificar que el socio esté dentro del radio permitido (ej. 100m) del gimnasio.
- **QR Dinámico**: Verifica el código QR escaneado contra el configurado en el Tenant.
- **Tiempo Real**: Al registrar un ingreso, se emite una notificación vía **SignalR (AccesoHub)** que actualiza el panel de recepción instantáneamente.

### 2. Gamificación y Notificaciones
- **Hitos de Asistencia**: El sistema cuenta las asistencias y dispara felicitaciones automáticas al alcanzar números clave (10, 50, 100 clases).
- **Integración WhatsApp**: Utiliza `WhatsAppNotificationService` para enviar recordatorios de deuda, felicitaciones y links de renovación.

### 3. Pagos y Suscripciones
- **Integración MercadoPago**: Gestiona tanto el pago de la suscripción del gimnasio a Gymvo como los cobros que el gimnasio realiza a sus socios.
- **Webhooks**: Un controlador dedicado procesa las notificaciones de MercadoPago para activar o renovar membresías automáticamente.

---

## 🛠️ Stack Tecnológico
- **Backend**: .NET 8.0 (C#)
- **Base de Datos**: SQL Server / Azure SQL (compatible con PostgreSQL)
- **ORM**: Entity Framework Core
- **Mensajería**: MediatR (In-process)
- **Comunicación**: SignalR (WebSockets)
- **Frontend**: Razor Pages / JavaScript (Vanilla) / CSS Moderno
- **Seguridad**: JWT (JSON Web Tokens) + ASP.NET Core Identity

---

## 📂 Estructura de Carpetas Clave
- `src/Application/Asistencias`: Lógica de registro y QR.
- `src/Application/Tenants`: Gestión de suscripciones y planes del SaaS.
- `src/Infrastructure/Persistence`: Migraciones y configuración de EF Core.
- `src/Web/Middlewares`: Control de acceso por suscripción vencida.

---

> [!NOTE]
> **Soft Delete**: El sistema implementa borrado lógico (`IsDeleted`) para evitar la pérdida de historial de socios y pagos, manteniendo la integridad referencial.
