/**
 * Gymvo OS - Molinete / Recepción
 * Lee DNI o código de acceso (tipeado o por lector de tarjeta/barcode como
 * teclado) y registra el ingreso vía RegistrarAccesoCommand. Sin cámara ni GPS:
 * el dispositivo ya está físicamente en el gimnasio.
 */
document.addEventListener('DOMContentLoaded', function () {
    const input = document.getElementById('inputAcceso');
    const resultado = document.getElementById('resultadoMolinete');
    let procesando = false;

    input.addEventListener('keydown', async function (e) {
        if (e.key !== 'Enter') return;
        e.preventDefault();

        const valor = input.value.trim();
        input.value = '';

        if (!valor || procesando) return;
        procesando = true;

        try {
            const response = await fetch('/Accesos/RegistrarMolinete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ input: valor })
            });

            const data = await response.json();
            mostrarResultado(data);
        } catch (err) {
            mostrarResultado({ permitido: false, mensaje: 'Error de conexión con el servidor.', color: 'danger' });
        } finally {
            procesando = false;
            input.focus();
        }
    });

    function mostrarResultado(dto) {
        const colorMap = { success: '#0aff0a', warning: '#f3ff0a', danger: '#ff2b2b' };
        const color = colorMap[dto.color] || colorMap.danger;

        resultado.style.border = `2px solid ${color}`;
        resultado.innerHTML = `
            <h3 style="color:${color};">${dto.socioNombre ?? ''}</h3>
            <p class="text-white mb-0">${dto.mensaje}</p>
            ${dto.clasesRestantes != null ? `<p class="text-secondary small mt-2">Clases restantes: ${dto.clasesRestantes}</p>` : ''}
        `;

        if ('speechSynthesis' in window && dto.permitido) {
            const mensaje = new SpeechSynthesisUtterance();
            mensaje.text = dto.socioNombre ? `Bienvenido ${dto.socioNombre}` : 'Bienvenido';
            mensaje.lang = 'es-AR';
            window.speechSynthesis.speak(mensaje);
        }
    }

    input.focus();
    // Mantener el foco en el input para que un lector-teclado siempre escriba ahí
    document.addEventListener('click', () => input.focus());
});
