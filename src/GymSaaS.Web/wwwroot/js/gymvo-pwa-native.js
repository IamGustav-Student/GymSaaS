/**
 * Gymvo PWA Native — utilidades compartidas para que la app del socio se
 * sienta "casi nativa": registro del Service Worker, banner de instalación
 * (Android/Chrome vía beforeinstallprompt + instrucciones manuales en iOS),
 * feedback háptico, ripple táctil, y pull-to-refresh.
 *
 * Se carga en todas las páginas del Portal. No depende de jQuery.
 */
(function () {
    'use strict';

    // ---------------------------------------------------------------
    // 1. Registro del Service Worker (único punto de registro)
    // ---------------------------------------------------------------
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js').catch(err => {
                console.warn('No se pudo registrar el Service Worker:', err);
            });
        });
    }

    // ---------------------------------------------------------------
    // 2. Feedback háptico (Vibration API — no todos los navegadores/OS
    //    lo soportan, especialmente iOS Safari; falla en silencio)
    // ---------------------------------------------------------------
    function vibrar(patron) {
        try {
            if (navigator.vibrate) navigator.vibrate(patron);
        } catch (e) { /* no-op */ }
    }
    window.GymvoHaptics = {
        tap: () => vibrar(10),
        exito: () => vibrar([15, 60, 15]),
        error: () => vibrar([40, 40, 40])
    };

    // ---------------------------------------------------------------
    // 3. Ripple táctil en cualquier elemento con la clase .tap-ripple
    // ---------------------------------------------------------------
    document.addEventListener('pointerdown', function (e) {
        const target = e.target.closest('.tap-ripple');
        if (!target) return;

        const rect = target.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const ripple = document.createElement('span');
        ripple.className = 'ripple-effect';
        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = (e.clientX - rect.left - size / 2) + 'px';
        ripple.style.top = (e.clientY - rect.top - size / 2) + 'px';

        target.appendChild(ripple);
        setTimeout(() => ripple.remove(), 500);
    });

    // ---------------------------------------------------------------
    // 4. Banner de instalación (Add to Home Screen)
    // ---------------------------------------------------------------
    let deferredPrompt = null;
    const YA_RESPONDIO_KEY = 'gymvo_install_prompt_dismissed';

    function yaFueDescartado() {
        try { return localStorage.getItem(YA_RESPONDIO_KEY) === '1'; }
        catch (e) { return false; }
    }
    function marcarDescartado() {
        try { localStorage.setItem(YA_RESPONDIO_KEY, '1'); }
        catch (e) { /* no-op */ }
    }

    function esStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches ||
               window.navigator.standalone === true;
    }

    function esIOS() {
        return /iphone|ipad|ipod/i.test(window.navigator.userAgent);
    }

    function mostrarBanner({ esIOSInstructivo }) {
        if (document.getElementById('gymvoInstallBanner')) return;

        const banner = document.createElement('div');
        banner.id = 'gymvoInstallBanner';
        banner.className = 'pwa-install-banner';

        const texto = esIOSInstructivo
            ? 'Instalá Gymvo: tocá <i class="bi bi-box-arrow-up"></i> y luego "Agregar a pantalla de inicio".'
            : 'Instalá Gymvo en tu celular para un acceso más rápido, incluso sin conexión.';

        banner.innerHTML = `
            <i class="bi bi-phone icon"></i>
            <div class="text">${texto}</div>
            ${esIOSInstructivo ? '' : '<button type="button" id="gymvoInstallBtn">Instalar</button>'}
            <button type="button" class="close-btn" id="gymvoInstallDismiss" aria-label="Cerrar">
                <i class="bi bi-x-lg"></i>
            </button>
        `;
        document.body.appendChild(banner);
        requestAnimationFrame(() => banner.classList.add('visible'));

        document.getElementById('gymvoInstallDismiss').addEventListener('click', () => {
            banner.classList.remove('visible');
            marcarDescartado();
            setTimeout(() => banner.remove(), 300);
        });

        const installBtn = document.getElementById('gymvoInstallBtn');
        if (installBtn) {
            installBtn.addEventListener('click', async () => {
                banner.classList.remove('visible');
                setTimeout(() => banner.remove(), 300);
                marcarDescartado();
                if (deferredPrompt) {
                    deferredPrompt.prompt();
                    await deferredPrompt.userChoice;
                    deferredPrompt = null;
                }
            });
        }
    }

    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        if (!esStandalone() && !yaFueDescartado()) {
            setTimeout(() => mostrarBanner({ esIOSInstructivo: false }), 1500);
        }
    });

    window.addEventListener('appinstalled', () => {
        marcarDescartado();
        const banner = document.getElementById('gymvoInstallBanner');
        if (banner) banner.remove();
    });

    // iOS Safari no dispara beforeinstallprompt: mostramos instrucciones manuales.
    document.addEventListener('DOMContentLoaded', () => {
        if (esIOS() && !esStandalone() && !yaFueDescartado()) {
            setTimeout(() => mostrarBanner({ esIOSInstructivo: true }), 1500);
        }
    });

    // ---------------------------------------------------------------
    // 5. Pull-to-refresh simple (solo cuando el scroll está en el tope)
    // ---------------------------------------------------------------
    function habilitarPullToRefresh(onRefresh) {
        let startY = 0;
        let pulling = false;
        const threshold = 70;

        let indicator = document.querySelector('.ptr-indicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'ptr-indicator';
            indicator.innerHTML = '<i class="bi bi-arrow-repeat"></i>';
            document.body.prepend(indicator);
        }

        document.addEventListener('touchstart', (e) => {
            if (window.scrollY === 0) {
                startY = e.touches[0].clientY;
                pulling = true;
            }
        }, { passive: true });

        document.addEventListener('touchmove', (e) => {
            if (!pulling) return;
            const delta = e.touches[0].clientY - startY;
            if (delta > 10 && delta < threshold * 2) {
                indicator.classList.add('visible');
            }
        }, { passive: true });

        document.addEventListener('touchend', (e) => {
            if (!pulling) return;
            pulling = false;
            const delta = (e.changedTouches[0]?.clientY ?? startY) - startY;
            if (delta > threshold) {
                indicator.classList.add('loading');
                GymvoHaptics.tap();
                Promise.resolve(onRefresh ? onRefresh() : null).finally(() => {
                    window.location.reload();
                });
            } else {
                indicator.classList.remove('visible');
            }
        });
    }
    window.GymvoPullToRefresh = habilitarPullToRefresh;
})();
