// School news, events and forms: times in the reader's own time zone; date pickers in local time that fill hidden UTC
// fields (all-day events keep their calendar date); the class list follows the chosen campus; the form builder adds,
// removes and renumbers questions and shows the choices box only for choice questions.
(function () {
    'use strict';
    var lang = document.documentElement.lang || undefined;
    var formats = {
        short: new Intl.DateTimeFormat(lang, { dateStyle: 'medium', timeStyle: 'short' }),
        date: new Intl.DateTimeFormat(lang, { dateStyle: 'medium' }),
        time: new Intl.DateTimeFormat(lang, { timeStyle: 'short' }),
        day: new Intl.DateTimeFormat(lang, { day: 'numeric' }),
        month: new Intl.DateTimeFormat(lang, { month: 'short' }),
        // All-day events are stored as a date: shown as that date wherever the reader is.
        allday: new Intl.DateTimeFormat(lang, { dateStyle: 'medium', timeZone: 'UTC' }),
        'allday-day': new Intl.DateTimeFormat(lang, { day: 'numeric', timeZone: 'UTC' }),
        'allday-month': new Intl.DateTimeFormat(lang, { month: 'short', timeZone: 'UTC' })
    };
    document.querySelectorAll('time[data-local-time]').forEach(function (el) {
        var d = new Date(el.getAttribute('datetime'));
        var f = formats[el.getAttribute('data-local-time')] || formats.short;
        if (!isNaN(d)) el.textContent = f.format(d);
    });

    function pad(n) { return (n < 10 ? '0' : '') + n; }
    function localValue(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + 'T' + pad(d.getHours()) + ':' + pad(d.getMinutes()); }

    document.querySelectorAll('form[data-community-form]').forEach(function (form) {
        var allDay = form.querySelector('[data-all-day]');
        var pickers = Array.prototype.slice.call(form.querySelectorAll('input[data-utc-for]'));
        function hiddenFor(picker) { return form.querySelector('input[type="hidden"][name="' + picker.getAttribute('data-utc-for') + '"]'); }
        function isDay(picker) { var n = picker.getAttribute('data-utc-for'); return allDay && allDay.checked && (n === 'StartsAt' || n === 'EndsAt'); }
        function write(picker) {
            var hidden = hiddenFor(picker);
            if (!hidden) return;
            if (!picker.value) { hidden.value = ''; return; }
            hidden.value = isDay(picker) ? picker.value.slice(0, 10) + 'T00:00:00Z' : new Date(picker.value).toISOString();
        }
        pickers.forEach(function (picker) {
            var hidden = hiddenFor(picker);
            if (hidden && hidden.value) {
                var d = new Date(hidden.value);
                if (!isNaN(d)) picker.value = isDay(picker) ? hidden.value.slice(0, 10) + 'T00:00' : localValue(d);
            }
            picker.addEventListener('change', function () { write(picker); });
        });
        if (allDay) allDay.addEventListener('change', function () { pickers.forEach(write); });
        form.addEventListener('submit', function () { pickers.forEach(write); });
    });

    // The class list follows the chosen campus.
    document.querySelectorAll('[data-audience-picker]').forEach(function (picker) {
        var campus = picker.querySelector('[data-audience-campus]');
        var classes = picker.querySelector('[data-audience-class]');
        if (!campus || !classes) return;
        function filter() {
            Array.prototype.forEach.call(classes.options, function (o) {
                var at = o.getAttribute('data-campus');
                var hide = !!(campus.value && at && at !== campus.value);
                o.hidden = hide;
                o.disabled = hide;
                if (hide && o.selected) classes.value = '';
            });
        }
        campus.addEventListener('change', filter);
        filter();
    });

    // Form builder.
    var builder = document.querySelector('[data-form-builder]');
    if (builder) {
        var list = builder.querySelector('[data-question-list]');
        function showOptions(row) {
            var type = row.querySelector('[data-question-type]');
            var options = row.querySelector('[data-question-options]');
            if (type && options) options.hidden = !(type.value === 'SingleChoice' || type.value === 'MultipleChoice');
        }
        function renumber() {
            Array.prototype.forEach.call(list.querySelectorAll('[data-question-row]'), function (row, n) {
                row.querySelectorAll('[name]').forEach(function (el) { el.name = el.name.replace(/Fields\[\d+\]/, 'Fields[' + n + ']'); });
                row.querySelectorAll('[id]').forEach(function (el) { el.id = el.id.replace(/-\d+$/, '-' + n); });
                row.querySelectorAll('label[for]').forEach(function (el) { el.htmlFor = el.htmlFor.replace(/-\d+$/, '-' + n); });
            });
        }
        list.querySelectorAll('[data-question-row]').forEach(showOptions);
        builder.addEventListener('change', function (e) {
            if (e.target.matches('[data-question-type]')) showOptions(e.target.closest('[data-question-row]'));
        });
        builder.addEventListener('click', function (e) {
            var remove = e.target.closest('[data-remove-question]');
            if (remove) {
                var row = remove.closest('[data-question-row]');
                if (list.querySelectorAll('[data-question-row]').length > 1) row.remove();
                else row.querySelectorAll('input[type="text"], input:not([type]), textarea').forEach(function (el) { el.value = ''; });
                renumber();
                return;
            }
            if (e.target.closest('[data-add-question]')) {
                var rows = list.querySelectorAll('[data-question-row]');
                var copy = rows[rows.length - 1].cloneNode(true);
                copy.querySelectorAll('input:not([type="checkbox"]):not([type="hidden"]), textarea').forEach(function (el) { el.value = ''; });
                copy.querySelectorAll('input[type="checkbox"]').forEach(function (el) { el.checked = false; });
                var type = copy.querySelector('[data-question-type]');
                if (type) type.value = 'ShortText';
                list.appendChild(copy);
                renumber();
                showOptions(copy);
                var label = copy.querySelector('input[name$=".Label"]');
                if (label) label.focus();
            }
        });
    }
})();
