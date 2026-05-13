/**
 * GYMVO SERVICE WORKER v2
 * Estrategia: Stale-While-Revalidate para UI + Cache First para Assets
 */

const CACHE_NAME = 'gymvo-cache-v2';
const STATIC_ASSETS = [
    '/',
    '/css/site.css',
    '/css/gymvo-cyberpunk.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/js/site.js',
    '/js/gymvo-db.js',
    '/js/gymvo-pwa-manager.js',
    '/js/gymvo-scanner.js',
    '/favicon.ico',
    'https://unpkg.com/dexie/dist/dexie.js',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css'
];

// Instalación: Cachear assets básicos
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(STATIC_ASSETS);
        })
    );
    self.skipWaiting();
});

// Activación: Limpieza de versiones viejas
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) => {
            return Promise.all(
                keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
            );
        })
    );
    self.clients.claim();
});

// Estrategia de Fetch
self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // No cachear llamadas SignalR ni de autenticación
    if (url.pathname.includes('/accesoHub') || url.pathname.includes('/Auth/')) {
        return;
    }

    // Estrategia: STALE-WHILE-REVALIDATE para navegación y archivos locales
    event.respondWith(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.match(event.request).then((cachedResponse) => {
                const fetchedResponse = fetch(event.request).then((networkResponse) => {
                    // Si la respuesta es válida, la guardamos en el caché
                    if (networkResponse && networkResponse.status === 200) {
                        cache.put(event.request, networkResponse.clone());
                    }
                    return networkResponse;
                }).catch(() => {
                    // Fallback offline para navegación HTML
                    if (event.request.mode === 'navigate') {
                        return cache.match('/');
                    }
                });

                return cachedResponse || fetchedResponse;
            });
        })
    );
});

// Background Sync (Sincronización en segundo plano)
self.addEventListener('sync', (event) => {
    if (event.tag === 'sync-asistencias') {
        console.log('[SW] Iniciando sincronización de fondo...');
        event.waitUntil(syncAsistencias());
    }
});

async function syncAsistencias() {
    // Nota: El SW no tiene acceso directo al DOM ni a GymvoDB directamente si no está en el mismo scope.
    // Usualmente se envía un mensaje al cliente para que el PWA_MANAGER haga el flush.
    const allClients = await clients.matchAll();
    allClients.forEach(client => {
        client.postMessage({ type: 'SYNC_NOW' });
    });
}