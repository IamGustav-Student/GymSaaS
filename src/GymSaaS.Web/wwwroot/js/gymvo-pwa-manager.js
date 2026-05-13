/**
 * GYMVO PWA MANAGER - Cerebro de la aplicación progresiva
 * Gestiona el estado de conexión, la sincronización y la inicialización de datos.
 */

const PWA_MANAGER = {
    init() {
        console.log("🚀 PWA Manager Iniciado");
        this.updateConnectionStatus();
        this.registerEventListeners();
        this.initialSync();
    },

    registerEventListeners() {
        window.addEventListener('online', () => this.updateConnectionStatus());
        window.addEventListener('offline', () => this.updateConnectionStatus());
    },

    updateConnectionStatus() {
        const isOnline = navigator.onLine;
        const badge = document.getElementById('pwa-connection-status');
        const text = badge.querySelector('.status-text');

        if (isOnline) {
            badge.classList.remove('offline');
            badge.classList.add('online');
            text.innerText = 'ONLINE';
            console.log("🌐 Conectado a la red");
            this.flushOfflineQueue(); // Intentar subir datos al reconectar
        } else {
            badge.classList.remove('online');
            badge.classList.add('offline');
            text.innerText = 'OFFLINE';
            console.warn("🔌 Modo Offline Activado");
        }
    },

    // Sincronización inicial: traer socios para poder validar offline
    async initialSync() {
        if (!navigator.onLine) return;

        try {
            console.log("📥 Sincronizando base de datos local de socios...");
            const response = await fetch('/Socios/GetSociosJson'); // Asumimos este endpoint existe o lo crearemos
            if (response.ok) {
                const socios = await response.json();
                await GymvoDB.saveSocios(socios);
                console.log(`✅ ${socios.length} socios sincronizados localmente.`);
            }
        } catch (error) {
            console.error("Error en sincronización inicial:", error);
        }
    },

    // Subir asistencias que se guardaron mientras estaba offline
    async flushOfflineQueue() {
        if (!navigator.onLine) return;

        const pendientes = await GymvoDB.getAsistenciasPendientes();
        if (pendientes.length === 0) return;

        console.log(`📤 Subiendo ${pendientes.length} asistencias pendientes...`);

        for (const item of pendientes) {
            try {
                const response = await fetch('/Accesos/RegistrarAsistenciaOffline', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(item)
                });

                if (response.ok) {
                    await GymvoDB.markAsSincronizada(item.id);
                }
            } catch (error) {
                console.error("Error sincronizando item:", item, error);
            }
        }

        await GymvoDB.clearSincronizadas();
        console.log("✨ Sincronización de cola completada.");
    }
};

// Auto-inicialización
document.addEventListener('DOMContentLoaded', () => PWA_MANAGER.init());
