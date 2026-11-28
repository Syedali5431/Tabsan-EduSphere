// Pastoral notes: the category list follows the kind of note and whether it is praise or a concern; "how serious" is
// asked only for concerns; a confidential note cannot be shared with the student or the family; "Everyone in the list"
// ticks every student.
(function () {
    'use strict';
    var form = document.querySelector('form[data-pastoral-note-form]');
    if (!form) return;

    var kind = form.querySelector('[data-note-kind]');
    var tones = Array.prototype.slice.call(form.querySelectorAll('[data-note-tone]'));
    var category = form.querySelector('[data-note-category]');
    var severityBox = form.querySelector('[data-severity-box]');
    var severity = form.querySelector('[data-note-severity]');
    var confidential = form.querySelector('[data-note-confidential]');
    var shares = Array.prototype.slice.call(form.querySelectorAll('[data-note-share]'));

    function tone() {
        var on = tones.filter(function (t) { return t.checked; })[0];
        return on ? on.value : 'Neutral';
    }

    function filterCategories() {
        if (!category) return;
        Array.prototype.forEach.call(category.options, function (o) {
            var k = o.getAttribute('data-kind');
            var t = o.getAttribute('data-tone');
            if (!k) return;
            var hide = (kind && k !== kind.value) || t !== tone();
            o.hidden = hide;
            o.disabled = hide;
            if (hide && o.selected) category.value = '';
        });
    }

    function showSeverity() {
        var concern = tone() === 'Concern';
        if (severityBox) severityBox.hidden = !concern;
        if (severity && !concern) severity.value = 'Low';
    }

    function lockSharing() {
        if (!confidential) return;
        shares.forEach(function (s) {
            if (confidential.checked) s.checked = false;
            s.disabled = confidential.checked;
        });
    }

    if (kind) kind.addEventListener('change', filterCategories);
    tones.forEach(function (t) { t.addEventListener('change', function () { filterCategories(); showSeverity(); }); });
    if (confidential) confidential.addEventListener('change', lockSharing);
    filterCategories();
    showSeverity();
    lockSharing();

    var all = form.querySelector('[data-select-all]');
    if (all) {
        var choices = Array.prototype.slice.call(form.querySelectorAll('[data-student-choice]'));
        function sync() { all.checked = choices.length > 0 && choices.every(function (c) { return c.checked; }); }
        all.addEventListener('change', function () { choices.forEach(function (c) { c.checked = all.checked; }); });
        choices.forEach(function (c) { c.addEventListener('change', sync); });
        sync();
    }
})();
