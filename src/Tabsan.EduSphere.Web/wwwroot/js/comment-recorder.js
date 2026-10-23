// Plan M3.1 — media comments: record a short voice message in the browser (MediaRecorder),
// or attach an audio/video file. Used by the grading workspace and the student submission page.
(function () {
    'use strict';

    var MAX_SECONDS = 300;
    var ACCEPT = '.webm,.ogg,.mp3,.m4a,.wav,.mp4';

    function create(container) {
        var recorded = null;   // { blob, name }
        var chosen = null;
        var mediaRecorder = null, chunks = [], timer = null, seconds = 0;

        var wrap = document.createElement('div');
        wrap.className = 'd-flex flex-wrap gap-2 align-items-center';
        var record = document.createElement('button');
        record.type = 'button';
        record.className = 'btn btn-sm btn-outline-secondary';
        record.textContent = t('🎙 Record');
        var state = document.createElement('span');
        state.className = 'small text-muted';
        state.setAttribute('aria-live', 'polite');
        var file = document.createElement('input');
        file.type = 'file';
        file.accept = ACCEPT;
        file.className = 'form-control form-control-sm';
        file.style.maxWidth = '14rem';
        file.setAttribute('aria-label', t('Attach an audio or video file'));
        var preview = document.createElement('audio');
        preview.controls = true;
        preview.hidden = true;
        preview.style.maxWidth = '16rem';

        var canRecord = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder);
        if (canRecord) wrap.appendChild(record);
        wrap.appendChild(file);
        wrap.appendChild(preview);
        wrap.appendChild(state);
        container.appendChild(wrap);

        function stop() {
            if (mediaRecorder && mediaRecorder.state !== 'inactive') mediaRecorder.stop();
            clearInterval(timer);
        }

        record.addEventListener('click', function () {
            if (mediaRecorder && mediaRecorder.state === 'recording') { stop(); return; }
            navigator.mediaDevices.getUserMedia({ audio: true }).then(function (stream) {
                chunks = []; seconds = 0;
                mediaRecorder = new MediaRecorder(stream);
                mediaRecorder.ondataavailable = function (e) { if (e.data.size) chunks.push(e.data); };
                mediaRecorder.onstop = function () {
                    stream.getTracks().forEach(function (t) { t.stop(); });
                    var type = mediaRecorder.mimeType || 'audio/webm';
                    var blob = new Blob(chunks, { type: type });
                    recorded = { blob: blob, name: 'voice-comment.' + (type.indexOf('ogg') >= 0 ? 'ogg' : type.indexOf('mp4') >= 0 ? 'm4a' : 'webm') };
                    preview.src = URL.createObjectURL(blob);
                    preview.hidden = false;
                    record.textContent = t('🎙 Record again');
                    record.classList.remove('btn-danger');
                    state.textContent = 'Recorded ' + seconds + ' s.';
                };
                mediaRecorder.start();
                record.textContent = t('■ Stop');
                record.classList.add('btn-danger');
                state.textContent = t('Recording…');
                timer = setInterval(function () {
                    seconds++;
                    state.textContent = t('Recording… ') + seconds + ' s';
                    if (seconds >= MAX_SECONDS) stop();
                }, 1000);
            }).catch(function () { state.textContent = t('The microphone is not available. Attach a file instead.'); });
        });

        file.addEventListener('change', function () {
            chosen = file.files && file.files[0] ? { blob: file.files[0], name: file.files[0].name } : null;
        });

        return {
            blob: function () { return recorded || chosen; },
            reset: function () {
                stop();
                recorded = null; chosen = null; file.value = '';
                preview.hidden = true; preview.removeAttribute('src');
                state.textContent = '';
                record.textContent = t('🎙 Record');
            }
        };
    }

    window.CommentRecorder = { create: create };
})();
