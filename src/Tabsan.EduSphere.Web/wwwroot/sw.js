/* Plan M4.2/M4.3 — EduSphere service worker.
 * - App shell (CSS/JS/icons) cached so the portal opens instantly and offline.
 * - Read-only offline copies of a few pages (timetable, announcements, course materials, notifications,
 *   dashboard) and of downloaded course-material files, refreshed every time they are opened online.
 *   They are wiped on sign-out and on the sign-in page, so the next person on a shared device sees nothing.
 * - Web Push: shows the notification and opens the linked portal page when it is clicked.
 * Nothing else is cached: forms, grades pages and every POST always go to the network.
 */
'use strict';

const VERSION = 'm4-1';
const SHELL_CACHE = 'edusphere-shell-' + VERSION;
const PAGE_CACHE = 'edusphere-pages-v1';
const FILE_CACHE = 'edusphere-files-v1';
const USER_CACHES = [PAGE_CACHE, FILE_CACHE];

const SHELL_URLS = [
    '/offline.html',
    '/css/site.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/js/site.js',
    '/js/pwa.js',
    '/images/pwa/icon-192.png',
    '/images/pwa/badge-96.png',
    '/images/tabsan-mark.png',
    '/favicon.ico'
];

// Pages kept for offline reading (matched on the path; the query string is kept in the cache key).
const OFFLINE_PAGES = [
    /^\/Portal\/Dashboard$/i,
    /^\/Portal\/Timetable(Student|Teacher)$/i,
    /^\/Portal\/Announcements$/i,
    /^\/Portal\/CourseMaterial(Student)?$/i,
    /^\/Portal\/Notifications$/i
];
const OFFLINE_FILES = [/^\/Portal\/DownloadCourseMaterialFile$/i];
const MAX_PAGES = 40;
const MAX_FILES = 30;
const MAX_FILE_BYTES = 25 * 1024 * 1024;

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(SHELL_CACHE)
            .then(function (cache) { return cache.addAll(SHELL_URLS.map(function (u) { return new Request(u, { cache: 'reload' }); })); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.filter(function (k) { return k.indexOf('edusphere-shell-') === 0 && k !== SHELL_CACHE; })
                .map(function (k) { return caches.delete(k); }));
        }).then(function () { return self.clients.claim(); })
    );
});

function matches(list, path) { return list.some(function (r) { return r.test(path); }); }

function isLoginResponse(response) {
    // A redirect to the sign-in page means the session ended — never keep that as the page.
    return response.redirected && /\/(login|home\/login)/i.test(new URL(response.url).pathname);
}

function trim(cacheName, max) {
    return caches.open(cacheName).then(function (cache) {
        return cache.keys().then(function (keys) {
            return Promise.all(keys.slice(0, Math.max(0, keys.length - max)).map(function (k) { return cache.delete(k); }));
        });
    });
}

function navigate(request, url) {
    var keep = matches(OFFLINE_PAGES, url.pathname);
    return fetch(request).then(function (response) {
        if (keep && response.ok && response.type === 'basic' && !isLoginResponse(response)) {
            var copy = response.clone();
            caches.open(PAGE_CACHE).then(function (cache) { return cache.put(request, copy); }).then(function () { return trim(PAGE_CACHE, MAX_PAGES); });
        }
        return response;
    }).catch(function () {
        return caches.open(PAGE_CACHE).then(function (cache) {
            return cache.match(request).then(function (hit) { return hit || (keep ? cache.match(request, { ignoreSearch: true }) : undefined); });
        }).then(function (hit) { return hit || caches.match('/offline.html'); });
    });
}

function staleWhileRevalidate(request) {
    return caches.open(SHELL_CACHE).then(function (cache) {
        return cache.match(request).then(function (cached) {
            var network = fetch(request).then(function (response) {
                if (response.ok && (response.type === 'basic' || response.type === 'cors' || response.type === 'opaque')) cache.put(request, response.clone());
                return response;
            }).catch(function () { return cached; });
            return cached || network;
        });
    });
}

function courseFile(request) {
    return fetch(request).then(function (response) {
        var length = Number(response.headers.get('Content-Length') || 0);
        if (response.ok && response.type === 'basic' && !isLoginResponse(response) && length <= MAX_FILE_BYTES) {
            var copy = response.clone();
            caches.open(FILE_CACHE).then(function (cache) { return cache.put(request, copy); }).then(function () { return trim(FILE_CACHE, MAX_FILES); });
        }
        return response;
    }).catch(function () {
        return caches.open(FILE_CACHE).then(function (cache) { return cache.match(request); })
            .then(function (hit) { return hit || caches.match('/offline.html'); });
    });
}

self.addEventListener('fetch', function (event) {
    var request = event.request;
    if (request.method !== 'GET') return;
    var url = new URL(request.url);

    if (url.origin !== self.location.origin) {
        if (url.hostname === 'fonts.googleapis.com' || url.hostname === 'fonts.gstatic.com') event.respondWith(staleWhileRevalidate(request));
        return;
    }
    if (url.pathname === '/sw.js') return;
    if (request.mode === 'navigate') { event.respondWith(navigate(request, url)); return; }
    if (/^\/(css|js|lib|images)\//i.test(url.pathname) || url.pathname === '/favicon.ico' || url.pathname === '/Tabsan.EduSphere.Web.styles.css') {
        event.respondWith(staleWhileRevalidate(request));
        return;
    }
    if (matches(OFFLINE_FILES, url.pathname)) event.respondWith(courseFile(request));
});

self.addEventListener('message', function (event) {
    if (event.data && event.data.type === 'clear-user-data') {
        event.waitUntil(Promise.all(USER_CACHES.map(function (c) { return caches.delete(c); })));
    }
});

// ── Web Push ────────────────────────────────────────────────────────────────

self.addEventListener('push', function (event) {
    var data = {};
    try { data = event.data ? event.data.json() : {}; } catch (e) { data = { body: event.data ? event.data.text() : '' }; }
    var url = typeof data.url === 'string' && data.url.charAt(0) === '/' && data.url.charAt(1) !== '/' ? data.url : '/Portal/Notifications';
    event.waitUntil(self.registration.showNotification(data.title || 'EduSphere', {
        body: data.body || '',
        icon: '/images/pwa/icon-192.png',
        badge: '/images/pwa/badge-96.png',
        tag: data.tag || 'edusphere',
        renotify: true,
        data: { url: url }
    }));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var target = new URL((event.notification.data && event.notification.data.url) || '/Portal/Notifications', self.location.origin).href;
    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (windows) {
        for (var i = 0; i < windows.length; i++) {
            if (windows[i].url === target && 'focus' in windows[i]) return windows[i].focus();
        }
        return self.clients.openWindow(target);
    }));
});
