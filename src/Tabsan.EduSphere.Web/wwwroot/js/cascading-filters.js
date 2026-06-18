/**
 * Phase 40: Full Cascading Filter System
 *
 * Handles the complete filter chain:
 *   Institute → Tenant → Campus → Department → Course → Period → Subject
 *
 * Role-based starting points:
 *   SuperAdmin  → Institute (all 7 levels)
 *   Admin       → Campus (5 levels: Campus→Dept→Course→Period→Subject)
 *   Finance     → Campus (5 levels: Campus→Dept→Course→Period→Subject)
 *   Faculty     → Department (4 levels: Dept→Course→Period→Subject)
 *   Student     → Period (2 levels: Period→Subject)
 *
 * Each filter form should have data-cascade-filter attribute.
 * Each dependent <select> should have data-cascade="<level>" attribute.
 * Period labels update dynamically via data-period-label and data-period-placeholder.
 */
(function () {
    'use strict';

    // ── Configuration ────────────────────────────────────────────────────────

    var DEFAULT_API_BASE = '/api/v1/filters';

    var CASCADE_CHAIN = [
        { key: 'institution',  label: 'Institute',   api: '/tenants'      },
        { key: 'tenant',       label: 'Tenant',       api: '/campuses'     },
        { key: 'campus',       label: 'Campus',       api: '/departments'  },
        { key: 'department',   label: 'Department',   api: '/courses'      },
        { key: 'course',       label: 'Course',       api: '/periods'      },
        { key: 'period',       label: 'Period',       api: '/subjects'     },
        { key: 'subject',      label: 'Subject',       api: null            }
    ];

    var ROLE_START_LEVELS = {
        'SuperAdmin': 0,
        'Admin':      2,
        'Finance':    2,
        'Faculty':    3,
        'Student':    5
    };

    var currentRole = null;
    var currentStartLevel = 0;

    // ── Helpers ──────────────────────────────────────────────────────────────

    function getSelectEl(levelKey) {
        return document.querySelector('[data-cascade="' + levelKey + '"]');
    }

    function setLoading(el, isLoading) {
        if (!el) return;
        el.disabled = isLoading;
        if (isLoading) el.innerHTML = '<option value="">Loading...</option>';
    }

    function populateDropdown(el, items, placeholder, selectedValue) {
        if (!el) return;
        el.disabled = false;
        var html = '<option value="">' + (placeholder || '-- Select --') + '</option>';
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var id = item.id !== undefined ? item.id : item.value;
            var name = item.name || item.title || item.code || item.semesterName || String(id);
            var sel = (selectedValue !== undefined && selectedValue !== null &&
                       String(id) === String(selectedValue)) ? ' selected' : '';
            html += '<option value="' + id + '"' + sel + '>' + escapeHtml(name) + '</option>';
        }
        el.innerHTML = html;
    }

    function resetDropdowns(fromLevelKey) {
        var found = false;
        for (var i = 0; i < CASCADE_CHAIN.length; i++) {
            if (CASCADE_CHAIN[i].key === fromLevelKey) { found = true; continue; }
            if (!found) continue;
            var el = getSelectEl(CASCADE_CHAIN[i].key);
            if (el) {
                el.innerHTML = '<option value="">-- Select ' + CASCADE_CHAIN[i].label + ' --</option>';
                el.disabled = true;
            }
        }
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str || ''));
        return div.innerHTML;
    }

    function getLevelIndex(levelKey) {
        for (var i = 0; i < CASCADE_CHAIN.length; i++) {
            if (CASCADE_CHAIN[i].key === levelKey) return i;
        }
        return -1;
    }

    // ── Core cascade handler ─────────────────────────────────────────────────

    function handleCascadeChange(sourceEl) {
        var levelKey = sourceEl.getAttribute('data-cascade');
        var selectedValue = sourceEl.value;

        resetDropdowns(levelKey);

        if (levelKey === 'institution' && selectedValue) {
            updatePeriodLabel(selectedValue);
        }

        if (!selectedValue) return;

        if (levelKey === 'course') {
            handleCourseChange(selectedValue);
            return;
        }

        fetchAndPopulate(levelKey, selectedValue);
    }

    function fetchAndPopulate(sourceLevelKey, sourceValue) {
        var targetKey = getNextLevelKey(sourceLevelKey);
        if (!targetKey) return;

        var targetEl = getSelectEl(targetKey);
        if (!targetEl) return;

        setLoading(targetEl, true);

        var apiUrl = buildFetchUrl(sourceLevelKey, sourceValue);
        if (!apiUrl) { setLoading(targetEl, false); return; }

        fetch(apiUrl)
            .then(function (res) {
                if (!res.ok) throw new Error('API error: ' + res.status);
                return res.json();
            })
            .then(function (data) {
                var items = mapToItems(data, targetKey);
                var label = CASCADE_CHAIN[getLevelIndex(targetKey)].label;
                populateDropdown(targetEl, items, '-- Select ' + label + ' --', null);
            })
            .catch(function (err) {
                console.warn('Cascade fetch failed for ' + sourceLevelKey + ':', err);
                if (targetEl) targetEl.innerHTML = '<option value="">-- Error loading --</option>';
                setLoading(targetEl, false);
            });
    }

    function handleCourseChange(courseId) {
        fetch(DEFAULT_API_BASE + '/periods?courseId=' + encodeURIComponent(courseId))
            .then(function (res) {
                if (!res.ok) throw new Error('API error: ' + res.status);
                return res.json();
            })
            .then(function (data) {
                var periodEl = getSelectEl('period');
                var subjectEl = getSelectEl('subject');
                if (!periodEl) return;

                if (data.label) updatePeriodLabelText(data.label);

                var items = (data.periods || []).map(function (p) {
                    return { id: p.id || p.value, name: p.name };
                });
                populateDropdown(periodEl, items, '-- Select ' + (data.label || 'Period') + ' --', null);

                if (subjectEl) {
                    subjectEl.innerHTML = '<option value="">-- Select Subject --</option>';
                    subjectEl.disabled = true;
                }
            })
            .catch(function (err) {
                console.warn('Period fetch failed:', err);
            });
    }

    function buildFetchUrl(sourceLevelKey, sourceValue) {
        switch (sourceLevelKey) {
            case 'institution':
                return DEFAULT_API_BASE + '/tenants?institutionType=' + encodeURIComponent(sourceValue);
            case 'tenant':
                return DEFAULT_API_BASE + '/campuses?tenantId=' + encodeURIComponent(sourceValue);
            case 'campus':
                return DEFAULT_API_BASE + '/departments?campusId=' + encodeURIComponent(sourceValue);
            case 'department':
                return DEFAULT_API_BASE + '/courses?departmentId=' + encodeURIComponent(sourceValue);
            case 'period':
                var courseEl = getSelectEl('course');
                var cid = courseEl ? courseEl.value : '';
                return DEFAULT_API_BASE + '/subjects?courseId=' + encodeURIComponent(cid) +
                       '&period=' + encodeURIComponent(sourceValue);
            default:
                return null;
        }
    }

    function getNextLevelKey(currentKey) {
        for (var i = 0; i < CASCADE_CHAIN.length - 1; i++) {
            if (CASCADE_CHAIN[i].key === currentKey) return CASCADE_CHAIN[i + 1].key;
        }
        return null;
    }

    function mapToItems(data, targetKey) {
        if (!Array.isArray(data)) {
            if (data && data.periods) return mapToItems(data.periods, targetKey);
            return [];
        }
        return data.map(function (d) {
            return {
                id: d.id !== undefined ? d.id : d.value,
                name: d.name || d.title || d.code || d.semesterName || String(d.id || d.value || '')
            };
        });
    }

    // ── Period label management ─────────────────────────────────────────────

    function resolvePeriodLabelFromInstitution(institutionType) {
        var type = parseInt(institutionType, 10);
        return type === 0 ? 'Semester' : 'Class';
    }

    function updatePeriodLabel(institutionType) {
        updatePeriodLabelText(resolvePeriodLabelFromInstitution(institutionType));
    }

    function updatePeriodLabelText(newLabel) {
        var labels = document.querySelectorAll('[data-period-label]');
        for (var i = 0; i < labels.length; i++) {
            labels[i].textContent = newLabel;
        }
        var placeholders = document.querySelectorAll('[data-period-placeholder]');
        for (var j = 0; j < placeholders.length; j++) {
            placeholders[j].textContent = 'All ' + newLabel + 's';
        }
        var periodEl = getSelectEl('period');
        if (periodEl && !periodEl.disabled) {
            var defaultOpt = periodEl.querySelector('option[value=""]');
            if (defaultOpt) defaultOpt.textContent = '-- Select ' + newLabel + ' --';
        }
    }

    // ── Initialization ──────────────────────────────────────────────────────

    function initFilterForm(formEl) {
        if (!formEl) return;

        var role = formEl.getAttribute('data-filter-role') ||
                   document.body.getAttribute('data-user-role') ||
                   detectRole();

        currentRole = role;
        currentStartLevel = ROLE_START_LEVELS[role] || 0;

        applyRoleVisibility(formEl, currentStartLevel);

        var triggers = formEl.querySelectorAll('[data-cascade]');
        for (var i = 0; i < triggers.length; i++) {
            var clone = triggers[i].cloneNode(true);
            triggers[i].parentNode.replaceChild(clone, triggers[i]);
            clone.addEventListener('change', function () {
                handleCascadeChange(this);
            });
        }
    }

    function applyRoleVisibility(formEl, startLevel) {
        for (var i = 0; i < CASCADE_CHAIN.length; i++) {
            if (i >= startLevel) continue;
            var key = CASCADE_CHAIN[i].key;
            var el = getSelectEl(key);
            if (!el) continue;
            var wrapper = el.closest('.col-md-3, .col-md-4, .col-12, .col-md-2');
            if (wrapper) wrapper.style.display = 'none';
            el.disabled = true;
        }
    }

    function detectRole() {
        var roleEl = document.querySelector('[data-user-role]');
        if (roleEl) return roleEl.getAttribute('data-user-role');
        return 'Student';
    }

    // ── Global API ──────────────────────────────────────────────────────────

    window.CascadeFilters = {
        configure: function (options) {
            if (options.apiBase) DEFAULT_API_BASE = options.apiBase;
            if (options.role) currentRole = options.role;
            if (options.startLevel !== undefined) currentStartLevel = options.startLevel;
        },
        init: function (formSelector) {
            var forms = formSelector
                ? document.querySelectorAll(formSelector)
                : document.querySelectorAll('[data-cascade-filter]');
            for (var i = 0; i < forms.length; i++) {
                initFilterForm(forms[i]);
            }
        },
        refresh: function (levelKey, value) {
            var el = getSelectEl(levelKey);
            if (el) {
                el.value = value || '';
                handleCascadeChange(el);
            }
        },
        getRole: function () { return currentRole; },
        getStartLevel: function () { return currentStartLevel; }
    };

    // ── Auto-init ───────────────────────────────────────────────────────────

    function autoInit() {
        var forms = document.querySelectorAll('[data-cascade-filter]');
        for (var i = 0; i < forms.length; i++) {
            initFilterForm(forms[i]);
        }

        var instEl = getSelectEl('institution');
        if (instEl && instEl.value) {
            updatePeriodLabel(instEl.value);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoInit);
    } else {
        autoInit();
    }
})();
