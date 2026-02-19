// =============================================================================
// ARCHIVO: INotificationService.cs
// CAPA: Application/Common/Interfaces
// PROPÓSITO: Define el CONTRATO (interfaz) que debe cumplir cualquier servicio
//            de notificaciones. Al usar una interfaz, el resto de la aplicación
//            no sabe NI LE IMPORTA si las notificaciones van por WhatsApp,
//            Email, SMS o simplemente se loguean. Eso es Clean Architecture.
//
// CAMBIOS EN ESTE ARCHIVO:
//   - Los 3 métodos originales (EnviarAlertaPagoFallido, EnviarConfirmacionPago,
//     EnviarNotificacion) se conservan INTACTOS para no romper nada.
//   - Se agregan nuevos métodos, uno por cada caso de uso de notificación.
//   - La implementación real (WhatsApp via Twilio/Meta API) está en Infrastructure.
//   - Si no tenés las credenciales de WhatsApp, la implementación hace mock (log).
// =============================================================================

namespace GymSaaS.Application.Common.Interfaces
{
    public interface INotificationService
    {
        // =====================================================================
        // MÉTODOS ORIGINALES — NO TOCAR, YA ESTÁN EN USO
        // =====================================================================

        /// <summary>
        /// Alerta cuando un pago automático (débito) falla en Mercado Pago.
        /// Se usa en EjecutarReintentosCommand.
        /// </summary>
        Task EnviarAlertaPagoFallido(string nombreUsuario, string telefono, DateTime fechaReintento);

        /// <summary>
        /// Confirmación de pago exitoso. Se usa en EjecutarReintentosCommand.
        /// </summary>
        Task EnviarConfirmacionPago(string nombreUsuario, string telefono, decimal monto);

        /// <summary>
        /// Método genérico que se usa para la notificación de lista de espera
        /// en CancelarReservaCommand.
        /// </summary>
        Task EnviarNotificacion(string telefono, string mensaje);

        // =====================================================================
        // MÓDULO 1: MEMBRESÍAS Y COBROS
        // =====================================================================

        /// <summary>
        /// Aviso automático cuando la membresía está próxima a vencer.
        /// Se invoca desde el CronJob de vencimientos (X días antes).
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio para personalizar el mensaje.</param>
        /// <param name="telefono">Número en formato internacional. Ej: +5491112345678</param>
        /// <param name="nombrePlan">Nombre del plan. Ej: "Plan Mensual Full"</param>
        /// <param name="diasRestantes">Cuántos días faltan para el vencimiento (5 o 3).</param>
        /// <param name="linkRenovacion">URL directa al portal para renovar.</param>
        Task EnviarAvisoVencimientoProximo(
            string nombreSocio,
            string telefono,
            string nombrePlan,
            int diasRestantes,
            string linkRenovacion);

        /// <summary>
        /// Confirmación detallada de pago con resumen del plan adquirido.
        /// Se diferencia del método original en que incluye info del plan.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="nombrePlan">Nombre del plan contratado.</param>
        /// <param name="monto">Monto cobrado.</param>
        /// <param name="fechaVencimiento">Hasta cuándo es válida la membresía.</param>
        Task EnviarConfirmacionPagoDetallada(
            string nombreSocio,
            string telefono,
            string nombrePlan,
            decimal monto,
            DateTime fechaVencimiento);

        /// <summary>
        /// Recordatorio para socios con membresía vencida o saldo pendiente.
        /// Se usa cuando el acceso es denegado por membresía vencida.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="linkRenovacion">URL directa para renovar desde el portal.</param>
        Task EnviarRecordatorioDeuda(
            string nombreSocio,
            string telefono,
            string linkRenovacion);

        // =====================================================================
        // MÓDULO 2: CLASES Y RESERVAS
        // =====================================================================

        /// <summary>
        /// Confirmación inmediata cuando el socio reserva una clase desde el portal.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="nombreClase">Nombre de la clase. Ej: "Yoga Matutino"</param>
        /// <param name="instructor">Nombre del instructor.</param>
        /// <param name="fechaHora">Fecha y hora de inicio de la clase.</param>
        Task EnviarConfirmacionReserva(
            string nombreSocio,
            string telefono,
            string nombreClase,
            string? instructor,
            DateTime fechaHora);

        /// <summary>
        /// Recordatorio 1 hora antes del inicio de la clase para reducir el ausentismo.
        /// Se invoca desde el CronJob de recordatorios de clases.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="nombreClase">Nombre de la clase.</param>
        /// <param name="horaInicio">Hora de inicio para mostrar en el mensaje.</param>
        Task EnviarRecordatorioClase(
            string nombreSocio,
            string telefono,
            string nombreClase,
            DateTime horaInicio);

