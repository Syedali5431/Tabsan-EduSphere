// Plan M3 — student submission page: preview submitted files (with the teacher's annotations once the
// grade is posted — the API decides), and add or delete comments (text or a voice recording).
(function () {
    'use strict';

    var page = document.getElementById('submission-page');
    if (!page) return;
    var token = (page.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
    var viewerHost = document.getElementById('student-viewer');
    var viewer = null;

    function url(attr, id) { return page.getAttribute(attr).replace('__id__', encodeURIComponent(id)); }

    page.querySelectorAll('[data-preview]').forEach(function (button) {
        button.addEventListener('click', function () {
            page.querySelectorAll('[data-preview]').forEach(function (b) { b.classList.remove('active'); });
            button.classList.add('active');
            if (viewer) viewer.dispose();
            var id = button.getAttribute('data-file-id');
            var kind = button.getAttribute('data-kind');
            var hasNotes = Number(button.getAttribute('data-annotations')) > 0;
            viewer = new window.SubmissionViewer(viewerHost, {
                kind: kind,
                src: url('data-file-url', id) + (button.getAttribute('data-pdf') === 'true' ? '&pdf=true' : ''),
                downloadUrl: url('data-file-url', id),
                annotatedUrl: hasNotes && kind !== 'download' ? url('data-annotated-url', id) : null,
                fileName: button.getAttribute('data-name'),
                fileId: id,
                editable: false,
                workerSrc: page.getAttribute('data-worker-url'),
                message: kind === 'download' ? t('A preview is not available for this file. Use Download to open it.') : null,
                urls: { list: url('data-annotations-url', id) }
            });
            viewerHost.scrollIntoView({ block: 'nearest' });
        });
    });

    // Comments
    var form = document.getElementById('student-comment-form');
    if (!form) return;
    var controls = document.getElementById('student-comment-controls');
    var recorder = window.CommentRecorder ? window.CommentRecorder.create(controls) : null;
    controls.appendChild(controls.querySelector('button[type="submit"]')); // keep "Add comment" last
    var status = document.getElementById('student-comment-status');
    var body = document.getElementById('student-comment-body');

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var media = recorder && recorder.blob();
        if (!body.value.trim() && !media) { status.textContent = t('Write something or record a message.'); return; }
        var fd = new FormData();
        fd.append('body', body.value);
        fd.append('isPrivate', 'false');
        if (media) fd.append('media', media.blob, media.name);
        status.textContent = t('Sending…');
        fetch(page.getAttribute('data-comment-url'), { method: 'POST', credentials: 'same-origin', headers: { 'RequestVerificationToken': token }, body: fd })
            .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || t('Your comment was not saved.')); return j; }); })
            .then(function () { window.location.reload(); })
            .catch(function (err) { status.textContent = err.message; });
    });

    page.querySelectorAll('[data-delete-comment]').forEach(function (button) {
        button.addEventListener('click', function () {
            if (!confirm(t('Delete this comment?'))) return;
            window.SubmissionViewer.postJson(url('data-delete-comment-url', button.getAttribute('data-delete-comment')), token)
                .then(function () { var row = button.closest('[data-comment]'); if (row) row.remove(); })
                .catch(function (err) { alert(err.message); });
        });
    });
})();
