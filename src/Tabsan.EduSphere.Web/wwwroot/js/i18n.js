// Plan M11 — portal language in scripts. t('English text') returns the translation the page embedded
// (<script type="application/json" id="portal-text">); values: t('Saved {0} item(s).', [3]).
(function () {
    'use strict';
    var dict = {};
    try {
        var data = document.getElementById('portal-text');
        if (data) dict = JSON.parse(data.textContent || '{}') || {};
    } catch (e) { dict = {}; }

    window.t = function (text, values) {
        // Keys are the trimmed English text; spaces around a fragment ('Marks out of ') are kept.
        var key = String(text).trim();
        var s = Object.prototype.hasOwnProperty.call(dict, key) ? text.replace(key, dict[key]) : text;
        if (values) s = s.replace(/\{(\d+)\}/g, function (m, i) { return values[i] !== undefined ? values[i] : m; });
        return s;
    };

    // Language switcher: choosing a language submits its form.
    document.addEventListener('change', function (e) {
        var select = e.target && e.target.closest ? e.target.closest('[data-lang-select]') : null;
        if (select && select.form) select.form.submit();
    });
})();
