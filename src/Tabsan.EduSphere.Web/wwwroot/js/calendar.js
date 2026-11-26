// The calendar page: lays the month's homework, quizzes, live classes and key dates out in the browser's time zone, as a
// month grid or a list, with a filter per kind. Entries come from <script type="application/json" id="calendar-data">.
(function () {
    'use strict';
    var root = document.querySelector('[data-calendar]');
    var dataEl = document.getElementById('calendar-data');
    var t = window.t || function (s, v) { return v ? s.replace(/\{(\d+)\}/g, function (m, i) { return v[i]; }) : s; };

    // Copy the calendar link.
    var copy = document.querySelector('[data-calendar-copy]');
    if (copy) {
        copy.addEventListener('click', function () {
            var input = document.querySelector('[data-calendar-feed-url]');
            var done = document.querySelector('[data-calendar-copied]');
            if (!input) return;
            var show = function () { if (done) { done.hidden = false; setTimeout(function () { done.hidden = true; }, 3000); } };
            if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(input.value).then(show, function () { input.select(); });
            else { input.select(); try { document.execCommand('copy'); show(); } catch (e) { /* the text stays selected */ } }
        });
    }

    if (!root || !dataEl) return;
    var data;
    try { data = JSON.parse(dataEl.textContent || '{}'); } catch (e) { return; }
    var lang = document.documentElement.lang || undefined;
    var parts = String(data.month || '').split('-');
    var year = parseInt(parts[0], 10), monthIndex = parseInt(parts[1], 10) - 1;
    if (isNaN(year) || isNaN(monthIndex)) return;

    var storage = {
        get: function (k) { try { return window.localStorage.getItem(k); } catch (e) { return null; } },
        set: function (k, v) { try { window.localStorage.setItem(k, v); } catch (e) { /* private mode */ } }
    };
    var hidden = {};
    try { hidden = JSON.parse(storage.get('calendar-hidden-kinds') || '{}') || {}; } catch (e) { hidden = {}; }

    function fmt(options) { try { return new Intl.DateTimeFormat(lang, options); } catch (e) { return new Intl.DateTimeFormat(undefined, options); } }
    var timeFmt = fmt({ hour: 'numeric', minute: '2-digit' });
    var dayFmt = fmt({ weekday: 'long', day: 'numeric', month: 'long' });
    var weekdayFmt = fmt({ weekday: 'short' });
    var monthFmt = fmt({ month: 'long', year: 'numeric' });

    function pad(n) { return (n < 10 ? '0' : '') + n; }
    function key(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }

    // A key date is a calendar date (stored as midnight UTC): it stays on that date in every time zone.
    var events = (data.events || []).map(function (e) {
        var start = new Date(e.start);
        var day = e.allDay ? new Date(start.getUTCFullYear(), start.getUTCMonth(), start.getUTCDate()) : new Date(start.getFullYear(), start.getMonth(), start.getDate());
        return { e: e, start: start, day: day, dayKey: key(day) };
    }).sort(function (a, b) {
        if (a.dayKey !== b.dayKey) return a.dayKey < b.dayKey ? -1 : 1;
        if (a.e.allDay !== b.e.allDay) return a.e.allDay ? -1 : 1;
        return a.start - b.start;
    });

    function prefix(e) {
        switch (e.kind) {
            case 'Assignment': return t('Due');
            case 'Quiz': return t('Quiz closes');
            case 'LiveClass': return t('Live class');
            default: return t('Key date');
        }
    }
    function statusLabel(e) {
        switch (e.status) {
            case 'Done': return e.kind === 'Quiz' ? t('Taken') : t('Handed in');
            case 'Overdue': return t('Overdue');
            case 'Cancelled': return t('Cancelled');
            case 'Draft': return t('Not published');
            default: return null;
        }
    }
    function statusBadge(status) {
        return status === 'Done' ? 'text-bg-success' : status === 'Overdue' ? 'text-bg-danger' : status === 'Cancelled' ? 'text-bg-secondary' : 'text-bg-light border';
    }
    function facts(e) {
        var list = [];
        if (e.ownDates) list.push(e.kind === 'Quiz' ? t('Own closing time') : t('Own due date'));
        if (e.toMark > 0) list.push(t('{0} to mark', [e.toMark]));
        if (e.someOwnDates) list.push(t('Some students have their own dates'));
        return list;
    }
    function when(x) { return x.e.allDay ? t('All day') : timeFmt.format(x.start); }
    function firstName(name) { return String(name || '').trim().split(/\s+/)[0]; }
    function el(tag, cls, text) {
        var n = document.createElement(tag);
        if (cls) n.className = cls;
        if (text !== undefined && text !== null) n.textContent = text;
        return n;
    }
    function kindClass(kind) { return 'calendar-' + String(kind).toLowerCase(); }
    function visible(x) { return !hidden[x.e.kind]; }
    function inMonth(x) { return x.day.getFullYear() === year && x.day.getMonth() === monthIndex; }
    function fullText(x) {
        var e = x.e, s = prefix(e) + ': ' + e.title;
        if (e.course) s += ' · ' + e.course;
        if (e.student) s = firstName(e.student) + ': ' + s;
        var status = statusLabel(e);
        if (status) s += ' (' + status + ')';
        return when(x) + ' — ' + s;
    }

    var title = root.querySelector('[data-calendar-title]');
    if (title) title.textContent = monthFmt.format(new Date(year, monthIndex, 1));

    var monthEl = root.querySelector('[data-calendar-month]');
    var listEl = root.querySelector('[data-calendar-list]');
    var views = root.querySelector('[data-calendar-views]');
    var filters = root.querySelector('[data-calendar-filters]');

    function renderMonth() {
        monthEl.textContent = '';
        var first = new Date(year, monthIndex, 1);
        // Weeks start on Monday.
        var start = new Date(year, monthIndex, 1 - ((first.getDay() + 6) % 7));
        var last = new Date(year, monthIndex + 1, 0);
        var end = new Date(year, monthIndex + 1, 0 + ((7 - last.getDay()) % 7));
        var byDay = {};
        events.filter(visible).forEach(function (x) { (byDay[x.dayKey] = byDay[x.dayKey] || []).push(x); });
        var todayKey = key(new Date());

        var table = el('table', 'table table-bordered calendar-month mb-0');
        var caption = el('caption', 'visually-hidden', monthFmt.format(first));
        table.appendChild(caption);
        var head = el('thead'), headRow = el('tr');
        for (var i = 0; i < 7; i++) {
            var th = el('th', 'text-center small', weekdayFmt.format(new Date(2024, 0, 1 + i))); // 1 Jan 2024 was a Monday
            th.scope = 'col';
            headRow.appendChild(th);
        }
        head.appendChild(headRow);
        table.appendChild(head);
        var body = el('tbody'), row = null;
        for (var d = new Date(start); d <= end; d = new Date(d.getFullYear(), d.getMonth(), d.getDate() + 1)) {
            if (((d.getDay() + 6) % 7) === 0) { row = el('tr'); body.appendChild(row); }
            var k = key(d);
            var cell = el('td', 'calendar-day' + (d.getMonth() !== monthIndex ? ' calendar-outside' : '') + (k === todayKey ? ' calendar-today' : ''));
            var number = el('div', 'calendar-daynum', String(d.getDate()));
            if (k === todayKey) number.setAttribute('aria-current', 'date');
            cell.appendChild(number);
            var items = byDay[k] || [];
            if (items.length) {
                var ul = el('ul', 'list-unstyled mb-0');
                items.slice(0, 4).forEach(function (x) {
                    var li = el('li');
                    var chip = el(x.e.url ? 'a' : 'span', 'calendar-chip ' + kindClass(x.e.kind) + (x.e.status ? ' calendar-' + x.e.status.toLowerCase() : ''));
                    if (x.e.url) chip.href = x.e.url;
                    chip.title = fullText(x);
                    chip.setAttribute('aria-label', fullText(x));
                    if (!x.e.allDay) chip.appendChild(el('span', 'calendar-chip-time', timeFmt.format(x.start)));
                    chip.appendChild(document.createTextNode((x.e.student ? firstName(x.e.student) + ': ' : '') + x.e.title));
                    li.appendChild(chip);
                    ul.appendChild(li);
                });
                if (items.length > 4) {
                    var more = el('li');
                    var link = el('button', 'btn btn-link btn-sm p-0 calendar-more', t('+{0} more', [items.length - 4]));
                    link.type = 'button';
                    link.setAttribute('data-calendar-day', k);
                    more.appendChild(link);
                    ul.appendChild(more);
                }
                cell.appendChild(ul);
            }
            row.appendChild(cell);
        }
        table.appendChild(body);
        var wrap = el('div', 'table-responsive');
        wrap.appendChild(table);
        monthEl.appendChild(wrap);
    }

    function renderList() {
        listEl.textContent = '';
        var shown = events.filter(function (x) { return visible(x) && inMonth(x); });
        if (!shown.length) { listEl.appendChild(el('p', 'text-muted mb-0', t('Nothing in the calendar this month.'))); return; }
        var currentKey = null, ul = null;
        shown.forEach(function (x) {
            if (x.dayKey !== currentKey) {
                currentKey = x.dayKey;
                var h = el('h3', 'h6 mt-3 mb-2', dayFmt.format(x.day));
                h.id = 'calendar-day-' + x.dayKey;
                listEl.appendChild(h);
                ul = el('ul', 'list-unstyled calendar-agenda mb-0');
                listEl.appendChild(ul);
            }
            var e = x.e;
            var li = el('li', 'calendar-item ' + kindClass(e.kind) + (e.status ? ' calendar-' + e.status.toLowerCase() : ''));
            li.setAttribute('data-calendar-entry', e.kind);
            li.appendChild(el('span', 'calendar-time', when(x)));
            var what = el('span', 'calendar-what');
            if (e.student) what.appendChild(el('strong', null, firstName(e.student) + ': '));
            what.appendChild(el('span', 'text-muted', prefix(e) + ': '));
            if (e.url) { var a = el('a', null, e.title); a.href = e.url; what.appendChild(a); }
            else what.appendChild(document.createTextNode(e.title));
            if (e.course) what.appendChild(el('span', 'text-muted small', ' · ' + e.course));
            var status = statusLabel(e);
            if (status) what.appendChild(el('span', 'badge ms-1 ' + statusBadge(e.status), status));
            var extra = facts(e);
            if (e.note) extra.unshift(e.note);
            if (extra.length) what.appendChild(el('div', 'small text-muted', extra.join(' · ')));
            li.appendChild(what);
            ul.appendChild(li);
        });
    }

    var view = storage.get('calendar-view') || (window.matchMedia && window.matchMedia('(min-width: 768px)').matches ? 'month' : 'list');
    function show(v) {
        view = v === 'list' ? 'list' : 'month';
        monthEl.hidden = view !== 'month';
        listEl.hidden = view !== 'list';
        views.querySelectorAll('[data-calendar-view]').forEach(function (b) {
            var on = b.getAttribute('data-calendar-view') === view;
            b.classList.toggle('active', on);
            b.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
    }
    function render() { renderMonth(); renderList(); show(view); }

    views.hidden = false;
    filters.hidden = false;
    views.addEventListener('click', function (ev) {
        var b = ev.target.closest('[data-calendar-view]');
        if (!b) return;
        storage.set('calendar-view', b.getAttribute('data-calendar-view'));
        show(b.getAttribute('data-calendar-view'));
    });
    filters.querySelectorAll('[data-calendar-kind]').forEach(function (box) {
        var kind = box.getAttribute('data-calendar-kind');
        box.checked = !hidden[kind];
        box.addEventListener('change', function () {
            if (box.checked) delete hidden[kind]; else hidden[kind] = true;
            storage.set('calendar-hidden-kinds', JSON.stringify(hidden));
            render();
        });
    });
    // "+2 more" opens the list at that day.
    monthEl.addEventListener('click', function (ev) {
        var b = ev.target.closest('[data-calendar-day]');
        if (!b) return;
        show('list');
        var target = document.getElementById('calendar-day-' + b.getAttribute('data-calendar-day'));
        if (target) target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
    render();
})();
