/**
 * GYMVO SERVICE WORKER v3
 * Estrategia: Stale-While-Revalidate para navegación/assets locales.
 * Las peticiones que no son GET (POST/PUT/DELETE) nunca pasan por el
 * cache — la Cache API solo soporta GET, y cachear una mutación no
 * tiene sentido de todas formas.
 */

const CACHE_NAME = 'gymvo-cache-v3';

// Assets propios: si alguno falla, el resto igual se precachea (no bloquea el install).
const LOCAL_ASSETS = [
    '/',
    '/css/site.css',
    '/css/gymvo-cyberpunk.css',
    '/css/gymvo-pwa-native.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/js/site.js',
    '/js/gymvo-db.js',
    '/js/gymvo-pwa-manager.js',
    '/js/gymvo-scanner.js',
    '/js/gymvo-pwa-native.js',
    '/favicon.ico',
    '/img/icons/icon-192.png',
    '/img/icons/icon-512.png'
];

// CDNs externos: mejor esfuerzo. Si el CDN está caído en el momento del
// install, no debe tumbar el Service Worker entero.
const EXTERNAL_ASSETS = [
    'https://unpkg.com/dexie/dist/dexie.js',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css'
];

async function precacheResiliente(cache, urls) {
    const resultados = await Promise.allSettled(urls.map((url) => cache.add(url)));
    resultados.forEach((r, i) => {
        if (r.status === 'rejected') {
            console.warn('[SW] No se pudo precachear', urls[i], r.reason);
        }
    });
}

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(async (cache) => {
            await precacheResiliente(cache, LOCAL_ASSETS);
            await precacheResiliente(cache, EXTERNAL_ASSETS);
        })
    );
    self.skipWaiting();
});

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

self.addEventListener('fetch', (event) => {
    const request = event.request;
    const url = new URL(request.url);

    // Solo GET es cacheable — dejamos pasar todo lo demás directo a la red.
    if (request.method !== 'GET') {
        return;
    }

    // No cachear SignalR ni autenticación.
    if (url.pathname.includes('/accesoHub') || url.pathname.includes('/Auth/')) {
        return;
    }

    event.respondWith(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.match(request).then((cachedResponse) => {
                const fetchedResponse = fetch(request).then((networkResponse) => {
                    if (networkResponse && networkResponse.status === 200) {
                        cache.put(request, networkResponse.clone());
                    }
                    return networkResponse;
                }).catch(() => {
                    // Sin red y sin caché de esta URL puntual: si es una navegación
                    // de página completa, al menos servimos el shell cacheado.
                    if (request.mode === 'navigate') {
                        return cache.match('/');
                    }
                    return cachedResponse;
                });

                return cachedResponse || fetchedResponse;
            });
        })
    );
});

// Background Sync: el SW no tiene acceso directo a IndexedDB en todos los
// navegadores, así que delega el flush real a gymvo-pwa-manager.js del cliente.
self.addEventListener('sync', (event) => {
    if (event.tag === 'sync-asistencias') {
        event.waitUntil(notificarClientesParaSincronizar());
    }
});

async function notificarClientesParaSincronizar() {
    const allClients = await clients.matchAll();
    allClients.forEach(client => {
        client.postMessage({ type: 'SYNC_NOW' });
    });
}
