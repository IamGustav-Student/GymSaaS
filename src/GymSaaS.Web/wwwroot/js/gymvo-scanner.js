document.addEventListener('DOMContentLoaded', () => {
    const formAcceso = document.querySelector('form[action="/Accesos/Registrar"]');
    const inputSocio = document.getElementById('socioIdInput'); // Asegúrate de poner este ID en tu input HTML
    const resultadoContainer = document.getElementById('resultado-acceso-container'); // Contenedor para mostrar feedback

    if (formAcceso) {
        formAcceso.addEventListener('submit', async (e) => {
            e.preventDefault(); // Detener recarga normal

            const socioId = inputSocio.value;
            if (!socioId) return;

            // Feedback visual inmediato (UX)
            inputSocio.disabled = true;
            if (resultadoContainer) resultadoContainer.innerHTML = '<div class="alert alert-info">Procesando...</div>';

            try {
                // LLAMADA A LA NUEVA API HÍBRIDA
                const response = await fetch('/Accesos/Registrar', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json' // CLAVE: Esto le dice al Controller que devuelva JSON
                    },
                    body: JSON.stringify({
                        SocioId: parseInt(socioId),
                        CodigoQrEscaneado: "", // Manual por ahora
                        LatitudUsuario: 0, // Implementar navigator.geolocation aquí luego
                        LongitudUsuario: 0
                    })
                });

                const data = await response.json();

                if (response.ok && data.exitoso) {
                    // ÉXITO
                    mostrarResultado(true, data.mensaje, data.nombreSocio, data.fotoUrl);
                    inputSocio.value = ''; // Limpiar para el siguiente
                } else {
                    // ERROR DE NEGOCIO (Rebotado)
                    mostrarResultado(false, data.mensaje || "Acceso Denegado", data.nombreSocio, data.fotoUrl);
                }

            } catch (error) {
                console.error('Error:', error);
                mostrarResultado(false, "Error de conexión. Intente nuevamente.");
            } finally {
                inputSocio.disabled = false;
                inputSocio.focus();
            }
        });
    }

    function mostrarResultado(exitoso, mensaje, nombre, foto) {
        // Aquí puedes usar SweetAlert2 si lo instalas, o manipular el DOM nativo
        // Ejemplo manipulando el Toast que vi en tu Layout o una tarjeta

        // Opción Simple: Actualizar un div en la pantalla
        const colorClass = exitoso ? 'alert-success' : 'alert-danger';
        const icon = exitoso ? '✅' : '⛔';

        const html = `
            <div class="alert ${colorClass} text-center p-4 animate__animated animate__fadeIn">
                <div class="display-1">${icon}</div>
                <h2 class="fw-bold">${exitoso ? 'ACCESO PERMITIDO' : 'ACCESO DENEGADO'}</h2>
                <p class="fs-4">${mensaje}</p>
                ${nombre ? `<p class="fw-bold text-uppercase">${nombre}</p>` : ''}
            </div>
        `;

        if (resultadoContainer) {
            resultadoContainer.innerHTML = html;
        } else {
            alert(`${exitoso ? 'SI' : 'NO'}: ${mensaje}`);
        }
    }
});
// gymvo-scanner.js
// Lógica para manejar el escaneo tanto de Socios como de Gimnasios

document.addEventListener('DOMContentLoaded', () => {
    const html5QrCode = new Html5Qrcode("reader"); // "reader" es el ID del div en el HTML
    const config = { fps: 10, qrbox: { width: 250, height: 250 } };

    const onScanSuccess = async (decodedText, decodedResult) => {
        // Detener el escaneo temporalmente para procesar
        await html5QrCode.pause(true);

        console.log(`Código detectado: ${decodedText}`);

        try {
            // Enviamos al controlador de Accesos
            const response = await fetch('/Accesos/Registrar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    // Si el código empieza con 'GYM-' es un Auto-Checkin
                    TenantCheckInCode: decodedText.startsWith('GYM-') ? decodedText : null,
                    // Si es un número, es un SocioId (Modo Monitor)
                    SocioId: !isNaN(decodedText) ? parseInt(decodedText) : null,
                    CodigoQrEscaneado: decodedText
                })
            });

            const data = await response.json();
            mostrarFeedback(data);

        } catch (error) {
            console.error("Error en el registro:", error);
        }

        // Reanudar después de 3 segundos
        setTimeout(() => html5QrCode.resume(), 3000);
    };

    html5QrCode.start({ facingMode: "environment" }, config, onScanSuccess);
});

function mostrarFeedback(data) {
    const container = document.getElementById('resultado-scanner');
    if (!container) return;

    const color = data.exitoso ? '#00f3ff' : '#ff00ff';
    container.innerHTML = `
        <div class="glass-panel p-4 animate__animated animate__zoomIn" style="border-color: ${color}">
            <img src="${data.fotoUrl || '/img/default-user.png'}" class="rounded-circle mb-2" style="width:80px">
            <h4 class="brand-font" style="color: ${color}">${data.nombreSocio}</h4>
            <p class="text-white">${data.mensaje}</p>
        </div>
    `;
}