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
        // --- LÓGICA HÍBRIDA PWA (OFFLINE SUPPORT) ---
        if (!navigator.onLine) {
            console.warn("Escaneando en modo offline...");
            await manejarAccesoOffline(qrData);
            return;
        }

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
                // Éxito Online
                finalizarAcceso(result.socioNombre);
            } else {
                alert("Error: " + result.message);
                isProcessing = false;
            }
        } catch (err) {
            console.error("Error de red, intentando modo offline: ", err);
            await manejarAccesoOffline(qrData);
        }
    }

    /**
     * LÓGICA OFFLINE: Busca al socio en IndexedDB
     */
    async function manejarAccesoOffline(qrData) {
        if (!window.GymvoDB) {
            alert("Sistema offline y base de datos local no disponible.");
            isProcessing = false;
            return;
        }

        const socio = await GymvoDB.findSocioByQR(qrData);

        if (socio) {
            // Guardamos la asistencia en la cola para sincronizar luego
            await GymvoDB.queueAsistencia(socio.id);
            
            // Intentar registrar el evento de fondo si el navegador lo soporta
            if ('serviceWorker' in navigator && 'SyncManager' in window) {
                const reg = await navigator.serviceWorker.ready;
                try {
                    await reg.sync.register('sync-asistencias');
                } catch (e) {
                    console.log("Background sync falló, se sincronizará manualmente al volver.");
                }
            }

            // Feedback visual de "Acceso Offline"
            mostrarFeedbackOffline(socio.nombre);
            finalizarAcceso(socio.nombre, true);
        } else {
            alert("Socio no encontrado en la base de datos local. Se requiere conexión para validar este código.");
            isProcessing = false;
        }
    }

    function finalizarAcceso(nombre, isOffline = false) {
        // Detenemos la cámara
        html5QrCode.stop();

        // Feedback de Voz (TTS)
        hablarBienvenida(nombre);

        // Mostramos el Modal de Bienvenida
        const prefijo = isOffline ? "[OFFLINE] " : "";
        $('#welcomeMessage').text(`${prefijo}Hola ${nombre}, bienvenido a tu entrenamiento.`);
        const modal = new bootstrap.Modal(document.getElementById('welcomeModal'));
        modal.show();
        
        if (isOffline) {
            $('#welcomeModal .modal-content').css('border', '2px solid var(--md-sys-color-error)');
        }
    }

    function mostrarFeedbackOffline(nombre) {
        console.log(`%c ⚡ ACCESO OFFLINE: ${nombre} `, 'background: #ff2a2a; color: #fff; font-weight: bold;');
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