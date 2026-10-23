// Plan M7 — module item forms on LMS Manage: show the fields each item type needs and only the requirements it allows.
(function () {
    'use strict';

    function syncMinScore(form) {
        var req = form.querySelector('[data-requirement]');
        var box = form.querySelector('[data-min-score]');
        if (!req || !box) return;
        var on = req.value === 'MustScoreAtLeast';
        box.hidden = !on;
        var input = box.querySelector('input');
        if (input) { input.required = on; input.disabled = !on; }
    }

    function syncAddForm(form) {
        var type = form.querySelector('[data-item-type]').value;
        var needsTarget = ['Header', 'Page', 'ExternalUrl'].indexOf(type) < 0;
        var show = function (selector, on) {
            var el = form.querySelector(selector);
            if (!el) return;
            el.hidden = !on;
            el.querySelectorAll('input, select, textarea').forEach(function (f) { f.disabled = !on; });
        };
        show('[data-for-target]', needsTarget);
        show('[data-for-url]', type === 'ExternalUrl');
        show('[data-for-body]', type === 'Page');
        var title = form.querySelector('[data-for-title] input');
        if (title) {
            title.required = !needsTarget;
            title.placeholder = needsTarget ? "Leave empty to use the item's name" : 'Title';
        }
        var url = form.querySelector('[data-for-url] input');
        if (url) url.required = type === 'ExternalUrl';

        // Only this type's course items.
        var target = form.querySelector('[data-target-select]');
        if (target) {
            var first = null;
            Array.prototype.forEach.call(target.options, function (o) {
                var match = o.getAttribute('data-type') === type;
                o.hidden = !match;
                o.disabled = !match;
                if (match && !first) first = o;
            });
            if (!target.selectedOptions.length || target.selectedOptions[0].disabled) target.value = first ? first.value : '';
            var empty = form.querySelector('[data-no-target]');
            if (needsTarget && !first) {
                if (!empty) {
                    empty = document.createElement('div');
                    empty.className = 'form-text text-danger';
                    empty.setAttribute('data-no-target', '');
                    target.parentNode.appendChild(empty);
                }
                empty.textContent = t('This course has nothing of this kind yet.');
            } else if (empty) empty.remove();
        }

        // Only the requirements this type allows.
        var req = form.querySelector('[data-requirement]');
        var allowed = JSON.parse(req.getAttribute('data-allowed') || '{}')[type] || ['None'];
        Array.prototype.forEach.call(req.options, function (o) {
            var ok = allowed.indexOf(o.value) >= 0;
            o.hidden = !ok;
            o.disabled = !ok;
        });
        if (allowed.indexOf(req.value) < 0) req.value = 'None';
        syncMinScore(form);
    }

    document.querySelectorAll('[data-add-item-form]').forEach(function (form) {
        form.querySelector('[data-item-type]').addEventListener('change', function () { syncAddForm(form); });
        form.querySelector('[data-requirement]').addEventListener('change', function () { syncMinScore(form); });
        syncAddForm(form);
    });
    document.querySelectorAll('[data-requirement-form]').forEach(function (form) {
        form.querySelector('[data-requirement]').addEventListener('change', function () { syncMinScore(form); });
        syncMinScore(form);
    });
})();
