// Plan M2 — the student's quiz paper: answer collection, autosave, server-anchored countdown,
// one-question-at-a-time paging, ordering controls and file upload.
// The server is the authority on the deadline and on locked questions; this script only mirrors them.
(function () {
    'use strict';

    var paper = document.getElementById('quiz-paper');
    if (!paper) return;

    var token = (paper.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
    var saveUrl = paper.dataset.saveUrl;
    var uploadUrl = paper.dataset.uploadUrl;
    var reviewUrl = paper.dataset.reviewUrl;
    var oneAtATime = paper.dataset.oneAtATime === 'true';
    var lockAnswered = paper.dataset.lockAnswered === 'true';
    var sections = Array.prototype.slice.call(paper.querySelectorAll('[data-question]'));
    var statusEl = paper.querySelector('[data-save-status]');
    var submitForm = paper.querySelector('[data-submit-form]');
    var dirty = {};
    var saving = false;
    var submitting = false;

    function setStatus(text, isError) {
        if (!statusEl) return;
        statusEl.textContent = text;
        statusEl.classList.toggle('text-danger', !!isError);
        statusEl.classList.toggle('text-muted', !isError);
    }

    // ── Collect one question's answer in the API's SubmitAnswerEntry shape ───────────
    function collect(section) {
        var qid = section.dataset.qid;
        var type = section.dataset.type;
        var entry = { quizQuestionId: qid, selectedOptionId: null, textResponse: null, selectedOptionIds: null, responseJson: null };
        switch (type) {
            case 'MultipleChoice':
            case 'TrueFalse': {
                var picked = section.querySelector('[data-option]:checked');
                entry.selectedOptionId = picked ? picked.value : null;
                entry.selectedOptionIds = picked ? [picked.value] : [];
                break;
            }
            case 'MultipleAnswers':
                entry.selectedOptionIds = Array.prototype.map.call(section.querySelectorAll('[data-option]:checked'), function (i) { return i.value; });
                break;
            case 'Matching': {
                var matches = {};
                section.querySelectorAll('[data-match-id]').forEach(function (s) { if (s.value) matches[s.dataset.matchId] = s.value; });
                entry.responseJson = JSON.stringify({ matches: matches });
                break;
            }
            case 'Ordering':
                entry.responseJson = JSON.stringify({ order: Array.prototype.map.call(section.querySelectorAll('[data-item-id]'), function (li) { return li.dataset.itemId; }) });
                break;
            case 'FillInBlank': {
                var blanks = {};
                section.querySelectorAll('[data-blank-id]').forEach(function (i) { blanks[i.dataset.blankId] = i.value; });
                entry.responseJson = JSON.stringify({ blanks: blanks });
                break;
            }
            case 'Numeric':
            case 'Formula': {
                var n = section.querySelector('[data-numeric]');
                entry.responseJson = JSON.stringify({ value: n ? n.value.trim() : '' });
                break;
            }
            case 'ShortAnswer':
            case 'Essay': {
                var textBox = section.querySelector('[data-text]');
                entry.textResponse = textBox ? textBox.value : '';
                break;
            }
            case 'FileUpload':
                return null; // uploaded separately
        }
        return entry;
    }

    function editable(section) { return section.dataset.locked !== 'true'; }

    function collectAll() {
        return sections.filter(editable).map(collect).filter(Boolean);
    }

    function lockSection(section) {
        section.dataset.locked = 'true';
        section.querySelectorAll('input, select, textarea, button').forEach(function (el) { el.disabled = true; });
    }

    // ── Autosave ───────────────────────────────────────────────────────────────────
    function post(url, body, headers) {
        headers = headers || {};
        headers.RequestVerificationToken = token;
        return fetch(url, { method: 'POST', credentials: 'same-origin', headers: headers, body: body })
            .then(function (r) { return r.ok ? r.json() : { ok: false, message: t('The server could not save (') + r.status + ').' }; });
    }

    function handleClosed(result) {
        // Time ran out or the attempt was closed elsewhere — the server has the final copy.
        if (result && (result.error === 'ATTEMPT_CLOSED' || result.error === 'TIME_EXPIRED')) {
            submitting = true;
            if (result.message) alert(result.message);
            window.location.href = reviewUrl;
            return true;
        }
        return false;
    }

    function save(onlyIds) {
        var ids = onlyIds || Object.keys(dirty);
        if (ids.length === 0 || saving) return Promise.resolve(true);
        var answers = sections.filter(function (s) { return ids.indexOf(s.dataset.qid) >= 0 && editable(s); }).map(collect).filter(Boolean);
        if (answers.length === 0) { ids.forEach(function (id) { delete dirty[id]; }); return Promise.resolve(true); }
        saving = true;
        setStatus(t('Saving…'));
        return post(saveUrl, JSON.stringify(answers), { 'Content-Type': 'application/json' })
            .then(function (result) {
                saving = false;
                if (handleClosed(result)) return false;
                if (!result.ok) { setStatus(result.message || t('Could not save — retrying.'), true); return false; }
                ids.forEach(function (id) { delete dirty[id]; });
                setStatus(Object.keys(dirty).length ? t('Unsaved changes') : t('All answers saved'));
                return true;
            })
            .catch(function () { saving = false; setStatus(t('Offline — answers will be saved when the connection returns.'), true); return false; });
    }

    var saveTimer = null;
    function markDirty(section) {
        if (!editable(section)) return;
        dirty[section.dataset.qid] = true;
        setStatus(t('Unsaved changes'));
        clearTimeout(saveTimer);
        saveTimer = setTimeout(function () { save(); }, 1500);
    }

    sections.forEach(function (section) {
        section.addEventListener('input', function () { markDirty(section); });
        section.addEventListener('change', function (e) {
            if (e.target.matches('[data-file]')) return;
            markDirty(section);
        });
    });
    setInterval(function () { if (Object.keys(dirty).length) save(); }, 30000);
    window.addEventListener('beforeunload', function (e) {
        if (!submitting && Object.keys(dirty).length) { e.preventDefault(); e.returnValue = ''; }
    });

    // ── Ordering controls ──────────────────────────────────────────────────────────
    paper.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-move]');
        if (!btn) return;
        var li = btn.closest('[data-item-id]');
        var dir = parseInt(btn.dataset.move, 10);
        if (dir < 0 && li.previousElementSibling) li.parentNode.insertBefore(li, li.previousElementSibling);
        if (dir > 0 && li.nextElementSibling) li.parentNode.insertBefore(li.nextElementSibling, li);
        btn.focus();
        markDirty(btn.closest('[data-question]'));
    });

    // ── Essay word count ───────────────────────────────────────────────────────────
    paper.querySelectorAll('textarea[data-text]').forEach(function (ta) {
        var out = ta.parentNode.querySelector('[data-word-count]');
        if (!out) return;
        var limit = parseInt(ta.dataset.wordLimit, 10);
        function update() {
            var words = ta.value.trim() ? ta.value.trim().split(/\s+/).length : 0;
            out.textContent = words + ' word' + (words === 1 ? '' : 's') + (limit ? ' of ' + limit : '');
            out.classList.toggle('text-danger', !!limit && words > limit);
        }
        ta.addEventListener('input', update);
        update();
    });

    // ── File upload ────────────────────────────────────────────────────────────────
    paper.querySelectorAll('[data-upload]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var section = btn.closest('[data-question]');
            var input = section.querySelector('[data-file]');
            var label = section.querySelector('[data-file-name]');
            if (!input.files || input.files.length === 0) { label.textContent = t('Choose a file first.'); return; }
            var fd = new FormData();
            fd.append('questionId', section.dataset.qid);
            fd.append('file', input.files[0]);
            btn.disabled = true;
            label.textContent = t('Uploading…');
            post(uploadUrl, fd).then(function (result) {
                btn.disabled = false;
                if (handleClosed(result)) return;
                label.textContent = result.ok ? 'Uploaded: ' + result.fileName : (result.message || t('Upload failed.'));
                label.classList.toggle('text-danger', !result.ok);
                if (result.ok) input.value = '';
            }).catch(function () { btn.disabled = false; label.textContent = t('Upload failed — check your connection.'); });
        });
    });

    // ── One question at a time ─────────────────────────────────────────────────────
    var pager = paper.querySelector('[data-pager]');
    var current = 0;
    function show(index) {
        current = Math.max(0, Math.min(index, sections.length - 1));
        sections.forEach(function (s, i) { s.hidden = i !== current; });
        pager.querySelector('[data-page-label]').textContent = 'Question ' + (current + 1) + ' of ' + sections.length;
        pager.querySelector('[data-prev]').hidden = lockAnswered || current === 0;
        pager.querySelector('[data-next]').hidden = current === sections.length - 1;
        submitForm.hidden = current !== sections.length - 1;
        var focusable = sections[current].querySelector('input:not([disabled]), select:not([disabled]), textarea:not([disabled])');
        if (focusable) focusable.focus({ preventScroll: true });
        sections[current].scrollIntoView({ block: 'nearest' });
    }
    if (oneAtATime && sections.length > 1) {
        pager.hidden = false;
        // Resume at the first question that is not locked.
        var firstOpen = sections.findIndex(editable);
        show(firstOpen < 0 ? sections.length - 1 : firstOpen);
        pager.querySelector('[data-prev]').addEventListener('click', function () { save().then(function () { show(current - 1); }); });
        pager.querySelector('[data-next]').addEventListener('click', function () {
            var section = sections[current];
            if (lockAnswered && editable(section)) {
                if (!confirm(t('Your answer to this question will be final. Continue?'))) return;
                dirty[section.dataset.qid] = true;
            }
            save([section.dataset.qid]).then(function (ok) {
                if (!ok && editable(section) && Object.keys(dirty).indexOf(section.dataset.qid) >= 0) return;
                if (lockAnswered) lockSection(section);
                show(current + 1);
            });
        });
    }

    // ── Submit ─────────────────────────────────────────────────────────────────────
    function submit(auto) {
        if (submitting) return;
        submitting = true;
        submitForm.querySelector('input[name="answersJson"]').value = JSON.stringify(collectAll());
        if (auto) setStatus(t('Time is up — submitting your answers…'));
        submitForm.submit();
    }
    submitForm.addEventListener('submit', function (e) {
        e.preventDefault();
        var unanswered = sections.filter(function (s) {
            var a = collect(s);
            if (!a) return false;
            if (a.selectedOptionIds && a.selectedOptionIds.length) return false;
            if (a.textResponse && a.textResponse.trim()) return false;
            if (a.responseJson && /:\s*"[^"]+"|:\s*\[\s*"/.test(a.responseJson)) return false;
            return editable(s);
        }).length;
        var msg = unanswered ? t('You have ') + unanswered + ' unanswered question(s). Submit anyway?' : t('Submit your quiz? You cannot change answers afterwards.');
        if (confirm(msg)) submit(false);
    });

    // ── Countdown (anchored to the server clock) ───────────────────────────────────
    var timerEl = paper.querySelector('[data-timer]');
    if (timerEl && paper.dataset.deadline) {
        var skew = Date.parse(paper.dataset.serverNow) - Date.now();
        var deadline = Date.parse(paper.dataset.deadline);
        var tick = function () {
            var left = Math.max(0, deadline - (Date.now() + skew));
            var total = Math.floor(left / 1000);
            var h = Math.floor(total / 3600), m = Math.floor((total % 3600) / 60), s = total % 60;
            timerEl.textContent = (h ? h + ':' + String(m).padStart(2, '0') : m) + ':' + String(s).padStart(2, '0');
            timerEl.classList.toggle('bg-warning', total <= 300 && total > 60);
            timerEl.classList.toggle('bg-danger', total <= 60);
            timerEl.classList.toggle('text-white', total <= 60);
            if (total === 300 || total === 60) timerEl.setAttribute('aria-live', 'assertive');
            if (left <= 0) { clearInterval(handle); submit(true); }
        };
        var handle = setInterval(tick, 1000);
        tick();
    }
})();
