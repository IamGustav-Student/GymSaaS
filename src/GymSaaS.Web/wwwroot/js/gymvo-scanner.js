/**
 * Gymvo OS - Scanner Logic
 * Este archivo maneja la cámara, el envío de geolocalización y el feedback visual/auditivo.
 */

$(document).ready(function () {
    // 1. Instanciamos el objeto de la librería que ya cargamos en la vista
    const html5QrCode = new Html5Qrcode("reader");
    let isProcessing = false; // Bandera para evitar múltiples escaneos simultáneos

    const config = { fps: 10, qrbox: { width: 250, height: 250 } };

    // Iniciar la cámara trasera
    html5QrCode.start(
        { facingMode: "environment" },
        config,
        onScanSuccess
    ).catch(err => {
        console.error("Error al iniciar cámara: ", err);
        alert("No se pudo acceder a la cámara. Verifique los permisos.");
    });

    /**
     * Función que se ejecuta cuando se detecta un código QR
     */
    async function onScanSuccess(decodedText, decodedResult) {
        if (isProcessing) return;
        isProcessing = true;

        console.log(`Código detectado: ${decodedText}`);

        // Obtenemos la ubicación del socio para el Geofencing
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                async (position) => {
                    const lat = position.coords.latitude;
                    const lon = position.coords.longitude;

                    // Enviamos los datos al servidor (Command: RegistrarIngresoQr)
                    await enviarAcceso(decodedText, lat, lon);
                },
                (error) => {
                    alert("Es obligatorio activar el GPS para validar tu ingreso al gimnasio.");
                    isProcessing = false;
                }
            );
        } else {
            alert("Tu navegador no soporta geolocalización.");
            isProcessing = false;
        }
    }

    /**
     * Comunicación con el Backend
     */
    async function enviarAcceso(qrData, lat, lon) {
        try {
            const response = await fetch('/Portal/RegistrarAsistencia', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    qrCode: qrData,
                    latitud: lat,
                    longitud: lon
                })
            });

            const result = await response.json();

            if (result.success) {
                // Detenemos la cámara
                html5QrCode.stop();

                // Feedback de Voz (TTS)
                hablarBienvenida(result.socioNombre);

                // Mostramos el Modal de Bienvenida
                $('#welcomeMessage').text(`Hola ${result.socioNombre}, bienvenido a tu entrenamiento.`);
                const modal = new bootstrap.Modal(document.getElementById('welcomeModal'));
                modal.show();
            } else {
                alert("Error: " + result.message);
                isProcessing = false;
            }
        } catch (err) {
            console.error("Error de red: ", err);
            alert("No se pudo conectar con el servidor.");
            isProcessing = false;
        }
    }

    /**
     * FUNCIÓN TTS: Hace que el sistema hable
     */
    function hablarBienvenida(nombre) {
        if ('speechSynthesis' in window) {
            const mensaje = new SpeechSynthesisUtterance();
            mensaje.text = `Hola ${nombre}, bienvenido`;
            mensaje.lang = 'es-AR'; // Acento argentino si está disponible
            mensaje.rate = 1.0;
            window.speechSynthesis.speak(mensaje);
        }
    }
});