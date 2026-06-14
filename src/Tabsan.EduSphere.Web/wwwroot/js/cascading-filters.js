/**
 * Phase 8: Cascading Filter System
 * 
 * Handles Institute → Department → Course → Semester/Class cascading.
 * When a parent dropdown changes, child dropdowns are reloaded via AJAX
 * and lower-level filters are reset.
 *
 * Usage: Add data-cascade attributes to trigger cascading behavior.
 *
 *   <select id="institutionType" data-cascade="institution"
 *           data-cascade-target="departmentId"
 *           data-cascade-api="/api/v1/departments">
 *
 * Targets are defined in cascadeTargets map below.
 */
(function () {
    'use strict';

    // ── Configuration: API endpoints for each cascade level ──────────────────

    var cascadeTargets = {
        institution: {
            target: 'departmentId',
            api: '/api/v1/departments',  // querystring: ?institutionType={value}
            resetBelow: ['courseId', 'courseOfferingId', 'semesterId', 'offeringId'],
            placeholder: '-- Select Department --'
        },
        department: {
            target: 'courseId',
            api: '/api/v1/courses',       // querystring: ?departmentId={value}
            resetBelow: ['courseOfferingId', 'semesterId', 'offeringId'],
            placeholder: '-- Select Course --'
        },
        course: {
            target: 'semesterId',
            // handled via course-offerings endpoint
            resetBelow: ['courseOfferingId', 'offeringId'],
            placeholder: '-- Select Semester/Class --'
        }
    };

    // ── Dynamic label resolver ────────────────────────────────────────────────

    function resolvePeriodLabel(institutionType) {
        // 0 = University → Semester,  1 = School → Class,  2 = College → Class
        var type = parseInt(institutionType, 10);
        return type === 0 ? 'Semester' : 'Class';
    }

    function updatePeriodLabels(newLabel) {
        var labels = document.querySelectorAll('[data-period-label]');
        for (var i = 0; i < labels.length; i++) {
            labels[i].textContent = newLabel;
        }
        var placeholders = document.querySelectorAll('[data-period-placeholder]');
        for (var j = 0; j < placeholders.length; j++) {
            placeholders[j].textContent = 'All ' + newLabel + 's';
        }
    }

    // ── Dropdown helpers ──────────────────────────────────────────────────────

    function setDropdownLoading(targetId, isLoading) {
        var el = document.getElementById(targetId);
        if (!el) return;
        el.disabled = isLoading;
        if (isLoading) {
            el.innerHTML = '<option value="">Loading...</option>';
        }
    }

    function populateDropdown(targetId, items, placeholder, selectedValue) {
        var el = document.getElementById(targetId);
        if (!el) return;
        el.disabled = false;
        var html = '<option value="">' + (placeholder || '-- Select --') + '</option>';
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var sel = (selectedValue && item.id === selectedValue) ? ' selected' : '';
            html += '<option value="' + item.id + '"' + sel + '>' + escapeHtml(item.name) + '</option>';
        }
        el.innerHTML = html;
    }

    function resetDropdown(targetId, placeholder) {
        var el = document.getElementById(targetId);
        if (!el) return;
        el.innerHTML = '<option value="">' + (placeholder || '-- Select --') + '</option>';
        el.disabled = true;
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    // ── Cascade handler ───────────────────────────────────────────────────────

    function handleCascadeChange(sourceEl) {
        var cascadeKey = sourceEl.getAttribute('data-cascade');
        var cfg = cascadeTargets[cascadeKey];
        if (!cfg) return;

        var selectedValue = sourceEl.value;

        // Reset children
        resetDropdown(cfg.target, cfg.placeholder);
        if (cfg.resetBelow) {
            for (var i = 0; i < cfg.resetBelow.length; i++) {
                resetDropdown(cfg.resetBelow[i], '-- Select --');
            }
        }

        // Update period labels if institution type changed
        if (cascadeKey === 'institution' && selectedValue) {
            updatePeriodLabels(resolvePeriodLabel(selectedValue));
        }

        if (!selectedValue) return;

        // Fetch filtered options
        setDropdownLoading(cfg.target, true);

        var apiUrl = cfg.api + '?' + cascadeKey.replace('institution', 'institutionType') + '=' + encodeURIComponent(selectedValue);

        // Special case: course cascade loads semester options via course-offerings
        if (cascadeKey === 'course') {
            apiUrl = '/api/v1/course-offerings?courseId=' + encodeURIComponent(selectedValue);
        }

        fetch(apiUrl)
            .then(function (res) {
                if (!res.ok) throw new Error('API error: ' + res.status);
                return res.json();
            })
            .then(function (data) {
                var items = Array.isArray(data) ? data.map(function (d) {
                    return { id: d.id, name: d.name || d.title || d.code };
                }) : [];
                if (cascadeKey === 'course') {
                    // Extract unique semesters from course offerings
                    var seen = {};
                    items = [];
                    for (var j = 0; j < data.length; j++) {
                        var sid = data[j].semesterId;
                        var sname = data[j].semesterName;
                        if (sid && !seen[sid]) {
                            seen[sid] = true;
                            items.push({ id: sid, name: sname });
                        }
                    }
                }
                populateDropdown(cfg.target, items, cfg.placeholder, null);
            })
            .catch(function (err) {
                console.warn('Cascade fetch failed:', err);
                setDropdownLoading(cfg.target, false);
                resetDropdown(cfg.target, '-- Error loading --');
            });
    }

    // ── Initialize ────────────────────────────────────────────────────────────

    function init() {
        var triggers = document.querySelectorAll('[data-cascade]');
        for (var i = 0; i < triggers.length; i++) {
            triggers[i].addEventListener('change', function () {
                handleCascadeChange(this);
            });
        }

        // Set initial period labels from institution type dropdown if present
        var instEl = document.querySelector('[data-cascade="institution"]');
        if (instEl && instEl.value) {
            updatePeriodLabels(resolvePeriodLabel(instEl.value));
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
