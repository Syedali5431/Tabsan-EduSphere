// Messages pages: times in the browser's time zone, the people filter and "select the whole group" on New message, and the
// latest message in view when a conversation opens.
(function () {
    'use strict';
    var lang = document.documentElement.lang || undefined;
    function fmt(options) { try { return new Intl.DateTimeFormat(lang, options); } catch (e) { return new Intl.DateTimeFormat(undefined, options); } }
    var short = fmt({ day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' });
    var long = fmt({ weekday: 'short', day: 'numeric', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' });
    document.querySelectorAll('time[data-local-time]').forEach(function (el) {
        var d = new Date(el.getAttribute('datetime'));
        if (!isNaN(d)) el.textContent = (el.getAttribute('data-local-time') === 'long' ? long : short).format(d);
    });

    // A conversation opens at its latest message.
    var list = document.querySelector('.message-list');
    if (list && list.lastElementChild) list.lastElementChild.scrollIntoView({ block: 'nearest' });

    var contacts = document.querySelector('[data-contact-list]');
    if (!contacts) return;
    var count = document.querySelector('[data-selected-count]');
    function boxes(scope) { return Array.prototype.slice.call(scope.querySelectorAll('input[name="recipientIds"]')); }
    function refresh() {
        if (count) count.textContent = String(boxes(contacts).filter(function (b) { return b.checked; }).length);
        contacts.querySelectorAll('[data-contact-group]').forEach(function (g) {
            var all = boxes(g).filter(function (b) { return !b.closest('[data-contact]').hidden; });
            var on = all.filter(function (b) { return b.checked; }).length;
            var head = g.querySelector('[data-select-group]');
            head.checked = all.length > 0 && on === all.length;
            head.indeterminate = on > 0 && on < all.length;
        });
    }
    contacts.addEventListener('change', function (e) {
        var head = e.target.closest('[data-select-group]');
        if (head) {
            boxes(head.closest('[data-contact-group]')).forEach(function (b) { if (!b.closest('[data-contact]').hidden) b.checked = head.checked; });
        }
        refresh();
    });

    // Typing narrows the list at once (the Search button asks the server, for long lists).
    var filter = document.querySelector('[data-contact-filter]');
    if (filter) {
        filter.addEventListener('input', function () {
            var q = filter.value.trim().toLowerCase();
            contacts.querySelectorAll('[data-contact-group]').forEach(function (g) {
                var shown = 0;
                g.querySelectorAll('[data-contact]').forEach(function (row) {
                    var hit = !q || row.textContent.toLowerCase().indexOf(q) >= 0 || g.querySelector('label').textContent.toLowerCase().indexOf(q) >= 0;
                    row.hidden = !hit;
                    if (hit) shown++;
                });
                g.hidden = shown === 0;
            });
            refresh();
        });
    }
    refresh();
})();
