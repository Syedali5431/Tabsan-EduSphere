/*
 * Plan M13 — the portal page around the SCORM player: shows "progress saved" when the package finishes, and asks the
 * player for a last commit before the Exit button leaves the page.
 * Packages in parts have a contents list with Previous / Next. Without sequencing the page opens the part the learner
 * picks (SCORM 1.2 prerequisites can keep parts locked). With SCORM 2004 sequencing the package decides: Next, Previous,
 * a part from the list and the SCO's own navigation requests are sent to the server, which says what to open.
 */
(function () {
    'use strict';
    var page = document.querySelector('[data-scorm-page]');
    if (!page) return;
    var frame = page.querySelector('.scorm-frame');
    var finished = page.querySelector('[data-scorm-finished]');
    var exit = page.querySelector('[data-scorm-exit]');
    var leaving = null;

    var partsBox = page.querySelector('[data-scorm-parts]');
    var playerUrl = partsBox ? partsBox.getAttribute('data-player-url') : null;
    var sequenced = !!partsBox && partsBox.getAttribute('data-sequenced') === 'true';
    var note = partsBox ? partsBox.querySelector('[data-scorm-nav-message]') : null;
    var pending = null;   // what to do once the open part has saved
    var busy = false;
    var statusText = { passed: t('Passed'), failed: t('Failed'), completed: t('Completed'), incomplete: t('In progress') };
    var statusClass = { passed: 'bg-success', completed: 'bg-success', failed: 'bg-danger', incomplete: 'bg-warning text-dark' };

    function items() { return partsBox ? Array.prototype.slice.call(partsBox.querySelectorAll('[data-part]')) : []; }

    function find(id) {
        return partsBox.querySelector('[data-part="' + (window.CSS && CSS.escape ? CSS.escape(id) : id) + '"]');
    }

    function say(text, value) {
        if (!note) return;
        note.textContent = text ? t(text).replace('{0}', value || '') : '';
        note.classList.toggle('d-none', !text);
    }

    function mark(id) {
        items().forEach(function (li) {
            var on = li.getAttribute('data-part') === id;
            li.classList.toggle('active', on);
            if (on) li.setAttribute('aria-current', 'step'); else li.removeAttribute('aria-current');
        });
    }

    function load(id) {
        if (id) mark(id);
        if (finished) finished.classList.add('d-none');
        frame.src = playerUrl + (id ? '?part=' + encodeURIComponent(id) : '');
    }

    // Statuses, scores and (sequenced or with prerequisites) which parts may be opened now.
    function apply(parts) {
        (parts || []).forEach(function (p) {
            if (p.isHeading) return;
            var li = find(p.id);
            if (!li) return;
            var badge = li.querySelector('[data-part-status]');
            if (badge) {
                badge.className = 'badge rounded-pill ' + (statusClass[p.status] || 'bg-secondary');
                badge.textContent = (statusText[p.status] || t('Not started')) + (p.scorePercent != null ? ' · ' + Math.round(p.scorePercent * 10) / 10 + '%' : '');
            }
            var button = li.querySelector('[data-part-open]');
            var available = p.available !== false;
            li.setAttribute('data-available', available ? 'true' : 'false');
            if (button) {
                button.disabled = !available;
                button.title = available || !p.reason ? '' : t(p.reason).replace('{0}', p.reasonArg || '');
                button.textContent = (available ? '' : '🔒 ') + p.title;
            }
            if (p.isCurrent) mark(p.id);
        });
    }

    function refresh() {
        if (!playerUrl) return;
        fetch(playerUrl + 'parts', { credentials: 'omit', cache: 'no-store' }).then(function (r) { return r.ok ? r.json() : []; }).then(apply)
            .catch(function () { /* the list keeps its last state */ });
    }

    // Save the part that is open, then carry on (Next/Previous/choice), or carry on at once if the player does not answer.
    function afterSave(action) {
        if (!frame) return;
        pending = action;
        frame.contentWindow.postMessage({ type: 'scorm-exit' }, '*');
        setTimeout(function () { if (pending === action) { pending = null; action(); } }, 1500);
    }

    // Sequenced packages: the server's sequencing says what to open.
    function navigate(request, target) {
        if (busy) return;
        busy = true;
        say('');
        fetch(playerUrl + 'navigate', {
            method: 'POST', credentials: 'omit', cache: 'no-store',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ request: request, target: target || null })
        }).then(function (r) { return r.ok ? r.json() : Promise.reject(r.status); }).then(function (result) {
            apply(result.parts);
            if (result.partId) load(result.partId);
            else if (result.ended) {
                say(result.message, result.messageArg);
                if (finished) finished.classList.remove('d-none');
                load(null);
            } else if (result.message) say(result.message, result.messageArg);
        }).catch(function () {
            say('The package could not be reached. Try again.');
        }).then(function () { busy = false; });
    }

    function openPart(id) {
        var li = find(id);
        if (li && li.getAttribute('data-available') === 'false') {
            var b = li.querySelector('[data-part-open]');
            say(b && b.title ? b.title : 'Finish the parts it depends on first.');
            return;
        }
        say('');
        afterSave(function () { refresh(); load(id); });
    }

    function step(delta) {
        if (sequenced) { afterSave(function () { navigate(delta > 0 ? 'continue' : 'previous'); }); return; }
        var list = items();
        var i = 0;
        for (var k = 0; k < list.length; k++) if (list[k].classList.contains('active')) i = k;
        var j = i + delta;
        if (j < 0 || j >= list.length) return;
        openPart(list[j].getAttribute('data-part'));
    }

    if (partsBox) {
        // Opening a page-only part counts it as done: refresh once each part has loaded.
        if (frame) frame.addEventListener('load', refresh);
        partsBox.addEventListener('click', function (e) {
            var button = e.target.closest('[data-part-open]');
            if (!button || button.disabled) return;
            var id = button.getAttribute('data-part-open');
            if (sequenced) afterSave(function () { navigate('choice', id); }); else openPart(id);
        });
        var prev = partsBox.querySelector('[data-scorm-prev]');
        var next = partsBox.querySelector('[data-scorm-next]');
        if (prev) prev.addEventListener('click', function () { step(-1); });
        if (next) next.addEventListener('click', function () { step(1); });
    }

    window.addEventListener('message', function (event) {
        if (!frame || event.source !== frame.contentWindow || !event.data) return;
        var type = event.data.type;
        if (type === 'scorm-exit-ready' && pending) {
            var action = pending;
            pending = null;
            action();
            return;
        }
        if (type === 'scorm-exit-ready' && leaving) {
            var target = leaving;
            leaving = null;
            window.location.href = target;
            return;
        }
        if (pending) return; // the open part is being replaced: its last messages do not count
        if (type === 'scorm-finished' && finished) finished.classList.remove('d-none');
        if (type === 'scorm-committed' || type === 'scorm-finished') refresh();
        // A SCO asked to move on (or to leave) when it ended — only the part that is open now (not one being replaced).
        var active = partsBox ? partsBox.querySelector('[data-part].active') : null;
        var openId = active ? active.getAttribute('data-part') : null;
        if (type === 'scorm-nav' && sequenced && event.data.request && (!event.data.part || !openId || event.data.part === openId))
            navigate(event.data.request, event.data.target);
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
