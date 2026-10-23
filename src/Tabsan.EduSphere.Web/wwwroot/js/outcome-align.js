// Plan M6 — "Outcomes" buttons on rubric criteria, quiz questions, bank questions and assignments.
// Markup: <button type="button" hidden data-outcome-align data-target-type="RubricCriterion" data-target-id="…">Outcomes</button>
// The buttons stay hidden unless the Learning Outcomes module answers for the first one.
(function () {
    'use strict';
    if (window.__eduOutcomeAlign) return;
    window.__eduOutcomeAlign = true;

    var endpoint = '/Portal/OutcomeAlignment';
    var dialog = null;
    var current = null;

    function token() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function load(type, id) {
        return fetch(endpoint + '?targetType=' + encodeURIComponent(type) + '&targetId=' + encodeURIComponent(id), {
            headers: { 'Accept': 'application/json' }, credentials: 'same-origin'
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (body) { return { ok: r.ok, status: r.status, body: body }; });
        });
    }

    function label(button, count) {
        var base = button.getAttribute('data-label') || 'Outcomes';
        button.textContent = count > 0 ? base + ' (' + count + ')' : base;
        button.setAttribute('aria-label', base + ': ' + count + ' aligned');
    }

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text != null) node.textContent = text;
        return node;
    }

    function ensureDialog() {
        if (dialog) return dialog;
        var wrap = el('div', { 'class': 'modal fade', id: 'outcome-align-modal', tabindex: '-1', 'aria-labelledby': 'outcome-align-title', 'aria-hidden': 'true' });
        wrap.innerHTML =
            '<div class="modal-dialog modal-dialog-scrollable"><div class="modal-content">' +
            '<div class="modal-header"><h2 class="modal-title h6" id="outcome-align-title">Align outcomes</h2>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>' +
            '<div class="modal-body"><p class="small text-muted mb-2" data-outcome-target></p><div data-outcome-list></div>' +
            '<div class="small text-danger mt-2" role="alert" data-outcome-error></div></div>' +
            '<div class="modal-footer"><button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button type="button" class="btn btn-primary btn-sm" data-outcome-save>Save</button></div></div></div>';
        document.body.appendChild(wrap);
        wrap.querySelector('[data-outcome-save]').addEventListener('click', save);
        dialog = wrap;
        return wrap;
    }

    function render(data) {
        var box = ensureDialog();
        box.querySelector('[data-outcome-target]').textContent = data.targetTitle || '';
        box.querySelector('[data-outcome-error]').textContent = '';
        var list = box.querySelector('[data-outcome-list]');
        list.innerHTML = '';
        var aligned = {};
        (data.aligned || []).forEach(function (o) { aligned[o.id] = true; });
        var all = (data.available || []).slice();
        (data.aligned || []).forEach(function (o) { if (!all.some(function (a) { return a.id === o.id; })) all.push(o); });
        if (all.length === 0) {
            list.appendChild(el('p', { 'class': 'small mb-0' }, t('No outcomes are available for this course yet. Add course outcomes on the Learning Outcomes page.')));
            return;
        }
        var groups = { Course: t('Course outcomes'), Program: t('Program outcomes'), Institution: t('Institution outcomes') };
        ['Course', 'Program', 'Institution'].forEach(function (level) {
            var items = all.filter(function (o) { return o.level === level; });
            if (items.length === 0) return;
            var fieldset = el('fieldset', { 'class': 'mb-2' });
            fieldset.appendChild(el('legend', { 'class': 'small fw-semibold mb-1', style: 'font-size: .85rem;' }, groups[level]));
            items.forEach(function (o) {
                var id = 'oa-' + o.id;
                var row = el('div', { 'class': 'form-check' });
                var box = el('input', { 'class': 'form-check-input', type: 'checkbox', id: id, value: o.id });
                if (aligned[o.id]) box.checked = true;
                row.appendChild(box);
                var lab = el('label', { 'class': 'form-check-label small', 'for': id });
                lab.appendChild(el('strong', {}, o.code));
                lab.appendChild(document.createTextNode(' ' + o.title));
                row.appendChild(lab);
                fieldset.appendChild(row);
            });
            list.appendChild(fieldset);
        });
    }

    function open(button) {
        current = button;
        load(button.getAttribute('data-target-type'), button.getAttribute('data-target-id')).then(function (r) {
            var box = ensureDialog();
            if (!r.ok) {
                render({ targetTitle: '', aligned: [], available: [] });
                box.querySelector('[data-outcome-list]').innerHTML = '';
                box.querySelector('[data-outcome-error]').textContent = (r.body && r.body.message) || t('Outcomes could not be loaded.');
            } else {
                render(r.body);
            }
            window.bootstrap.Modal.getOrCreateInstance(box).show();
        });
    }

    function save() {
        if (!current) return;
        var box = ensureDialog();
        var ids = Array.prototype.map.call(box.querySelectorAll('[data-outcome-list] input:checked'), function (i) { return i.value; });
        var button = box.querySelector('[data-outcome-save]');
        button.disabled = true;
        fetch(endpoint, {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify({ targetType: current.getAttribute('data-target-type'), targetId: current.getAttribute('data-target-id'), outcomeIds: ids })
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (body) {
                button.disabled = false;
                if (!r.ok) { box.querySelector('[data-outcome-error]').textContent = body.message || t('The alignment could not be saved.'); return; }
                label(current, (body.aligned || []).length);
                window.bootstrap.Modal.getOrCreateInstance(box).hide();
            });
        }, function () {
            button.disabled = false;
            box.querySelector('[data-outcome-error]').textContent = t('The alignment could not be saved.');
        });
    }

    function init() {
        var buttons = Array.prototype.slice.call(document.querySelectorAll('[data-outcome-align]'));
        if (buttons.length === 0) return;
        load(buttons[0].getAttribute('data-target-type'), buttons[0].getAttribute('data-target-id')).then(function (probe) {
            if (!probe.ok) return; // module off or no access: the buttons stay hidden
            buttons.forEach(function (b) {
                if (!b.getAttribute('data-label')) b.setAttribute('data-label', b.textContent.trim() || 'Outcomes');
                b.hidden = false;
                b.addEventListener('click', function () { open(b); });
            });
            label(buttons[0], (probe.body.aligned || []).length);
            document.documentElement.setAttribute('data-outcomes-ready', 'true');
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
