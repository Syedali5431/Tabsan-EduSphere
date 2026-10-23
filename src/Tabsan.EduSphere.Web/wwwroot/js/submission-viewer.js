// Plan M3.3 — submission viewer: PDF pages (PDF.js) or an image, with an SVG annotation layer.
// Geometry is stored page-relative (0–1), so annotations stay in place at any zoom.
// Graders get drawing tools; students see annotations read-only (the API only returns them once the grade is posted).
// Security: PDF.js runs with isEvalSupported:false (CVE-2024-4367) and every user string is set as text, never HTML.
(function () {
    'use strict';

    var SVG = 'http://www.w3.org/2000/svg';
    var VIEW_W = 1000; // SVG units across a page; height follows the page's aspect ratio

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) {
            if (k === 'class') node.className = attrs[k];
            else if (k.indexOf('data-') === 0 || k === 'role' || k.indexOf('aria-') === 0 || k === 'title' || k === 'type') node.setAttribute(k, attrs[k]);
            else node[k] = attrs[k];
        });
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function svg(tag, attrs) {
        var node = document.createElementNS(SVG, tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        return node;
    }

    function round(v) { return Math.round(Math.max(0, Math.min(1, v)) * 100000) / 100000; }

    function postJson(url, token, body) {
        return fetch(url, {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: body === undefined ? '{}' : JSON.stringify(body)
        }).then(function (r) {
            return r.text().then(function (body) {
                var data = null;
                try { data = body ? JSON.parse(body) : null; } catch (e) { data = null; }
                if (!r.ok) throw new Error((data && data.message) || (t('Request failed (') + r.status + ')'));
                return data;
            });
        });
    }

    var TYPE_LABELS = { Highlight: 'Highlight', Strikeout: 'Strike-out', Box: 'Box', Text: 'Text', Pin: t('Comment pin'), Freehand: 'Drawing' };

    /**
     * options: {
     *   kind: 'pdf' | 'image' | 'download', src, downloadUrl, fileName, fileId,
     *   editable, token, urls: { list, add, update, remove } (update/remove take an id), onChange(count)
     * }
     */
    function SubmissionViewer(host, options) {
        this.host = host;
        this.o = options;
        this.annotations = [];
        this.pages = [];   // { number, wrap, svg, aspect }
        this.tool = 'none';
        this.color = '#f5c400';
        this.zoom = 1;
        this.selectedId = null;
        this.disposed = false;
        this.build();
    }

    SubmissionViewer.prototype.build = function () {
        var self = this, o = this.o;
        this.host.textContent = '';
        var bar = el('div', { class: 'd-flex flex-wrap gap-1 align-items-center mb-2 sv-toolbar', role: 'toolbar', 'aria-label': t('Viewer tools') });
        if (o.kind !== 'download') {
            bar.appendChild(this.button('−', t('Zoom out'), function () { self.setZoom(self.zoom / 1.2); }));
            bar.appendChild(this.button('Fit', t('Fit to width'), function () { self.setZoom(1); }));
            bar.appendChild(this.button('+', t('Zoom in'), function () { self.setZoom(self.zoom * 1.2); }));
        }
        if (o.editable && o.kind !== 'download') {
            bar.appendChild(el('span', { class: 'vr mx-1' }));
            [['none', '✋', t('Select / scroll')], ['Highlight', '▮', 'Highlight'], ['Strikeout', 'S̶', 'Strike-out'], ['Box', '▭', 'Box'],
             ['Text', 'T', 'Text'], ['Pin', '📌', t('Comment pin')], ['Freehand', '✎', 'Draw']].forEach(function (tool) {
                var b = self.button(tool[1], tool[2], function () { self.setTool(tool[0]); });
                b.setAttribute('data-tool', tool[0]);
                bar.appendChild(b);
            });
            var color = el('input', { type: 'color', value: this.color, class: 'form-control form-control-color form-control-sm', title: 'Colour', 'aria-label': t('Annotation colour') });
            color.addEventListener('input', function () { self.color = color.value; });
            bar.appendChild(color);
        }
        var download = el('a', { class: 'btn btn-sm btn-outline-secondary ms-auto', href: o.downloadUrl }, 'Download');
        download.setAttribute('download', '');
        bar.appendChild(download);
        if (o.annotatedUrl) bar.appendChild(el('a', { class: 'btn btn-sm btn-outline-secondary', href: o.annotatedUrl }, t('Annotated PDF')));
        this.host.appendChild(bar);

        this.status = el('div', { class: 'small text-muted mb-1', 'aria-live': 'polite' });
        this.host.appendChild(this.status);
        // Focusable so keyboard users can scroll the pages (WCAG 2.1.1).
        this.canvasArea = el('div', { class: 'sv-pages border rounded bg-light p-2', style: 'overflow:auto; max-height: 75vh;', tabIndex: 0, role: 'region', 'aria-label': t('Document pages') });
        this.host.appendChild(this.canvasArea);
        this.list = el('div', { class: 'sv-list mt-2' });
        this.host.appendChild(this.list);
        this.setTool('none');

        if (o.kind === 'download') {
            this.canvasArea.appendChild(el('div', { class: 'p-4 text-center text-muted' },
                o.message || t('This file cannot be shown here. Use Download to open it.')));
            return;
        }
        this.load();
    };

    SubmissionViewer.prototype.button = function (label, title, onClick) {
        var b = el('button', { type: 'button', class: 'btn btn-sm btn-outline-secondary', title: title, 'aria-label': title }, label);
        b.addEventListener('click', onClick);
        return b;
    };

    SubmissionViewer.prototype.setTool = function (tool) {
        this.tool = tool;
        this.host.querySelectorAll('[data-tool]').forEach(function (b) {
            var on = b.getAttribute('data-tool') === tool;
            b.classList.toggle('active', on);
            b.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
        this.pages.forEach(function (p) { p.svg.style.cursor = tool === 'none' ? 'default' : 'crosshair'; p.svg.style.pointerEvents = tool === 'none' ? 'none' : 'auto'; });
    };

    SubmissionViewer.prototype.setZoom = function (z) {
        this.zoom = Math.max(0.4, Math.min(4, z));
        this.renderPages();
    };

    SubmissionViewer.prototype.load = function () {
        var self = this, o = this.o;
        this.status.textContent = t('Loading…');
        var annotationsPromise = o.urls && o.urls.list
            ? fetch(o.urls.list, { credentials: 'same-origin' }).then(function (r) { return r.ok ? r.json() : []; }).catch(function () { return []; })
            : Promise.resolve([]);

        var docPromise;
        if (o.kind === 'pdf') {
            if (!window.pdfjsLib) { this.status.textContent = t('The PDF viewer could not load.'); return; }
            window.pdfjsLib.GlobalWorkerOptions.workerSrc = o.workerSrc || '/lib/pdfjs/pdf.worker.min.js';
            docPromise = window.pdfjsLib.getDocument({ url: o.src, isEvalSupported: false, enableXfa: false, withCredentials: true }).promise
                .then(function (pdf) { self.pdf = pdf; return pdf.numPages; });
        } else {
            docPromise = new Promise(function (resolve, reject) {
                var img = new Image();
                img.onload = function () { self.image = img; resolve(1); };
                img.onerror = function () { reject(new Error(t('The image could not be loaded.'))); };
                img.src = o.src;
            });
        }

        Promise.all([docPromise, annotationsPromise]).then(function (r) {
            if (self.disposed) return;
            self.pageCount = r[0];
            self.annotations = Array.isArray(r[1]) ? r[1] : [];
            self.status.textContent = self.pageCount + (self.pageCount === 1 ? ' page' : ' pages');
            self.renderPages();
        }).catch(function (e) {
            self.status.textContent = (e && e.message) || t('The file could not be shown.');
        });
    };

    SubmissionViewer.prototype.renderPages = function () {
        var self = this;
        if (!this.pageCount) return;
        this.canvasArea.textContent = '';
        this.pages = [];
        var width = Math.max(320, (this.canvasArea.clientWidth || 800) - 24) * this.zoom;
        var chain = Promise.resolve();
        for (var n = 1; n <= this.pageCount; n++) {
            (function (number) {
                chain = chain.then(function () { return self.renderPage(number, width); });
            })(n);
        }
        this.host.removeAttribute('data-ready');
        var generation = this.generation = (this.generation || 0) + 1;
        chain.then(function () {
            if (self.disposed || generation !== self.generation) return;
            self.drawAll(); self.renderList(); self.setTool(self.tool);
            self.host.setAttribute('data-ready', 'true'); // pages and annotations are in place
        }).catch(function (e) {
            // Switching file/student (dispose) or zooming (new generation) cancels rendering on purpose.
            if (self.disposed || generation !== self.generation) return;
            self.status.textContent = (e && e.message) || t('The file could not be shown.');
        });
    };

    SubmissionViewer.prototype.renderPage = function (number, width) {
        var self = this;
        if (this.disposed) return Promise.resolve();
        var wrap = el('div', { class: 'sv-page position-relative mx-auto mb-3 shadow-sm bg-white', 'data-page': String(number) });
        wrap.setAttribute('aria-label', 'Page ' + number);
        this.canvasArea.appendChild(wrap);

        function finish(aspect, drawable) {
            wrap.style.width = width + 'px';
            wrap.style.height = (width * aspect) + 'px';
            drawable.style.width = '100%';
            drawable.style.height = '100%';
            drawable.style.display = 'block';
            wrap.appendChild(drawable);
            var layer = svg('svg', { viewBox: '0 0 ' + VIEW_W + ' ' + (VIEW_W * aspect), preserveAspectRatio: 'none', class: 'position-absolute top-0 start-0 w-100 h-100' });
            layer.style.touchAction = 'none';
            wrap.appendChild(layer);
            var page = { number: number, wrap: wrap, svg: layer, aspect: aspect };
            self.pages.push(page);
            if (self.o.editable) self.attachDrawing(page);
        }

        if (this.image) {
            var img = this.image.cloneNode();
            img.alt = this.o.fileName || t('Submitted image');
            finish(this.image.naturalHeight / this.image.naturalWidth, img);
            return Promise.resolve();
        }
        return this.pdf.getPage(number).then(function (page) {
            var base = page.getViewport({ scale: 1 });
            var scale = width / base.width;
            var viewport = page.getViewport({ scale: scale * (window.devicePixelRatio || 1) });
            var canvas = el('canvas', {});
            canvas.width = Math.floor(viewport.width);
            canvas.height = Math.floor(viewport.height);
            canvas.setAttribute('role', 'img');
            canvas.setAttribute('aria-label', 'Page ' + number + ' of ' + (self.o.fileName || 'the submission'));
            finish(base.height / base.width, canvas);
            return page.render({ canvasContext: canvas.getContext('2d'), viewport: viewport }).promise;
        });
    };

    // ── Drawing ───────────────────────────────────────────────────────────

    SubmissionViewer.prototype.toPage = function (page, evt) {
        var rect = page.svg.getBoundingClientRect();
        return { x: round((evt.clientX - rect.left) / rect.width), y: round((evt.clientY - rect.top) / rect.height) };
    };

    SubmissionViewer.prototype.attachDrawing = function (page) {
        var self = this, start = null, preview = null, points = null;
        page.svg.addEventListener('pointerdown', function (evt) {
            if (self.tool === 'none') return;
            evt.preventDefault();
            var p = self.toPage(page, evt);
            if (self.tool === 'Pin') { self.create(page.number, 'Pin', { x: p.x, y: p.y }, ''); return; }
            if (self.tool === 'Text') { self.askText(page, p); return; }
            page.svg.setPointerCapture(evt.pointerId);
            start = p;
            if (self.tool === 'Freehand') {
                points = [[p.x, p.y]];
                preview = svg('polyline', { fill: 'none', stroke: self.color, 'stroke-width': 4, 'stroke-linecap': 'round', 'stroke-linejoin': 'round' });
            } else {
                preview = svg('rect', { fill: self.color, 'fill-opacity': 0.25, stroke: self.color, 'stroke-dasharray': '6 4' });
            }
            page.svg.appendChild(preview);
        });
        page.svg.addEventListener('pointermove', function (evt) {
            if (!start || !preview) return;
            var p = self.toPage(page, evt);
            var H = VIEW_W * page.aspect;
            if (points) {
                var last = points[points.length - 1];
                if (Math.abs(last[0] - p.x) + Math.abs(last[1] - p.y) > 0.002 && points.length < 2000) points.push([p.x, p.y]);
                preview.setAttribute('points', points.map(function (q) { return q[0] * VIEW_W + ',' + q[1] * H; }).join(' '));
            } else {
                var x = Math.min(start.x, p.x), y = Math.min(start.y, p.y);
                preview.setAttribute('x', x * VIEW_W); preview.setAttribute('y', y * H);
                preview.setAttribute('width', Math.abs(p.x - start.x) * VIEW_W); preview.setAttribute('height', Math.abs(p.y - start.y) * H);
            }
        });
        function finish(evt) {
            if (!start) return;
            var p = self.toPage(page, evt);
            if (preview) preview.remove();
            if (points) {
                if (points.length >= 2) self.create(page.number, 'Freehand', { points: points, width: 0.004 }, '');
            } else {
                var g = { x: Math.min(start.x, p.x), y: Math.min(start.y, p.y), w: round(Math.abs(p.x - start.x)), h: round(Math.abs(p.y - start.y)) };
                if (g.w > 0.005 && g.h > 0.003) self.create(page.number, self.tool, g, '');
            }
            start = null; preview = null; points = null;
        }
        page.svg.addEventListener('pointerup', finish);
        page.svg.addEventListener('pointercancel', function () { if (preview) preview.remove(); start = null; preview = null; points = null; });
    };

    SubmissionViewer.prototype.askText = function (page, p) {
        var self = this;
        var box = el('textarea', { class: 'form-control form-control-sm position-absolute', rows: 2, 'aria-label': t('Annotation text') });
        box.style.left = (p.x * 100) + '%';
        box.style.top = (p.y * 100) + '%';
        box.style.width = '14rem';
        box.style.zIndex = 5;
        page.wrap.appendChild(box);
        box.focus();
        var done = false;
        function commit() {
            if (done) return;
            done = true;
            var text = box.value.trim();
            box.remove();
            if (text) self.create(page.number, 'Text', { x: p.x, y: p.y }, text);
        }
        box.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); commit(); }
            if (e.key === 'Escape') { done = true; box.remove(); }
        });
        box.addEventListener('blur', commit);
    };

    SubmissionViewer.prototype.create = function (pageNumber, type, geometry, comment) {
        var self = this, o = this.o;
        this.status.textContent = t('Saving…');
        postJson(o.urls.add, o.token, { pageNumber: pageNumber, type: type, geometryJson: JSON.stringify(geometry), comment: comment || null, color: this.color })
            .then(function (a) {
                self.annotations.push(a);
                self.selectedId = a.annotationId;
                self.drawAll();
                self.renderList();
                self.status.textContent = t('Saved.');
                if (type === 'Pin' || (type !== 'Text' && !comment)) {
                    var input = self.list.querySelector('[data-comment-for="' + a.annotationId + '"]');
                    if (input && type === 'Pin') input.focus();
                }
                if (o.onChange) o.onChange(self.annotations.length);
            })
            .catch(function (e) { self.status.textContent = e.message; });
    };

    SubmissionViewer.prototype.update = function (a, comment) {
        var self = this, o = this.o;
        return postJson(o.urls.update(a.annotationId), o.token, { pageNumber: a.pageNumber, type: a.type, geometryJson: a.geometryJson, comment: comment, color: a.color })
            .then(function (saved) {
                var i = self.annotations.findIndex(function (x) { return x.annotationId === a.annotationId; });
                if (i >= 0) self.annotations[i] = saved;
                self.drawAll();
                self.status.textContent = t('Saved.');
            })
            .catch(function (e) { self.status.textContent = e.message; });
    };

    SubmissionViewer.prototype.remove = function (a) {
        var self = this, o = this.o;
        return postJson(o.urls.remove(a.annotationId), o.token)
            .then(function () {
                self.annotations = self.annotations.filter(function (x) { return x.annotationId !== a.annotationId; });
                self.drawAll();
                self.renderList();
                if (o.onChange) o.onChange(self.annotations.length);
            })
            .catch(function (e) { self.status.textContent = e.message; });
    };

    // ── Rendering annotations ─────────────────────────────────────────────

    SubmissionViewer.prototype.drawAll = function () {
        var self = this;
        var numbers = this.numbers();
        this.pages.forEach(function (page) {
            Array.prototype.slice.call(page.svg.querySelectorAll('[data-annotation]')).forEach(function (n) { n.remove(); });
            self.annotations.filter(function (a) { return a.pageNumber === page.number; }).forEach(function (a) {
                var node = self.shape(a, page, numbers[a.annotationId]);
                if (node) page.svg.appendChild(node);
            });
        });
    };

    SubmissionViewer.prototype.numbers = function () {
        var map = {}, n = 0;
        this.annotations.slice().sort(function (a, b) { return a.pageNumber - b.pageNumber; }).forEach(function (a) {
            if (a.type !== 'Text') map[a.annotationId] = ++n;
        });
        return map;
    };

    SubmissionViewer.prototype.shape = function (a, page, number) {
        var g;
        try { g = JSON.parse(a.geometryJson); } catch (e) { return null; }
        var H = VIEW_W * page.aspect, color = a.color || '#f5c400';
        var group = svg('g', { 'data-annotation': a.annotationId });
        var selected = a.annotationId === this.selectedId;
        var title = svg('title', {});
        title.textContent = (TYPE_LABELS[a.type] || a.type) + (a.comment ? ': ' + a.comment : '');
        group.appendChild(title);
        var anchorX = 0, anchorY = 0;
        if (a.type === 'Highlight' || a.type === 'Strikeout' || a.type === 'Box') {
            var x = g.x * VIEW_W, y = g.y * H, w = g.w * VIEW_W, h = g.h * H;
            if (a.type === 'Highlight') group.appendChild(svg('rect', { x: x, y: y, width: w, height: h, fill: color, 'fill-opacity': 0.35 }));
            if (a.type === 'Box') group.appendChild(svg('rect', { x: x, y: y, width: w, height: h, fill: 'none', stroke: color, 'stroke-width': 3 }));
            if (a.type === 'Strikeout') group.appendChild(svg('line', { x1: x, y1: y + h / 2, x2: x + w, y2: y + h / 2, stroke: color, 'stroke-width': 3 }));
            anchorX = x + w; anchorY = y;
        } else if (a.type === 'Freehand') {
            group.appendChild(svg('polyline', {
                points: (g.points || []).map(function (p) { return p[0] * VIEW_W + ',' + p[1] * H; }).join(' '),
                fill: 'none', stroke: color, 'stroke-width': Math.max(2, (g.width || 0.004) * VIEW_W), 'stroke-linecap': 'round', 'stroke-linejoin': 'round'
            }));
            anchorX = (g.points && g.points[0] ? g.points[0][0] : 0) * VIEW_W; anchorY = (g.points && g.points[0] ? g.points[0][1] : 0) * H;
        } else if (a.type === 'Text') {
            var text = svg('text', { x: g.x * VIEW_W, y: g.y * H + 18, fill: color, 'font-size': 18, 'font-family': 'sans-serif' });
            (a.comment || '').split('\n').forEach(function (line, i) {
                var span = svg('tspan', { x: g.x * VIEW_W, dy: i === 0 ? 0 : 22 });
                span.textContent = line;
                text.appendChild(span);
            });
            group.appendChild(text);
            return group;
        } else if (a.type === 'Pin') {
            anchorX = g.x * VIEW_W; anchorY = g.y * H;
        }
        if (number && (a.type === 'Pin' || a.comment)) {
            group.appendChild(svg('circle', { cx: anchorX, cy: anchorY, r: 13, fill: '#c81e1e', stroke: selected ? '#000' : '#fff', 'stroke-width': selected ? 3 : 2 }));
            var label = svg('text', { x: anchorX, y: anchorY + 5, 'text-anchor': 'middle', 'font-size': 14, 'font-weight': 'bold', fill: '#fff', 'font-family': 'sans-serif' });
            label.textContent = String(number);
            group.appendChild(label);
        }
        return group;
    };

    SubmissionViewer.prototype.renderList = function () {
        var self = this, o = this.o;
        this.list.textContent = '';
        if (this.annotations.length === 0) {
            if (o.editable) this.list.appendChild(el('div', { class: 'small text-muted' }, t('No annotations yet — pick a tool above and mark the page.')));
            return;
        }
        var numbers = this.numbers();
        var heading = el('div', { class: 'small fw-semibold mb-1' }, t('Annotations (') + this.annotations.length + ')');
        this.list.appendChild(heading);
        this.annotations.slice().sort(function (a, b) { return a.pageNumber - b.pageNumber; }).forEach(function (a) {
            var row = el('div', { class: 'border-bottom py-1 small' + (a.annotationId === self.selectedId ? ' bg-light' : '') });
            var head = el('div', { class: 'd-flex gap-2 align-items-center' });
            row.appendChild(head);
            var label = (numbers[a.annotationId] ? '#' + numbers[a.annotationId] + ' · ' : '') + (TYPE_LABELS[a.type] || a.type) + ' · p. ' + a.pageNumber;
            var go = el('button', { type: 'button', class: 'btn btn-link btn-sm p-0 me-auto text-start' }, label);
            go.addEventListener('click', function () {
                self.selectedId = a.annotationId;
                self.drawAll();
                var page = self.pages.find(function (p) { return p.number === a.pageNumber; });
                if (page) page.wrap.scrollIntoView({ block: 'center', behavior: 'smooth' });
            });
            head.appendChild(go);
            if (o.editable) {
                var del = el('button', { type: 'button', class: 'btn btn-sm btn-link text-danger p-0', 'aria-label': 'Delete ' + label }, 'Delete');
                del.addEventListener('click', function () { self.remove(a); });
                head.appendChild(del);
            }
            if (o.editable && a.type !== 'Text') {
                var input = el('textarea', { class: 'form-control form-control-sm mt-1', rows: 2, 'aria-label': t('Comment for ') + label, 'data-comment-for': a.annotationId, placeholder: t('Add a comment…') });
                input.style.minHeight = '0';
                input.value = a.comment || '';
                input.addEventListener('change', function () { self.update(a, input.value); });
                row.appendChild(input);
            } else if (a.comment) {
                row.appendChild(el('div', { style: 'white-space: pre-wrap;' }, a.comment));
            }
            self.list.appendChild(row);
        });
    };

    SubmissionViewer.prototype.dispose = function () {
        this.disposed = true;
        if (this.pdf) { try { this.pdf.destroy(); } catch (e) { /* ignore */ } }
        this.host.textContent = '';
    };

    window.SubmissionViewer = SubmissionViewer;
    window.SubmissionViewer.postJson = postJson;
})();
