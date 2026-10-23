/*
 * Plan M13 — SCORM player shell. Creates the SCORM runtime (scorm-again: window.API for SCORM 1.2, window.API_1484_11 for
 * SCORM 2004) with the attempt's data, then loads the package in the frame below; the package finds the runtime in its
 * parent window. Commits go to "commit" next to this page (the play link authorises them).
 */
(function () {
    'use strict';
    var config = JSON.parse(document.getElementById('scorm-config').textContent);
    var is2004 = config.version === 'SCORM 2004';
    var settings = {
        autocommit: true,
        autocommitSeconds: 15,
        lmsCommitUrl: 'commit',
        dataCommitFormat: 'json',
        terminateCommitParam: 'final',
        logLevel: 4
    };

    var api;
    if (is2004) {
        api = window.API_1484_11 = new window.Scorm2004API(settings);
    } else {
        api = window.API = new window.Scorm12API(settings);
    }
    try {
        api.loadFromJSON(JSON.parse(config.cmi || '{}'), '');
    } catch (e) {
        // A damaged saved state must not stop the package; it starts from its defaults.
    }

    function tellPortal(type) {
        if (window.parent && window.parent !== window) window.parent.postMessage({ type: type }, '*');
    }

    api.on(is2004 ? 'Terminate' : 'LMSFinish', function () { tellPortal('scorm-finished'); });
    api.on(is2004 ? 'Commit' : 'LMSCommit', function () { tellPortal('scorm-committed'); });

    // The portal page asks for a last commit before the learner leaves with its Exit button.
    window.addEventListener('message', function (event) {
        if (event.source !== window.parent || !event.data || event.data.type !== 'scorm-exit') return;
        try {
            if (is2004) api.Commit(''); else api.LMSCommit('');
        } catch (e) { /* not started yet: nothing to save */ }
        tellPortal('scorm-exit-ready');
    });

    // The runtime exists now; load the package once the frame is in the page.
    function start() { document.getElementById('scorm-content').src = config.launchUrl; }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start); else start();
})();