        /// <summary>
        /// Aviso urgente cuando el gimnasio cancela una clase por imprevistos.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio afectado.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="nombreClase">Nombre de la clase cancelada.</param>
        /// <param name="fechaHora">Fecha y hora original de la clase.</param>
        Task EnviarAvísoCancelacionClase(
            string nombreSocio,
            string telefono,
            string nombreClase,
            DateTime fechaHora);

        // =====================================================================
        // MÓDULO 3: CONTROL DE ACCESOS Y ONBOARDING
        // =====================================================================

        /// <summary>
        /// Mensaje de bienvenida al registrar un nuevo socio.
        /// Incluye su usuario y link al portal del alumno.
        /// </summary>
        /// <param name="nombreSocio">Nombre del nuevo socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="dni">DNI que usa como usuario para ingresar al portal.</param>
        /// <param name="linkPortal">URL del portal del alumno.</param>
        Task EnviarBienvenidaNuevoSocio(
            string nombreSocio,
            string telefono,
            string dni,
            string linkPortal);

        /// <summary>
        /// Mensaje motivacional cuando un socio lleva N días sin asistir.
        /// Estrategia de retención. Se invoca desde CronJob de inactividad.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio inactivo.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="diasSinAsistir">Cantidad de días sin registrar asistencia.</param>
        Task EnviarAlertaInactividad(
            string nombreSocio,
            string telefono,
            int diasSinAsistir);

        /// <summary>
        /// Notificación instantánea cuando el acceso QR es denegado por membresía vencida.
        /// Le manda el link de renovación directo al celular.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="linkRenovacion">URL directa para renovar.</param>
        Task EnviarNotificacionAccesoDenegado(
            string nombreSocio,
            string telefono,
            string linkRenovacion);

        // =====================================================================
        // MÓDULO 4: ENTRENAMIENTO Y RUTINAS
        // =====================================================================

        /// <summary>
        /// Aviso cuando el coach asigna o actualiza una rutina al socio.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="nombreRutina">Nombre de la rutina asignada.</param>
        /// <param name="linkPortal">Link para ver la rutina en el portal.</param>
        Task EnviarNotificacionRutinaAsignada(
            string nombreSocio,
            string telefono,
            string nombreRutina,
            string linkPortal);

        /// <summary>
        /// Felicitación automática por hitos de asistencia (gamificación).
        /// Se dispara cuando el socio completa un número redondo de asistencias.
        /// </summary>
        /// <param name="nombreSocio">Nombre del socio.</param>
        /// <param name="telefono">Teléfono en formato internacional.</param>
        /// <param name="totalAsistencias">Total de asistencias acumuladas.</param>
        /// <param name="nivelActual">Nivel de gamificación alcanzado.</param>
        Task EnviarFelicitacionLogro(
            string nombreSocio,
            string telefono,
            int totalAsistencias,
            string nivelActual);

        // =====================================================================
        // MÓDULO 5: ADMINISTRACIÓN (NOTIFICACIONES AL DUEÑO DEL GIMNASIO)
        // =====================================================================

        /// <summary>
        /// Alerta al dueño del gimnasio sobre el estado de su suscripción a Gymvo.
        /// Se envía cuando la suscripción está próxima a vencer.
        /// </summary>
        /// <param name="nombreGimnasio">Nombre del gimnasio/tenant.</param>
        /// <param name="telefonoDueno">Teléfono del dueño en formato internacional.</param>
        /// <param name="diasRestantes">Días que faltan para que venza la suscripción.</param>
        /// <param name="linkRenovacion">URL para renovar la suscripción de Gymvo.</param>
        Task EnviarAlertaSuscripcionGimnasio(
            string nombreGimnasio,
            string telefonoDueno,
            int diasRestantes,
            string linkRenovacion);

        /// <summary>
        /// Resumen diario de métricas clave enviado al dueño del gimnasio.
        /// Ej: "Hoy ingresaron 40 personas y se vendieron 3 membresías."
        /// </summary>
        /// <param name="nombreGimnasio">Nombre del gimnasio.</param>
        /// <param name="telefonoDueno">Teléfono del dueño.</param>
        /// <param name="ingresosHoy">Cantidad de accesos registrados hoy.</param>
        /// <param name="membresíasVendidas">Membresías vendidas hoy.</param>
        /// <param name="recaudacionHoy">Total recaudado hoy.</param>
        Task EnviarResumenDiario(
            string nombreGimnasio,
            string telefonoDueno,
            int ingresosHoy,
            int membresíasVendidas,
            decimal recaudacionHoy);
    }
}