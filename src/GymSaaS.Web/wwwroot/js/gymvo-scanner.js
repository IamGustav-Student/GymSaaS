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