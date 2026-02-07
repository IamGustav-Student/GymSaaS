const CACHE_NAME = 'gymvo-v1-static';
const DATA_CACHE_NAME = 'gymvo-v1-data';

const FILES_TO_CACHE = [
    '/',
    '/css/site.css',
    '/css/gymvo-cyberpunk.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/js/site.js',
    '/js/gymvo-scanner.js',
    '/favicon.ico',
    '/Accesos' // Cacheamos la página principal de la app
];

// INSTALACIÓN: Cachear assets estáticos (Shell)
self.addEventListener('install', (evt) => {
    evt.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            console.log('[ServiceWorker] Pre-caching offline page');
            return cache.addAll(FILES_TO_CACHE);
        })
    );
    self.skipWaiting();
});

// ACTIVACIÓN: Limpiar cachés viejos
self.addEventListener('activate', (evt) => {
    evt.waitUntil(
        caches.keys().then((keyList) => {
            return Promise.all(keyList.map((key) => {
                if (key !== CACHE_NAME && key !== DATA_CACHE_NAME) {
                    console.log('[ServiceWorker] Removing old cache', key);
                    return caches.delete(key);
                }
            }));
        })
    );
    self.clients.claim();
});

// FETCH: Estrategia Híbrida
self.addEventListener('fetch', (evt) => {
    // 1. Para APIs (/api/ o llamadas AJAX): Network First, luego Cache (o falla)
    if (evt.request.url.includes('/api/') || evt.request.headers.get('Accept').includes('application/json')) {
        evt.respondWith(
            fetch(evt.request)
                .then((response) => {
                    return response;
                })
                .catch(() => {
                    // Si falla la red, intentar devolver algo del caché o error offline
                    return new Response(JSON.stringify({ error: "Estás offline. No se pudo procesar." }), {
                        headers: { 'Content-Type': 'application/json' }
                    });
                })
        );
        return;
    }

    // 2. Para Archivos Estáticos (CSS, JS, HTML): Cache First, luego Network
    evt.respondWith(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.match(evt.request).then((response) => {
                return response || fetch(evt.request);
            });
        })
    );
});