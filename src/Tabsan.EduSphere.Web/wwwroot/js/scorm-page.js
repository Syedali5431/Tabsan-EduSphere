/*
 * Plan M13 — the portal page around the SCORM player: shows "progress saved" when the package finishes, and asks the
 * player for a last commit before the Exit button leaves the page.
 */
(function () {
    'use strict';
    var page = document.querySelector('[data-scorm-page]');
    if (!page) return;
    var frame = page.querySelector('.scorm-frame');
    var finished = page.querySelector('[data-scorm-finished]');
    var exit = page.querySelector('[data-scorm-exit]');
    var leaving = null;

    // Multi-part packages: the contents list, Previous / Next, and each part's status after it commits.
    var partsBox = page.querySelector('[data-scorm-parts]');
    var playerUrl = partsBox ? partsBox.getAttribute('data-player-url') : null;
    var items = partsBox ? Array.prototype.slice.call(partsBox.querySelectorAll('[data-part]')) : [];
    var switching = null;
    var statusText = { passed: t('Passed'), failed: t('Failed'), completed: t('Completed'), incomplete: t('In progress') };
    var statusClass = { passed: 'bg-success', completed: 'bg-success', failed: 'bg-danger', incomplete: 'bg-warning text-dark' };

    function currentIndex() {
        for (var i = 0; i < items.length; i++) if (items[i].classList.contains('active')) return i;
        return 0;
    }

    function show(id) {
        items.forEach(function (li) {
            var on = li.getAttribute('data-part') === id;
            li.classList.toggle('active', on);
            if (on) li.setAttribute('aria-current', 'step'); else li.removeAttribute('aria-current');
        });
        if (finished) finished.classList.add('d-none');
        frame.src = playerUrl + '?part=' + encodeURIComponent(id);
    }

    // Save the part that is open, then open the next one.
    function open(id) {
        if (!frame || !id) return;
        switching = id;
        frame.contentWindow.postMessage({ type: 'scorm-exit' }, '*');
        setTimeout(function () { if (switching) { var next = switching; switching = null; show(next); } }, 1500);
    }

    function refresh() {
        if (!playerUrl) return;
        fetch(playerUrl + 'parts', { credentials: 'omit', cache: 'no-store' }).then(function (r) { return r.ok ? r.json() : []; }).then(function (parts) {
            (parts || []).forEach(function (p) {
                var li = partsBox.querySelector('[data-part="' + (window.CSS && CSS.escape ? CSS.escape(p.id) : p.id) + '"]');
                var badge = li && li.querySelector('[data-part-status]');
                if (!badge) return;
                badge.className = 'badge rounded-pill ' + (statusClass[p.status] || 'bg-secondary');
                badge.textContent = (statusText[p.status] || t('Not started')) + (p.scorePercent != null ? ' · ' + Math.round(p.scorePercent * 10) / 10 + '%' : '');
            });
        }).catch(function () { /* the list keeps its last state */ });
    }

    if (partsBox) {
        // Opening a page-only part counts it as done: refresh once each part has loaded.
        if (frame) frame.addEventListener('load', refresh);
        partsBox.addEventListener('click', function (e) {
            var button = e.target.closest('[data-part-open]');
            if (button) open(button.getAttribute('data-part-open'));
        });
        var prev = partsBox.querySelector('[data-scorm-prev]');
        var next = partsBox.querySelector('[data-scorm-next]');
        if (prev) prev.addEventListener('click', function () { var i = currentIndex(); if (i > 0) open(items[i - 1].getAttribute('data-part')); });
        if (next) next.addEventListener('click', function () { var i = currentIndex(); if (i < items.length - 1) open(items[i + 1].getAttribute('data-part')); });
    }

    window.addEventListener('message', function (event) {
        if (!frame || event.source !== frame.contentWindow || !event.data) return;
        if (event.data.type === 'scorm-finished' && finished) finished.classList.remove('d-none');
        if (event.data.type === 'scorm-committed' || event.data.type === 'scorm-finished') refresh();
        if (event.data.type === 'scorm-exit-ready' && switching) {
            var id = switching;
            switching = null;
            refresh();
            show(id);
            return;
        }
        if (event.data.type === 'scorm-exit-ready' && leaving) {
            var target = leaving;
            leaving = null;
            window.location.href = target;
        }
    });

    // Load the player only now, so its first messages (a short package can finish at once) are not missed.
    if (frame && frame.dataset.src) frame.src = frame.dataset.src;

    if (exit && frame) {
        exit.addEventListener('click', function (e) {
            e.preventDefault();
            leaving = exit.getAttribute('href');
            frame.contentWindow.postMessage({ type: 'scorm-exit' }, '*');
            // Leave anyway if the player does not answer.
            setTimeout(function () { if (leaving) window.location.href = leaving; }, 1500);
        });
    }
})();
