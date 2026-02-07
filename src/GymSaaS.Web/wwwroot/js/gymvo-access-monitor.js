// gymvo-access-monitor.js
// Escucha eventos de acceso y reproduce audio TTS

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/accesoHub")
    .withAutomaticReconnect()
    .build();

// Recuperamos el TenantId del contexto actual (inyectado en el Layout)
const currentTenantId = document.getElementById('meta-tenant-id')?.value;

if (currentTenantId) {
    connection.start().then(function () {
        console.log("🟢 Monitor de Acceso Conectado. Tenant: " + currentTenantId);

        // Nos unimos al "canal" privado de este gimnasio
        connection.invoke("UnirseAlGrupoGym", currentTenantId).catch(err => console.error(err));

    }).catch(function (err) {
        return console.error("🔴 Error SignalR: " + err.toString());
    });

    // ESCUCHAR EVENTO: "RecibirIngreso"
    connection.on("RecibirIngreso", function (nombre, fotoUrl, mensaje) {
        mostrarNotificacionAcceso(nombre, fotoUrl, mensaje);
        hablarBienvenida(mensaje);
    });
}

function mostrarNotificacionAcceso(nombre, fotoUrl, mensaje) {
    // Llenar datos del Modal/Toast
    const imgElement = document.getElementById('toastAccesoFoto');
    const titleElement = document.getElementById('toastAccesoTitulo');
    const msgElement = document.getElementById('toastAccesoMensaje');

    if (imgElement) imgElement.src = fotoUrl || '/img/default-avatar.png';
    if (titleElement) titleElement.innerText = nombre;
    if (msgElement) msgElement.innerText = "Acceso Correcto";

    // Mostrar Bootstrap Toast
    const toastLiveExample = document.getElementById('liveToastAcceso');
    if (toastLiveExample) {
        const toast = new bootstrap.Toast(toastLiveExample);
        toast.show();
    }
}

function hablarBienvenida(texto) {
    if ('speechSynthesis' in window) {
        // Cancelar cualquier audio anterior para no solaparse
        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(texto);
        utterance.lang = 'es-ES'; // Español
        utterance.rate = 1.1; // Un poco más rápido y dinámico
        utterance.pitch = 1;

        window.speechSynthesis.speak(utterance);
    }
}