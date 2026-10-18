// Plan M2 — question editor for the quiz builder and question banks.
// Every form marked [data-quiz-editor] gets type-specific fields; on submit the options and the
// type settings are serialised into the hidden OptionsJson / ConfigJson inputs. The API validates
// everything again, so this script only shapes input. DOM is built with textContent/value only.
(function () {
    'use strict';

    var OPTION_TYPES = ['MultipleChoice', 'MultipleAnswers', 'TrueFalse'];
    var PARTIAL_TYPES = ['MultipleAnswers', 'Matching', 'Ordering', 'FillInBlank'];
    var SECTION_FOR = {
        MultipleChoice: 'options', MultipleAnswers: 'options', TrueFalse: 'options',
        Matching: 'matching', Ordering: 'ordering', FillInBlank: 'blanks',
        Numeric: 'numeric', Formula: 'formula', FileUpload: 'file', Essay: 'essay', ShortAnswer: null
    };
    var HELP = {
        MultipleChoice: 'Add the answer options and mark the one correct answer.',
        MultipleAnswers: 'Add the options and tick every correct one. With partial credit, wrong picks cancel right picks.',
        TrueFalse: 'Mark which statement is correct.',
        Matching: 'Each prompt has one correct answer. Wrong extra answers make it harder.',
        Ordering: 'List the items in the CORRECT order — students get them shuffled.',
        FillInBlank: 'Put [[1]], [[2]] … in the question text where the blanks go, then list the accepted answers for each.',
        Numeric: 'Give the exact answer (with an optional ± tolerance) or an accepted range.',
        Formula: 'Write variables in the question as [a], [b]. Each attempt gets its own random values.',
        ShortAnswer: 'Students type a short answer that you mark by hand.',
        Essay: 'Students write a long answer that you mark by hand.',
        FileUpload: 'Students upload a file that you mark by hand.'
    };

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) {
            if (k === 'class') node.className = attrs[k];
            else if (k === 'checked' || k === 'disabled') node[k] = !!attrs[k];
            else node.setAttribute(k, attrs[k]);
        });
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function parseJson(text, fallback) {
        if (!text) return fallback;
        try { return JSON.parse(text); } catch (e) { return fallback; }
    }

    function removeButton(onRemove, label) {
        var b = el('button', { type: 'button', class: 'btn btn-sm btn-outline-danger', 'aria-label': label || 'Remove' }, '×');
        b.addEventListener('click', onRemove);
        return b;
    }

    function init(form) {
        var typeSelect = form.querySelector('[data-field="type"]');
        var textArea = form.querySelector('[data-field="text"]');
        var help = form.querySelector('[data-type-help]');
        var optionsJson = form.querySelector('input[name="OptionsJson"]');
        var configJson = form.querySelector('input[name="ConfigJson"]');
        var existingConfig = parseJson(form.getAttribute('data-existing-config'), {}) || {};
        var existingOptions = parseJson(form.getAttribute('data-existing-options'), []) || [];
        var uid = form.id || ('qe' + Math.random().toString(36).slice(2));

        function section(name) { return form.querySelector('[data-section="' + name + '"]'); }
        function currentType() { return typeSelect.value; }

        // ── Options (MC / MA / TF) ───────────────────────────────────────────
        var optionList = form.querySelector('[data-options]');
        function addOption(text, correct) {
            var type = currentType();
            var row = el('div', { class: 'input-group input-group-sm mb-2', 'data-option-row': '' });
            var mark = el('div', { class: 'input-group-text' });
            var inputType = type === 'MultipleAnswers' ? 'checkbox' : 'radio';
            var check = el('input', { type: inputType, class: 'form-check-input mt-0', name: uid + '-correct', 'aria-label': 'Correct answer', checked: correct });
            mark.appendChild(check);
            var input = el('input', { type: 'text', class: 'form-control', maxlength: '1000', placeholder: 'Answer option', 'aria-label': 'Answer option' });
            input.value = text || '';
            row.appendChild(mark);
            row.appendChild(input);
            if (type !== 'TrueFalse') row.appendChild(removeButton(function () { row.remove(); }, 'Remove option'));
            optionList.appendChild(row);
        }
        function resetOptions(fromExisting) {
            optionList.textContent = '';
            var type = currentType();
            if (fromExisting && existingOptions.length) {
                existingOptions.forEach(function (o) { addOption(o.text, o.isCorrect); });
            } else if (type === 'TrueFalse') {
                addOption('True', true); addOption('False', false);
            } else {
                addOption('', true); addOption('', false); addOption('', false);
            }
            var addBtn = form.querySelector('[data-add-option]');
            if (addBtn) addBtn.hidden = type === 'TrueFalse';
        }
        var addOptionBtn = form.querySelector('[data-add-option]');
        if (addOptionBtn) addOptionBtn.addEventListener('click', function () { addOption('', false); });

        // ── Matching ─────────────────────────────────────────────────────────
        var pairList = form.querySelector('[data-pairs]');
        function addPair(prompt, answer) {
            var row = el('div', { class: 'input-group input-group-sm mb-2', 'data-pair-row': '' });
            var p = el('input', { type: 'text', class: 'form-control', placeholder: 'Prompt', 'aria-label': 'Prompt', maxlength: '1000' });
            var a = el('input', { type: 'text', class: 'form-control', placeholder: 'Correct answer', 'aria-label': 'Correct answer', maxlength: '1000' });
            p.value = prompt || ''; a.value = answer || '';
            row.appendChild(p); row.appendChild(el('span', { class: 'input-group-text' }, '→')); row.appendChild(a);
            row.appendChild(removeButton(function () { row.remove(); }, 'Remove pair'));
            pairList.appendChild(row);
        }
        var addPairBtn = form.querySelector('[data-add-pair]');
        if (addPairBtn) addPairBtn.addEventListener('click', function () { addPair('', ''); });

        // ── Ordering ─────────────────────────────────────────────────────────
        var orderList = form.querySelector('[data-order-items]');
        function addOrderItem(text) {
            var row = el('div', { class: 'input-group input-group-sm mb-2', 'data-order-row': '' });
            var input = el('input', { type: 'text', class: 'form-control', placeholder: 'Item', 'aria-label': 'Item', maxlength: '1000' });
            input.value = text || '';
            var up = el('button', { type: 'button', class: 'btn btn-sm btn-outline-secondary', 'aria-label': 'Move up' }, '↑');
            up.addEventListener('click', function () { if (row.previousElementSibling) row.parentNode.insertBefore(row, row.previousElementSibling); });
            row.appendChild(input); row.appendChild(up);
            row.appendChild(removeButton(function () { row.remove(); }, 'Remove item'));
            orderList.appendChild(row);
        }
        var addOrderBtn = form.querySelector('[data-add-order-item]');
        if (addOrderBtn) addOrderBtn.addEventListener('click', function () { addOrderItem(''); });

        // ── Fill in the blanks ───────────────────────────────────────────────
        var blankList = form.querySelector('[data-blanks]');
        function blankIds() {
            var ids = [], re = /\[\[([A-Za-z0-9_-]{1,20})\]\]/g, m;
            while ((m = re.exec(textArea.value)) !== null) if (ids.indexOf(m[1]) < 0) ids.push(m[1]);
            return ids;
        }
        function renderBlanks(seed) {
            var previous = {};
            blankList.querySelectorAll('[data-blank-row]').forEach(function (r) {
                previous[r.getAttribute('data-blank-row')] = { answers: r.querySelector('textarea').value, cs: r.querySelector('input[type=checkbox]').checked };
            });
            (seed || []).forEach(function (b) { previous[b.id] = { answers: (b.answers || []).join('\n'), cs: !!b.caseSensitive }; });
            blankList.textContent = '';
            var ids = blankIds();
            if (!ids.length) {
                blankList.appendChild(el('p', { class: 'small text-muted mb-0' }, 'No blanks yet — add [[1]] to the question text.'));
                return;
            }
            ids.forEach(function (id) {
                var row = el('div', { class: 'mb-2', 'data-blank-row': id });
                row.appendChild(el('label', { class: 'form-label small fw-semibold mb-1', for: uid + '-blank-' + id }, 'Blank [[' + id + ']] — accepted answers, one per line'));
                var ta = el('textarea', { class: 'form-control form-control-sm', rows: '2', id: uid + '-blank-' + id });
                ta.value = previous[id] ? previous[id].answers : '';
                var wrap = el('div', { class: 'form-check form-check-inline small mt-1' });
                var cs = el('input', { type: 'checkbox', class: 'form-check-input', id: uid + '-cs-' + id, checked: previous[id] && previous[id].cs });
                wrap.appendChild(cs); wrap.appendChild(el('label', { class: 'form-check-label', for: uid + '-cs-' + id }, 'Case-sensitive'));
                row.appendChild(ta); row.appendChild(wrap);
                blankList.appendChild(row);
            });
        }
        textArea.addEventListener('input', function () { if (currentType() === 'FillInBlank') renderBlanks(); });

        // ── Formula ──────────────────────────────────────────────────────────
        var varList = form.querySelector('[data-variables]');
        function addVariable(v) {
            v = v || {};
            var row = el('div', { class: 'row g-1 mb-2 align-items-center', 'data-var-row': '' });
            function cell(ph, val, width) {
                var c = el('div', { class: width });
                var input = el('input', { type: ph === 'name' ? 'text' : 'number', step: 'any', class: 'form-control form-control-sm', placeholder: ph, 'aria-label': ph });
                input.value = val === undefined || val === null ? '' : val;
                c.appendChild(input);
                return c;
            }
            row.appendChild(cell('name', v.name, 'col-3'));
            row.appendChild(cell('min', v.min, 'col-3'));
            row.appendChild(cell('max', v.max, 'col-3'));
            row.appendChild(cell('decimals', v.decimals === undefined ? 0 : v.decimals, 'col-2'));
            var rm = el('div', { class: 'col-1' }); rm.appendChild(removeButton(function () { row.remove(); }, 'Remove variable'));
            row.appendChild(rm);
            varList.appendChild(row);
        }
        var addVarBtn = form.querySelector('[data-add-variable]');
        if (addVarBtn) addVarBtn.addEventListener('click', function () { addVariable(); });
        var detectBtn = form.querySelector('[data-detect-variables]');
        if (detectBtn) detectBtn.addEventListener('click', function () {
            var have = Array.prototype.map.call(varList.querySelectorAll('[data-var-row]'), function (r) { return r.querySelector('input').value.toLowerCase(); });
            var re = /\[([A-Za-z_][A-Za-z0-9_]{0,19})\]/g, m;
            while ((m = re.exec(textArea.value)) !== null) {
                if (have.indexOf(m[1].toLowerCase()) < 0) { addVariable({ name: m[1], min: 1, max: 10, decimals: 0 }); have.push(m[1].toLowerCase()); }
            }
        });

        // ── Switching type ───────────────────────────────────────────────────
        function show(fromExisting) {
            var type = currentType();
            form.querySelectorAll('[data-section]').forEach(function (s) { s.hidden = s.getAttribute('data-section') !== SECTION_FOR[type]; });
            var partial = form.querySelector('[data-partial-credit]');
            if (partial) partial.hidden = PARTIAL_TYPES.indexOf(type) < 0;
            if (help) help.textContent = HELP[type] || '';
            if (OPTION_TYPES.indexOf(type) >= 0) resetOptions(fromExisting);
            if (type === 'FillInBlank') renderBlanks(fromExisting ? existingConfig.blanks : null);
        }

        // Prefill other sections from the existing config.
        (existingConfig.pairs || []).forEach(function (p) { addPair(p.prompt, p.answer); });
        if (!pairList.children.length) { addPair('', ''); addPair('', ''); }
        var distractors = form.querySelector('[data-field="distractors"]');
        if (distractors) distractors.value = (existingConfig.distractors || []).join('\n');
        (existingConfig.items || []).forEach(function (i) { addOrderItem(i.text); });
        if (!orderList.children.length) { addOrderItem(''); addOrderItem(''); addOrderItem(''); }
        (existingConfig.variables || []).forEach(addVariable);
        function setField(name, value) { var f = form.querySelector('[data-field="' + name + '"]'); if (f && value !== undefined && value !== null) f.value = value; }
        if (existingConfig.answer !== undefined && existingConfig.answer !== null) { setField('numAnswer', existingConfig.answer); setField('numTolerance', existingConfig.tolerance); }
        else if (existingConfig.min !== undefined || existingConfig.max !== undefined) {
            var rangeRadio = form.querySelector('[data-field="numMode"][value="range"]'); if (rangeRadio) rangeRadio.checked = true;
            setField('numMin', existingConfig.min); setField('numMax', existingConfig.max);
        }
        setField('expression', existingConfig.expression);
        setField('formulaDecimals', existingConfig.decimals);
        setField('formulaTolerance', existingConfig.tolerance);
        setField('extensions', (existingConfig.allowedExtensions || []).join(', '));
        setField('maxSizeMb', existingConfig.maxSizeMb);
        setField('wordLimit', existingConfig.wordLimit);
        var partialBox = form.querySelector('[data-field="partialCredit"]');
        if (partialBox && existingConfig.partialCredit === false) partialBox.checked = false;

        typeSelect.addEventListener('change', function () { show(false); });
        show(true);

        // ── Serialise on submit ──────────────────────────────────────────────
        function num(name) { var f = form.querySelector('[data-field="' + name + '"]'); return f && f.value !== '' ? Number(f.value) : null; }
        function val(name) { var f = form.querySelector('[data-field="' + name + '"]'); return f ? f.value : ''; }
        function lines(text) { return (text || '').split(/\r?\n|\|/).map(function (s) { return s.trim(); }).filter(Boolean); }

        form.addEventListener('submit', function () {
            var type = currentType();
            var partialCredit = partialBox ? partialBox.checked : true;
            var options = [], config = null;

            if (OPTION_TYPES.indexOf(type) >= 0) {
                optionList.querySelectorAll('[data-option-row]').forEach(function (r) {
                    options.push({ text: r.querySelector('input[type=text]').value, isCorrect: r.querySelector('.form-check-input').checked });
                });
                if (type === 'MultipleAnswers') config = { partialCredit: partialCredit };
            } else if (type === 'Matching') {
                config = { pairs: [], distractors: lines(val('distractors')), partialCredit: partialCredit };
                pairList.querySelectorAll('[data-pair-row]').forEach(function (r) {
                    var inputs = r.querySelectorAll('input');
                    if (inputs[0].value.trim() || inputs[1].value.trim()) config.pairs.push({ prompt: inputs[0].value, answer: inputs[1].value });
                });
            } else if (type === 'Ordering') {
                config = { items: [], partialCredit: partialCredit };
                orderList.querySelectorAll('[data-order-row] input').forEach(function (i) { if (i.value.trim()) config.items.push({ text: i.value }); });
            } else if (type === 'FillInBlank') {
                config = { blanks: [], partialCredit: partialCredit };
                blankList.querySelectorAll('[data-blank-row]').forEach(function (r) {
                    config.blanks.push({ id: r.getAttribute('data-blank-row'), answers: lines(r.querySelector('textarea').value), caseSensitive: r.querySelector('input[type=checkbox]').checked });
                });
            } else if (type === 'Numeric') {
                var mode = form.querySelector('[data-field="numMode"]:checked');
                config = mode && mode.value === 'range'
                    ? { min: num('numMin'), max: num('numMax') }
                    : { answer: num('numAnswer'), tolerance: num('numTolerance') || 0 };
            } else if (type === 'Formula') {
                config = { expression: val('expression'), variables: [], decimals: num('formulaDecimals') || 0, tolerance: num('formulaTolerance') || 0 };
                varList.querySelectorAll('[data-var-row]').forEach(function (r) {
                    var i = r.querySelectorAll('input');
                    if (i[0].value.trim()) config.variables.push({ name: i[0].value.trim(), min: Number(i[1].value || 0), max: Number(i[2].value || 0), decimals: Number(i[3].value || 0) });
                });
            } else if (type === 'FileUpload') {
                config = { allowedExtensions: val('extensions').split(/[,\s]+/).filter(Boolean), maxSizeMb: num('maxSizeMb') || 10 };
            } else if (type === 'Essay') {
                config = { wordLimit: num('wordLimit') };
            }

            optionsJson.value = JSON.stringify(options);
            configJson.value = config ? JSON.stringify(config) : '';
        });
    }

    document.querySelectorAll('form[data-quiz-editor]').forEach(init);
})();
