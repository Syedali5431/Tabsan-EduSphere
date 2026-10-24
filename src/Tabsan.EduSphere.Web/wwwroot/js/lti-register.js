// Plan M5.1 — LTI Dynamic Registration: when the tool's registration page (in the frame) posts
// { subject: "org.imsglobal.lti.close" }, go back to the tool list. Messages from other origins are ignored.
(function () {
    'use strict';
    var page = document.querySelector('[data-lti-registration]');
    if (!page) return;
    var toolOrigin = page.getAttribute('data-tool-origin');
    var doneUrl = page.getAttribute('data-done-url');
    window.addEventListener('message', function (event) {
        if (event.origin !== toolOrigin) return;
        var data = event.data;
        if (typeof data === 'string') {
            try { data = JSON.parse(data); } catch (e) { return; }
        }
        if (data && data.subject === 'org.imsglobal.lti.close') window.location.href = doneUrl;
    });
})();
