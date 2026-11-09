// Plan M3.4 — grading workspace: move between students without leaving the page, view each attempt's
// files with annotations, autosave marks and feedback, score the rubric, moderate, and comment
// (text or a voice recording). Every user-supplied string is inserted as text.
(function () {
    'use strict';

    var root = document.getElementById('grading-workspace');
    if (!root) return;
    var config = JSON.parse(document.getElementById('workspace-config').textContent);
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
    var postJson = window.SubmissionViewer.postJson;
    var listEl = document.getElementById('ws-list');
    var panel = document.getElementById('ws-panel');
    var viewerHost = document.getElementById('ws-viewer');
    var items = config.submissions;
    var current = null;     // detail of the selected submission
    var viewer = null;
    var saveTimer = null;
    // Plan M9: latest similarity check per submission.
    var similarity = {};
    (config.similarity || []).forEach(function (s) { similarity[s.submissionId] = s; });

    function simClass(s) {
        if (s.status !== 'Completed') return 'bg-secondary';
        return s.score < 25 ? 'bg-success' : s.score < 50 ? 'bg-warning text-dark' : 'bg-danger';
    }
    function simText(s) {
        if (s.status === 'Completed') return (Math.round(s.score * 10) / 10) + '%';
        if (s.status === 'Queued' || s.status === 'Submitted') return 'checking…';
        return s.status === 'Skipped' ? 'no text' : 'failed';
    }

    function el(tag, cls, text) {
        var n = document.createElement(tag);
        if (cls) n.className = cls;
        if (text !== undefined && text !== null) n.textContent = text;
        return n;
    }

    function url(template, id) { return template.replace('__id__', encodeURIComponent(id)); }

    // Server times are UTC but arrive without a time zone ("2026-11-09T13:20:06"), which the browser would read as
    // local time (hand-in times showed hours off). A time without a zone is UTC.
    function parseServerTime(value) {
        if (typeof value === 'string' && /T\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/.test(value)) value += 'Z';
        return new Date(value);
    }

    function fmtDate(iso) {
        if (!iso) return '';
        var d = parseServerTime(iso);
        return isNaN(d) ? '' : d.toLocaleString();
    }

    function fmtMarks(v) { return v === null || v === undefined ? '—' : (Math.round(v * 100) / 100).toString(); }

    // ── Student list ──────────────────────────────────────────────────────

    function renderList() {
        listEl.textContent = '';
        if (items.length === 0) {
            listEl.appendChild(el('div', 'p-3 text-muted small', t('No submissions match this filter.')));
            return;
        }
        items.forEach(function (it, index) {
            var btn = el('button', 'list-group-item list-group-item-action d-flex justify-content-between align-items-start gap-2');
            btn.type = 'button';
            btn.setAttribute('data-submission', it.submissionId);
            if (current && current.submissionId === it.submissionId) { btn.classList.add('active'); btn.setAttribute('aria-current', 'true'); }
            var left = el('div', 'text-start');
            left.appendChild(el('div', 'fw-semibold', it.studentLabel));
            var meta = el('div', 'small');
            meta.textContent = 'Attempt ' + it.attemptNumber + ' · ' + fmtDate(it.submittedAt);
            left.appendChild(meta);
            btn.appendChild(left);
            var right = el('div', 'text-end small');
            right.appendChild(el('div', '', fmtMarks(it.marksAwarded) + ' / ' + config.maxMarks));
            var badges = el('div', 'd-flex gap-1 justify-content-end flex-wrap');
            if (it.needsGrading) badges.appendChild(el('span', 'badge bg-warning text-dark', t('To grade')));
            if (it.isLate) badges.appendChild(el('span', 'badge bg-danger', 'Late'));
            // Plan M8: automatic missing grade and late deduction.
            if (it.isMissing) badges.appendChild(el('span', 'badge bg-dark', 'Missing'));
            if (it.latePenalty > 0) badges.appendChild(el('span', 'badge bg-light text-danger border', '−' + fmtMarks(it.latePenalty) + ' late'));
            var sim = similarity[it.submissionId];
            if (sim) {
                var simBadge = el('span', 'badge ' + simClass(sim), 'Similarity ' + simText(sim));
                simBadge.setAttribute('data-similarity', sim.status);
                badges.appendChild(simBadge);
            }
            if (it.marksAwarded !== null && it.marksAwarded !== undefined) badges.appendChild(el('span', 'badge ' + (it.isPosted ? 'bg-success' : 'bg-secondary'), it.isPosted ? 'Posted' : 'Hidden'));
            if (it.provisionalGradeCount) badges.appendChild(el('span', 'badge bg-info text-dark', it.provisionalGradeCount + ' provisional'));
            right.appendChild(badges);
            btn.appendChild(right);
            btn.addEventListener('click', function () { open(it.submissionId); });
            listEl.appendChild(btn);
        });
        document.getElementById('ws-position').textContent = current
            ? (items.findIndex(function (x) { return x.submissionId === current.submissionId; }) + 1) + ' of ' + items.length
            : items.length + ' submission(s)';
    }

    function move(delta) {
        if (!current || items.length === 0) return;
        var i = items.findIndex(function (x) { return x.submissionId === current.submissionId; });
        var next = items[Math.max(0, Math.min(items.length - 1, i + delta))];
        if (next && next.submissionId !== current.submissionId) open(next.submissionId);
    }

    // ── Detail panel ──────────────────────────────────────────────────────

    function open(submissionId) {
        flushSave();
        panel.setAttribute('aria-busy', 'true');
        fetch(url(config.urls.detail, submissionId), { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : r.json().then(function (e) { throw new Error(e.message || t('Could not load the submission.')); }); })
            .then(function (detail) {
                current = detail;
                var u = new URL(window.location.href);
                u.searchParams.set('submissionId', submissionId);
                history.replaceState(null, '', u.toString());
                renderPanel();
                renderList();
                showActive();
            })
            .catch(function (e) { panel.textContent = e.message; })
            .then(function () { panel.removeAttribute('aria-busy'); });
    }

    // Scrolls only the list to the selected student. (scrollIntoView would also move the browser's Tab starting point
    // into the list, so a keyboard user's first Tab after the page loads would skip the skip link, header and menu.)
    function showActive() {
        var active = listEl.querySelector('.active');
        if (!active) return;
        var box = listEl.getBoundingClientRect(), r = active.getBoundingClientRect();
        if (r.top < box.top) listEl.scrollTop -= box.top - r.top;
        else if (r.bottom > box.bottom) listEl.scrollTop += r.bottom - box.bottom;
    }

    function renderPanel() {
        var d = current;
        panel.textContent = '';

        var head = el('div', 'd-flex flex-wrap gap-2 align-items-center mb-2');
        var title = el('div', 'flex-grow-1');
        title.appendChild(el('h2', 'h5 mb-0', d.studentLabel));
        title.appendChild(el('div', 'small text-muted', (d.registrationNumber ? d.registrationNumber + ' · ' : '') + 'Submitted ' + fmtDate(d.submittedAt)));
        head.appendChild(title);
        if (d.isLate) head.appendChild(el('span', 'badge bg-danger', 'Late'));
        if (d.isMissing) head.appendChild(el('span', 'badge bg-dark', t('Missing — automatic grade')));
        if (d.groupName) head.appendChild(el('span', 'badge bg-info text-dark', 'Group: ' + d.groupName));
        head.appendChild(el('span', 'badge ' + (d.isPosted ? 'bg-success' : 'bg-secondary'), d.isPosted ? t('Grade posted') : (d.marksAwarded !== null && d.marksAwarded !== undefined ? t('Grade hidden from student') : t('Not graded'))));
        panel.appendChild(head);
        renderSimilarity(d);

        renderAttempts(d);
        renderGrade(d);
        if (d.rubric) renderRubric(d);
        if (d.provisionalGrades) renderProvisional(d);
        renderComments(d);
    }

    function renderAttempts(d) {
        var box = el('div', 'mb-3');
        var row = el('div', 'd-flex gap-2 align-items-center mb-2');
        row.appendChild(el('label', 'small fw-semibold', 'Attempt'));
        var select = el('select', 'form-select form-select-sm flex-grow-1');
        select.style.minWidth = '0';
        select.setAttribute('aria-label', 'Attempt');
        d.attempts.forEach(function (a) {
            var opt = el('option', '', '#' + a.attemptNumber + ' · ' + fmtDate(a.submittedAt) + (a.isLate ? ' (late)' : ''));
            opt.value = a.attemptNumber;
            select.appendChild(opt);
        });
        row.appendChild(select);
        box.appendChild(row);
        var filesBox = el('div', 'd-flex flex-wrap gap-1 mb-2');
        var textBox = el('div', 'small p-2 bg-light rounded mb-2');
        textBox.style.whiteSpace = 'pre-wrap';
        box.appendChild(filesBox);
        box.appendChild(textBox);
        panel.appendChild(box);

        function show(number) {
            var attempt = d.attempts.find(function (a) { return a.attemptNumber === Number(number); }) || d.attempts[0];
            filesBox.textContent = '';
            textBox.textContent = attempt && attempt.textContent ? attempt.textContent : '';
            textBox.hidden = !(attempt && attempt.textContent);
            if (!attempt || attempt.files.length === 0) {
                filesBox.appendChild(el('span', 'small text-muted', t('No files in this attempt.')));
                showFile(null);
                return;
            }
            attempt.files.forEach(function (f, i) {
                var b = el('button', 'btn btn-sm btn-outline-primary', f.fileName + (f.annotationCount ? ' (' + f.annotationCount + ')' : ''));
                b.type = 'button';
                b.addEventListener('click', function () {
                    filesBox.querySelectorAll('button').forEach(function (x) { x.classList.remove('active'); });
                    b.classList.add('active');
                    showFile(f);
                });
                filesBox.appendChild(b);
                if (i === 0) { b.classList.add('active'); showFile(f); }
            });
        }
        select.addEventListener('change', function () { show(select.value); });
        show(d.attempts.length ? d.attempts[0].attemptNumber : null);
    }

    function showFile(f) {
        if (viewer) { viewer.dispose(); viewer = null; }
        viewerHost.textContent = '';
        if (!f) { viewerHost.appendChild(el('div', 'p-4 text-muted', t('Nothing to show.'))); return; }
        if (f.isLegacy) {
            var p = el('div', 'p-4');
            p.appendChild(el('div', 'mb-2', t('This file was uploaded before the grading workspace existed.')));
            var a = el('a', 'btn btn-sm btn-outline-secondary', 'Open ' + f.fileName);
            a.href = f.legacyUrl; a.target = '_blank'; a.rel = 'noopener';
            p.appendChild(a);
            viewerHost.appendChild(p);
            return;
        }
        var kind = f.viewerKind;
        var message = null;
        if (kind === 'download') {
            message = f.conversionStatus === 'Pending' || f.conversionStatus === 'Converting'
                ? (config.conversionEnabled ? t('Preparing a preview… refresh in a moment, or download the file.') : t('Previews of office files need the document conversion service. Download the file to read it.'))
                : f.conversionStatus === 'Failed' ? t('The preview could not be made (') + (f.conversionError || 'conversion failed') + ').' : null;
        }
        var src = url(config.urls.file, f.fileId) + (f.fileName.toLowerCase().endsWith('.pdf') || kind !== 'pdf' ? '' : '&pdf=true');
        viewer = new window.SubmissionViewer(viewerHost, {
            kind: kind, src: src, fileName: f.fileName, fileId: f.fileId, message: message,
            downloadUrl: url(config.urls.file, f.fileId),
            annotatedUrl: kind === 'download' ? null : url(config.urls.annotated, f.fileId),
            editable: true, token: token, workerSrc: config.urls.worker,
            urls: {
                list: url(config.urls.annotations, f.fileId),
                add: url(config.urls.addAnnotation, f.fileId),
                update: function (id) { return url(config.urls.updateAnnotation, id); },
                remove: function (id) { return url(config.urls.deleteAnnotation, id); }
            }
        });
        if (f.conversionStatus === 'Failed') {
            var retry = el('button', 'btn btn-sm btn-outline-secondary mt-2', t('Try the preview again'));
            retry.type = 'button';
            retry.addEventListener('click', function () {
                postJson(url(config.urls.reconvert, f.fileId), token).then(function () { retry.textContent = t('Queued — reopen this student shortly.'); retry.disabled = true; })
                    .catch(function (e) { retry.textContent = e.message; });
            });
            viewerHost.appendChild(retry);
        }
    }

    // ── Grade ─────────────────────────────────────────────────────────────

    var gradeStatus = null;

    function renderSimilarity(d) {
        var s = similarity[d.submissionId];
        if (!s) return;
        var row = el('div', 'd-flex flex-wrap align-items-center gap-2 small mb-2');
        row.id = 'ws-similarity';
        row.appendChild(el('span', '', 'Similarity:'));
        row.appendChild(el('span', 'badge ' + simClass(s), simText(s)));
        if (s.status === 'Completed' || s.status === 'Failed' || s.status === 'Skipped') {
            var link = el('a', '', t('Open report'));
            link.href = url(config.urls.similarityReport, s.checkId);
            row.appendChild(link);
        }
        var again = el('button', 'btn btn-link btn-sm p-0', t('Check again'));
        again.type = 'button';
        again.addEventListener('click', function () {
            again.disabled = true;
            postJson(url(config.urls.similarityRecheck, d.submissionId), token)
                .then(function (r) {
                    similarity[d.submissionId] = r;
                    renderList();
                    renderPanel();
                })
                .catch(function (e) { again.textContent = e.message; });
        });
        row.appendChild(again);
        panel.appendChild(row);
    }

    function renderGrade(d) {
        var card = el('div', 'border rounded p-2 mb-3');
        var moderated = config.moderated && !d.canModerate;
        card.appendChild(el('div', 'small fw-semibold mb-1', moderated ? t('Your provisional grade') : 'Grade'));
        var row = el('div', 'd-flex gap-2 align-items-center mb-2');
        var marks = el('input', 'form-control form-control-sm');
        marks.type = 'number'; marks.min = '0'; marks.max = String(d.maxMarks); marks.step = '0.25';
        marks.style.maxWidth = '7rem';
        marks.id = 'ws-marks';
        marks.setAttribute('aria-label', t('Marks out of ') + d.maxMarks);
        // A grader who is not the moderator only ever receives their own provisional grade.
        var mine = moderated && d.provisionalGrades && d.provisionalGrades.length ? d.provisionalGrades[0] : null;
        // Plan M8: the box holds the mark as entered; the late deduction is shown (and applied) separately.
        var startMarks = moderated ? (mine ? mine.marks : null) : (d.rawMarks !== null && d.rawMarks !== undefined ? d.rawMarks : d.marksAwarded);
        if (startMarks !== null && startMarks !== undefined) marks.value = startMarks;
        row.appendChild(marks);
        row.appendChild(el('span', 'small', '/ ' + d.maxMarks));
        gradeStatus = el('span', 'small text-muted ms-auto');
        gradeStatus.setAttribute('aria-live', 'polite');
        row.appendChild(gradeStatus);
        card.appendChild(row);
        if (!moderated) renderPolicyNotes(d, card, marks, function () { schedule(); });
        var feedback = el('textarea', 'form-control form-control-sm');
        feedback.rows = 3; feedback.maxLength = 2000; feedback.id = 'ws-feedback';
        feedback.placeholder = t('Feedback for the student');
        feedback.setAttribute('aria-label', 'Feedback');
        feedback.value = (moderated ? (mine && mine.feedback) : d.feedback) || '';
        card.appendChild(feedback);
        card.appendChild(el('div', 'form-text', t('Saved automatically. Ctrl+Enter saves now · Alt+→ / Alt+← next / previous student.')));
        panel.appendChild(card);

        function schedule() {
            gradeStatus.textContent = t('Unsaved…');
            clearTimeout(saveTimer);
            saveTimer = setTimeout(function () { save(marks, feedback); }, 900);
        }
        marks.addEventListener('input', schedule);
        feedback.addEventListener('input', schedule);
        feedback.addEventListener('keydown', function (e) { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); clearTimeout(saveTimer); save(marks, feedback); } });
        flushSave = function () { if (saveTimer) { clearTimeout(saveTimer); saveTimer = null; save(marks, feedback); } };
    }

    var flushSave = function () { };

    // Plan M8: late deduction (with waiver), group grading and the peer-review average.
    var penaltyLine = null;

    function renderPolicyNotes(d, card, marks, changed) {
        penaltyLine = el('div', 'small mb-2');
        penaltyLine.id = 'ws-penalty';
        card.appendChild(penaltyLine);
        showPenalty(d.latePenalty, d.marksAwarded);
        if (d.isLate && d.rawMarks !== null && d.rawMarks !== undefined && (d.latePenalty > 0 || d.latePenaltyWaived)) {
            var wrap = el('div', 'form-check mb-2');
            var box = el('input', 'form-check-input');
            box.type = 'checkbox'; box.id = 'ws-waive'; box.checked = !!d.latePenaltyWaived;
            var label = el('label', 'form-check-label small', t('Waive the late deduction'));
            label.htmlFor = 'ws-waive';
            wrap.appendChild(box); wrap.appendChild(label);
            card.appendChild(wrap);
            box.addEventListener('change', function () {
                box.disabled = true;
                postJson(url(config.urls.latePenalty, d.submissionId), token, { waived: box.checked })
                    .then(function () { open(d.submissionId); })
                    .catch(function (e) { gradeStatus.textContent = e.message; box.disabled = false; });
            });
        }
        if (d.groupId) {
            card.appendChild(el('div', 'small text-muted mb-2', t('Group work (') + (d.groupName || 'group') + '): unless members are graded separately, this grade is given to every member.'));
        }
        if (d.peerReviewsCompleted > 0) {
            var peer = el('div', 'small mb-2');
            peer.id = 'ws-peer';
            peer.appendChild(el('div', '', t('Peer reviews: ') + (d.peerReviewAverage !== null && d.peerReviewAverage !== undefined
                ? 'average ' + fmtMarks(d.peerReviewAverage) + ' from ' : '') + d.peerReviewsCompleted + ' review(s)'));
            if (d.peerReviewAverage !== null && d.peerReviewAverage !== undefined) {
                var use = el('button', 'btn btn-sm btn-outline-secondary mt-1', t('Use peer average'));
                use.type = 'button';
                use.addEventListener('click', function () { marks.value = d.peerReviewAverage; changed(); });
                peer.appendChild(use);
            }
            card.appendChild(peer);
        }
    }

    function showPenalty(penalty, finalMarks) {
        if (!penaltyLine) return;
        penaltyLine.textContent = penalty > 0
            ? t('Late deduction −') + fmtMarks(penalty) + ' → final grade ' + fmtMarks(finalMarks) + ' / ' + config.maxMarks
            : '';
        penaltyLine.className = penalty > 0 ? 'small text-danger mb-2' : 'small mb-2';
    }

    function save(marks, feedback) {
        saveTimer = null;
        if (marks.value === '') { gradeStatus.textContent = t('Enter marks to save.'); return; }
        var value = Number(marks.value);
        var id = current.submissionId;
        gradeStatus.textContent = t('Saving…');
        postJson(url(config.urls.grade, id), token, { marksAwarded: value, feedback: feedback.value })
            .then(function (r) {
                gradeStatus.textContent = r.provisional ? t('Saved as your provisional grade') : r.isPosted ? t('Saved · visible to the student') : t('Saved · hidden until posted');
                var it = items.find(function (x) { return x.submissionId === id; });
                if (it && !r.provisional) { it.marksAwarded = r.marksAwarded; it.isPosted = r.isPosted; it.needsGrading = false; it.latePenalty = r.latePenalty || 0; it.isMissing = false; }
                if (!r.provisional) showPenalty(r.latePenalty || 0, r.marksAwarded);
                if (it && !r.provisional && r.groupMembersGraded > 0 && it.groupId) {
                    items.forEach(function (x) { if (x.groupId === it.groupId && x !== it) { x.needsGrading = false; x.isPosted = r.isPosted; x.marksAwarded = r.marksAwarded; } });
                    gradeStatus.textContent += ' · also given to ' + r.groupMembersGraded + ' group member(s)';
                }
                if (it && r.provisional) it.provisionalGradeCount = Math.max(1, it.provisionalGradeCount || 0);
                renderList();
            })
            .catch(function (e) { gradeStatus.textContent = e.message; });
    }

    // ── Rubric ────────────────────────────────────────────────────────────

    function renderRubric(d) {
        var card = el('div', 'border rounded p-2 mb-3');
        card.appendChild(el('div', 'small fw-semibold mb-1', t('Rubric · ') + d.rubric.title));
        var total = el('div', 'small mb-1');
        var chosen = {};
        d.rubric.criteria.forEach(function (c) { if (c.selectedLevelId) chosen[c.criterionId] = c.selectedLevelId; });
        function updateTotal() {
            var sum = 0;
            d.rubric.criteria.forEach(function (c) {
                var level = c.levels.find(function (l) { return l.levelId === chosen[c.criterionId]; });
                if (level) sum += level.points;
            });
            total.textContent = 'Total ' + sum + ' / ' + d.rubric.maxPoints;
        }
        d.rubric.criteria.forEach(function (c) {
            var fs = el('fieldset', 'mb-2');
            fs.appendChild(el('legend', 'small mb-1 fs-6', c.name + ' (' + c.maxPoints + ')'));
            var group = el('div', 'btn-group btn-group-sm flex-wrap');
            group.setAttribute('role', 'radiogroup');
            c.levels.forEach(function (l) {
                var b = el('button', 'btn btn-outline-primary' + (chosen[c.criterionId] === l.levelId ? ' active' : ''), l.label + ' · ' + l.points);
                b.type = 'button';
                b.setAttribute('role', 'radio');
                b.setAttribute('aria-checked', chosen[c.criterionId] === l.levelId ? 'true' : 'false');
                b.addEventListener('click', function () {
                    chosen[c.criterionId] = l.levelId;
                    group.querySelectorAll('button').forEach(function (x) { x.classList.remove('active'); x.setAttribute('aria-checked', 'false'); });
                    b.classList.add('active'); b.setAttribute('aria-checked', 'true');
                    updateTotal();
                });
                group.appendChild(b);
            });
            fs.appendChild(group);
            card.appendChild(fs);
        });
        updateTotal();
        card.appendChild(total);
        var apply = el('div', 'form-check');
        var applyBox = el('input', 'form-check-input'); applyBox.type = 'checkbox'; applyBox.id = 'ws-rubric-apply'; applyBox.checked = true;
        var applyLabel = el('label', 'form-check-label small', t('Set the marks from the rubric total (scaled to ') + d.maxMarks + ')'); applyLabel.htmlFor = 'ws-rubric-apply';
        apply.appendChild(applyBox); apply.appendChild(applyLabel);
        card.appendChild(apply);
        var saveBtn = el('button', 'btn btn-sm btn-primary mt-1', t('Save rubric'));
        saveBtn.type = 'button';
        var status = el('span', 'small text-muted ms-2');
        saveBtn.addEventListener('click', function () {
            var grades = Object.keys(chosen).map(function (k) { return { criterionId: k, levelId: chosen[k] }; });
            if (grades.length === 0) { status.textContent = t('Choose a level first.'); return; }
            status.textContent = t('Saving…');
            postJson(url(config.urls.rubric, current.submissionId), token, { grades: grades, applyTotalToMarks: applyBox.checked })
                .then(function (detail) { current = detail; renderPanelKeepViewer(); })
                .catch(function (e) { status.textContent = e.message; });
        });
        card.appendChild(saveBtn);
        card.appendChild(status);
        panel.appendChild(card);
    }

    function renderPanelKeepViewer() {
        // Re-render the right-hand panel and the list; the viewer is rebuilt for the first file.
        renderPanel();
        var it = items.find(function (x) { return x.submissionId === current.submissionId; });
        if (it) { it.marksAwarded = current.marksAwarded; it.isPosted = current.isPosted; it.needsGrading = current.status === 'Submitted'; }
        renderList();
    }

    // ── Moderation ────────────────────────────────────────────────────────

    function renderProvisional(d) {
        if (!d.canModerate && (!d.provisionalGrades || d.provisionalGrades.length === 0)) return;
        var card = el('div', 'border rounded p-2 mb-3');
        card.appendChild(el('div', 'small fw-semibold mb-1', d.canModerate ? t('Provisional grades (you choose the final grade)') : t('Provisional grades')));
        if (d.provisionalGrades.length === 0) card.appendChild(el('div', 'small text-muted', t('No provisional grades yet.')));
        d.provisionalGrades.forEach(function (p) {
            var row = el('div', 'd-flex gap-2 align-items-start border-bottom py-1 small');
            var text = el('div', 'flex-grow-1');
            text.appendChild(el('div', 'fw-semibold', p.graderName + ': ' + fmtMarks(p.marks) + ' / ' + d.maxMarks + (p.isSelected ? ' ✓ chosen' : '')));
            if (p.feedback) { var fb = el('div', '', p.feedback); fb.style.whiteSpace = 'pre-wrap'; text.appendChild(fb); }
            row.appendChild(text);
            if (d.canModerate) {
                var use = el('button', 'btn btn-sm btn-outline-primary', t('Use this'));
                use.type = 'button';
                use.addEventListener('click', function () {
                    postJson(url(config.urls.moderate, d.submissionId), token, { provisionalGradeId: p.provisionalGradeId })
                        .then(function (detail) { current = detail; renderPanelKeepViewer(); })
                        .catch(function (e) { alert(e.message); });
                });
                row.appendChild(use);
            }
            card.appendChild(row);
        });
        if (d.canModerate) card.appendChild(el('div', 'form-text', t('Or type your own final marks in the Grade box above.')));
        panel.appendChild(card);
    }

    // ── Comments ──────────────────────────────────────────────────────────

    function renderComments(d) {
        var card = el('div', 'border rounded p-2 mb-3');
        card.appendChild(el('div', 'small fw-semibold mb-1', 'Comments'));
        var list = el('div', 'mb-2');
        d.comments.forEach(function (c) { list.appendChild(commentRow(c)); });
        if (d.comments.length === 0) list.appendChild(el('div', 'small text-muted', t('No comments yet.')));
        card.appendChild(list);

        var form = el('form', '');
        var body = el('textarea', 'form-control form-control-sm mb-1');
        body.rows = 2; body.maxLength = 4000; body.placeholder = t('Write a comment'); body.setAttribute('aria-label', 'Comment');
        form.appendChild(body);
        var controls = el('div', 'd-flex flex-wrap gap-2 align-items-center');
        var privWrap = el('div', 'form-check mb-0');
        var priv = el('input', 'form-check-input'); priv.type = 'checkbox'; priv.id = 'ws-comment-private';
        var privLabel = el('label', 'form-check-label small', t('Graders only')); privLabel.htmlFor = 'ws-comment-private';
        privWrap.appendChild(priv); privWrap.appendChild(privLabel);
        controls.appendChild(privWrap);
        var recorder = window.CommentRecorder ? window.CommentRecorder.create(controls) : null;
        var send = el('button', 'btn btn-sm btn-primary ms-auto', t('Add comment')); send.type = 'submit';
        controls.appendChild(send);
        form.appendChild(controls);
        var status = el('div', 'small text-muted');
        form.appendChild(status);
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var fd = new FormData();
            fd.append('body', body.value);
            fd.append('isPrivate', priv.checked ? 'true' : 'false');
            fd.append('attemptNumber', String(d.currentAttemptNumber));
            var media = recorder && recorder.blob();
            if (media) fd.append('media', media.blob, media.name);
            if (!body.value.trim() && !media) { status.textContent = t('Write something or record a message.'); return; }
            send.disabled = true;
            fetch(url(config.urls.comment, d.submissionId), { method: 'POST', credentials: 'same-origin', headers: { 'RequestVerificationToken': token }, body: fd })
                .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || t('The comment was not saved.')); return j; }); })
                .then(function (c) {
                    if (list.firstChild && list.firstChild.classList && list.firstChild.classList.contains('text-muted')) list.textContent = '';
                    list.appendChild(commentRow(c));
                    body.value = ''; priv.checked = false; status.textContent = '';
                    if (recorder) recorder.reset();
                })
                .catch(function (err) { status.textContent = err.message; })
                .then(function () { send.disabled = false; });
        });
        card.appendChild(form);
        panel.appendChild(card);
    }

    function commentRow(c) {
        var row = el('div', 'border-bottom py-1 small');
        var head = el('div', 'd-flex gap-2 align-items-center');
        head.appendChild(el('span', 'fw-semibold', c.authorName));
        if (c.isPrivateToGraders) head.appendChild(el('span', 'badge bg-dark', t('Graders only')));
        if (c.attemptNumber) head.appendChild(el('span', 'text-muted', 'attempt ' + c.attemptNumber));
        head.appendChild(el('span', 'text-muted ms-auto', fmtDate(c.createdAt)));
        if (c.isMine) {
            var del = el('button', 'btn btn-link btn-sm p-0 text-danger', 'Delete');
            del.type = 'button';
            del.addEventListener('click', function () {
                postJson(url(config.urls.deleteComment, c.commentId), token).then(function () { row.remove(); }).catch(function (e) { alert(e.message); });
            });
            head.appendChild(del);
        }
        row.appendChild(head);
        if (c.body) { var b = el('div', '', c.body); b.style.whiteSpace = 'pre-wrap'; row.appendChild(b); }
        if (c.hasMedia) {
            var media = document.createElement((c.mediaContentType || '').indexOf('video') === 0 ? 'video' : 'audio');
            media.controls = true; media.preload = 'none';
            media.src = url(config.urls.media, c.commentId);
            media.className = 'w-100 mt-1';
            row.appendChild(media);
        }
        return row;
    }

    // ── Keyboard ──────────────────────────────────────────────────────────

    document.addEventListener('keydown', function (e) {
        var typing = /^(INPUT|TEXTAREA|SELECT)$/.test((e.target && e.target.tagName) || '');
        if (e.altKey && e.key === 'ArrowRight') { e.preventDefault(); move(1); }
        else if (e.altKey && e.key === 'ArrowLeft') { e.preventDefault(); move(-1); }
        else if (!typing && (e.key === 'j' || e.key === 'J')) move(1);
        else if (!typing && (e.key === 'k' || e.key === 'K')) move(-1);
    });
    document.getElementById('ws-prev').addEventListener('click', function () { move(-1); });
    document.getElementById('ws-next').addEventListener('click', function () { move(1); });
    window.addEventListener('beforeunload', function () { flushSave(); });

    renderList();
    if (config.selected) open(config.selected);
    else panel.appendChild(el('div', 'text-muted', t('No submissions yet.')));
})();
