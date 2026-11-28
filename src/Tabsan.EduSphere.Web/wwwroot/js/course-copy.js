// Course copy: each part's box ticks or clears its items, the "x of y chosen" count follows the ticks, the course list
// opens a course's content as soon as one is chosen, and history times show in the reader's own time zone.
(function () {
    'use strict';
    var t = window.t || function (text, values) {
        return String(text).replace(/\{(\d+)\}/g, function (_, i) { return values && values[i] !== undefined ? values[i] : ''; });
    };

    var lang = document.documentElement.lang || undefined;
    var short = new Intl.DateTimeFormat(lang, { dateStyle: 'medium', timeStyle: 'short' });
    var dateOnly = new Intl.DateTimeFormat(lang, { dateStyle: 'medium' });
    document.querySelectorAll('time[data-local-time]').forEach(function (el) {
        var d = new Date(el.getAttribute('datetime'));
        if (!isNaN(d)) el.textContent = (el.getAttribute('data-local-time') === 'date' ? dateOnly : short).format(d);
    });

    var source = document.querySelector('[data-copy-source]');
    if (source) source.addEventListener('change', function () { if (source.value) source.form.submit(); });

    document.querySelectorAll('[data-copy-part]').forEach(function (part) {
        var toggle = part.querySelector('[data-copy-part-toggle]');
        var chosen = part.querySelector('[data-copy-chosen]');
        var items = Array.prototype.slice.call(part.querySelectorAll('input[name="items"]'));
        function refresh() {
            var ticked = items.filter(function (b) { return b.checked; }).length;
            toggle.checked = ticked > 0;
            toggle.indeterminate = ticked > 0 && ticked < items.length;
            if (chosen) chosen.textContent = t('{0} of {1} chosen', [ticked, items.length]);
        }
        toggle.addEventListener('change', function () {
            items.forEach(function (b) { b.checked = toggle.checked; });
            refresh();
        });
        items.forEach(function (b) { b.addEventListener('change', refresh); });
    });

    var form = document.querySelector('[data-copy-form]');
    if (form) form.addEventListener('submit', function (e) {
        if (!form.querySelector('input[name="items"]:checked')) {
            e.preventDefault();
            var empty = form.querySelector('[data-copy-empty]');
            if (empty) empty.classList.remove('d-none');
        }
    });
})();
