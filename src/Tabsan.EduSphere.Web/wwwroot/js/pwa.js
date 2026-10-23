// Plan M4 — installable app, offline banner, sign-out cache wipe and "notifications on this device".
(function () {
    'use strict';

    var USER_CACHES = ['edusphere-pages-v1', 'edusphere-files-v1'];

    function clearUserData() {
        var jobs = [];
        if (window.caches) USER_CACHES.forEach(function (c) { jobs.push(caches.delete(c)); });
        if (navigator.serviceWorker && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({ type: 'clear-user-data' });
        }
        return Promise.all(jobs).catch(function () { /* nothing to clear */ });
    }
    window.EduSpherePwa = { clearUserData: clearUserData };

    // ── Service worker ────────────────────────────────────────────────────
    if ('serviceWorker' in navigator && window.isSecureContext) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { scope: '/', updateViaCache: 'none' }).catch(function () { /* the portal works without it */ });
        });
    }

    // ── Install button ────────────────────────────────────────────────────
    var deferredPrompt = null;
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        document.querySelectorAll('[data-pwa-install]').forEach(function (b) { b.hidden = false; });
    });
    document.addEventListener('click', function (e) {
        var button = e.target.closest && e.target.closest('[data-pwa-install]');
        if (!button || !deferredPrompt) return;
        deferredPrompt.prompt();
        deferredPrompt.userChoice.finally(function () {
            deferredPrompt = null;
            document.querySelectorAll('[data-pwa-install]').forEach(function (b) { b.hidden = true; });
        });
    });
    window.addEventListener('appinstalled', function () {
        document.querySelectorAll('[data-pwa-install]').forEach(function (b) { b.hidden = true; });
    });

    // ── Offline banner ────────────────────────────────────────────────────
    var banner = null;
    function updateOnline() {
        if (navigator.onLine) { if (banner) { banner.remove(); banner = null; } return; }
        if (banner) return;
        banner = document.createElement('div');
        banner.className = 'position-fixed bottom-0 start-50 translate-middle-x mb-3 px-3 py-2 rounded shadow bg-dark text-white small';
        banner.style.zIndex = 2000;
        banner.setAttribute('role', 'status');
        banner.textContent = t('You are offline — you are looking at a saved copy. Changes cannot be sent until you reconnect.');
        document.body.appendChild(banner);
    }
    window.addEventListener('online', updateOnline);
    window.addEventListener('offline', updateOnline);
    document.addEventListener('DOMContentLoaded', updateOnline);

    // ── Sign-out wipes offline copies (shared devices) ────────────────────
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !/\/logout(\?|$)/i.test(form.getAttribute('action') || '') || form.dataset.pwaCleared === 'true') return;
        e.preventDefault();
        form.dataset.pwaCleared = 'true';
        var unsubscribe = Promise.resolve();
        // This device stops receiving the signed-out user's notifications.
        if (navigator.serviceWorker && window.PushManager) {
            unsubscribe = navigator.serviceWorker.getRegistration().then(function (reg) {
                return reg && reg.pushManager.getSubscription();
            }).then(function (sub) {
                if (!sub) return;
                var token = (form.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
                var url = document.body.getAttribute('data-push-unsubscribe-url');
                var serverCall = url ? fetch(url + '?endpoint=' + encodeURIComponent(sub.endpoint), { method: 'POST', credentials: 'same-origin', headers: { 'RequestVerificationToken': token } }) : Promise.resolve();
                return serverCall.catch(function () { }).then(function () { return sub.unsubscribe(); });
            }).catch(function () { });
        }
        Promise.all([clearUserData(), unsubscribe]).then(function () { form.submit(); });
    }, true);

    // ── Notifications on this device (Notifications page) ─────────────────
    document.addEventListener('DOMContentLoaded', function () {
        var card = document.getElementById('push-device-card');
        if (!card) return;
        var status = card.querySelector('[data-push-status]');
        var enable = card.querySelector('[data-push-enable]');
        var disable = card.querySelector('[data-push-disable]');
        var test = card.querySelector('[data-push-test]');
        var prefsBox = card.querySelector('[data-push-prefs]');
        var devicesBox = card.querySelector('[data-push-devices]');
        var token = (card.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
        var urls = {
            summary: card.getAttribute('data-summary-url'), subscribe: card.getAttribute('data-subscribe-url'),
            unsubscribe: card.getAttribute('data-unsubscribe-url'), prefs: card.getAttribute('data-prefs-url'), test: card.getAttribute('data-test-url')
        };
        var summary = null;

        function say(text) { status.textContent = text; }
        function post(url, body) {
            return fetch(url, {
                method: 'POST', credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: body === undefined ? '{}' : JSON.stringify(body)
            }).then(function (r) {
                return r.text().then(function (body) {
                    var j = null; try { j = body ? JSON.parse(body) : null; } catch (e) { j = null; }
                    if (!r.ok) throw new Error((j && j.message) || t('The request failed (') + r.status + ').');
                    return j;
                });
            });
        }
        function keyBytes(base64url) {
            var s = base64url.replace(/-/g, '+').replace(/_/g, '/');
            while (s.length % 4) s += '=';
            var raw = atob(s), out = new Uint8Array(raw.length);
            for (var i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i);
            return out;
        }
        function deviceLabel() {
            var ua = navigator.userAgent;
            var browser = /Edg\//.test(ua) ? 'Edge' : /Firefox\//.test(ua) ? 'Firefox' : /Chrome\//.test(ua) ? 'Chrome' : /Safari\//.test(ua) ? 'Safari' : 'Browser';
            var os = /Android/.test(ua) ? 'Android' : /iPhone|iPad/.test(ua) ? 'iOS' : /Windows/.test(ua) ? 'Windows' : /Mac OS X/.test(ua) ? 'macOS' : /Linux/.test(ua) ? 'Linux' : 'device';
            return browser + ' on ' + os;
        }
        function sendSubscription(sub) {
            var json = sub.toJSON();
            return post(urls.subscribe, { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth, deviceLabel: deviceLabel() });
        }
        function render(subscribed) {
            enable.hidden = subscribed;
            disable.hidden = !subscribed;
            test.hidden = !subscribed;
            prefsBox.hidden = false;
            prefsBox.textContent = '';
            (summary.preferences || []).forEach(function (p) {
                var wrap = document.createElement('div');
                wrap.className = 'form-check form-check-inline';
                var box = document.createElement('input');
                box.type = 'checkbox'; box.className = 'form-check-input'; box.id = 'push-pref-' + p.type; box.checked = p.pushEnabled;
                var label = document.createElement('label');
                label.className = 'form-check-label small'; label.htmlFor = box.id; label.textContent = p.label;
                box.addEventListener('change', function () {
                    p.pushEnabled = box.checked;
                    post(urls.prefs, { preferences: summary.preferences }).then(function () { say(t('Saved.')); }).catch(function (e) { say(e.message); });
                });
                wrap.appendChild(box); wrap.appendChild(label);
                prefsBox.appendChild(wrap);
            });
            devicesBox.textContent = '';
            (summary.devices || []).forEach(function (d) {
                var li = document.createElement('li');
                li.textContent = (d.deviceLabel || d.endpointHost) + (d.isActive ? '' : ' (stopped)') + ' — added ' + new Date(d.createdAt).toLocaleDateString();
                devicesBox.appendChild(li);
            });
        }

        if (!('serviceWorker' in navigator) || !('PushManager' in window) || !window.isSecureContext) {
            say(/iPhone|iPad/.test(navigator.userAgent)
                ? t('On iPhone and iPad, first add EduSphere to your Home Screen (Share › Add to Home Screen), then open it from there to turn on notifications.')
                : t('This browser cannot show notifications from EduSphere.'));
            return;
        }

        fetch(urls.summary, { credentials: 'same-origin' }).then(function (r) { return r.json(); }).then(function (s) {
            summary = s;
            if (!s.enabled || !s.publicKey) { say(t('Notifications on devices are not enabled on this server.')); return null; }
            return navigator.serviceWorker.ready.then(function (reg) { return reg.pushManager.getSubscription(); });
        }).then(function (sub) {
            if (!summary || !summary.enabled) return;
            if (sub) {
                // Keep the server's copy current (another user may have used this browser before).
                return sendSubscription(sub).then(function () { say(t('This device receives EduSphere notifications.')); render(true); });
            }
            say(Notification.permission === 'denied' ? t('Notifications are blocked for this site in your browser settings.') : t('This device does not receive notifications yet.'));
            render(false);
        }).catch(function (e) { say(e.message || t('Could not load your notification settings.')); });

        enable.addEventListener('click', function () {
            Notification.requestPermission().then(function (permission) {
                if (permission !== 'granted') { say(t('Permission was not given, so notifications stay off.')); return; }
                return navigator.serviceWorker.ready.then(function (reg) {
                    return reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: keyBytes(summary.publicKey) });
                }).then(sendSubscription).then(function () {
                    say(t('Done — this device will show EduSphere notifications.'));
                    return fetch(urls.summary, { credentials: 'same-origin' }).then(function (r) { return r.json(); }).then(function (s) { summary = s; render(true); });
                });
            }).catch(function (e) { say(e.message || t('Notifications could not be turned on.')); });
        });

        disable.addEventListener('click', function () {
            navigator.serviceWorker.ready.then(function (reg) { return reg.pushManager.getSubscription(); }).then(function (sub) {
                if (!sub) return;
                return fetch(urls.unsubscribe + '?endpoint=' + encodeURIComponent(sub.endpoint), { method: 'POST', credentials: 'same-origin', headers: { 'RequestVerificationToken': token } })
                    .then(function () { return sub.unsubscribe(); });
            }).then(function () { say(t('This device no longer receives notifications.')); render(false); })
              .catch(function (e) { say(e.message); });
        });

        test.addEventListener('click', function () {
            post(urls.test).then(function (r) { say(r && r.delivered ? t('Test sent — it should appear in a moment.') : t('No device accepted the test message.')); })
                .catch(function (e) { say(e.message); });
        });
    });
})();
