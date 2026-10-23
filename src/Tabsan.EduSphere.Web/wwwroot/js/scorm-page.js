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

    window.addEventListener('message', function (event) {
        if (!frame || event.source !== frame.contentWindow || !event.data) return;
        if (event.data.type === 'scorm-finished' && finished) finished.classList.remove('d-none');
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
