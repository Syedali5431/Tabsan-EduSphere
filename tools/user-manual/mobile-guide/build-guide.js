// Builds the Tabsan EduSphere Mobile App Connection Guide (.docx) in the user manual's house style.
// Text: guide-content.js. Images: shots/phone (adb screencap from the phone), shots/portal (capture-portal.mjs),
// shots/diagram (diagram.js). Word then refreshes the contents page and exports the PDF (../finalize.ps1).
// usage: node build-guide.js guide.docx
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const d = require('docx');
const content = require('./guide-content.js');

const OUT = process.argv[2] || 'guide.docx';
const LOGO_DIR = path.join(__dirname, '../../../src/Tabsan.EduSphere.Web/wwwroot/images');
const APP_RES = path.join(__dirname, '../../../mobile/Tabsan.EduSphere.Mobile/Resources');
const NAVY = '0F2A4A', TEAL = '1590A8', TEAL_DARK = '146C8A', INK = '1F2937', MUTED = '64748B', LINE = 'CBD5E1', SOFT = 'EEF6F9';
const FONT = 'Segoe UI', FONT_HEAD = 'Segoe UI Semibold', MONO = 'Consolas';
const PAGE_W = 11906, MARGIN = 1247; // A4, 2.2 cm margins
const BODY_W = PAGE_W - 2 * MARGIN;   // 9412 DXA
const IMG_W_PX = 560;                // ~5.8 in at 96 dpi
const { VERSION, DATE } = content;

// ── images ───────────────────────────────────────────────────────────────────
function imageFile(ref) {
  const [kind, name] = ref.split(':');
  const file = path.join(__dirname, 'shots', kind, name + '.png');
  if (!fs.existsSync(file)) throw new Error('missing image ' + ref + ' (' + file + ')');
  return file;
}

// GUIDE_DRAFT=1: grey stand-ins for screenshots not taken yet (only to check the layout; never published).
async function placeholder(ref) {
  const [w, h] = ref.startsWith('phone') ? [1080, 2280] : [1600, 1000];
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><rect width="100%" height="100%" fill="#E2E8F0"/><text x="50%" y="50%" font-size="64" text-anchor="middle" fill="#64748B" font-family="Segoe UI">${ref}</text></svg>`;
  return sharp(Buffer.from(svg)).png().toBuffer();
}

async function loadImage(ref, maxRatio = 0.95, width = 1240) {
  const draft = process.env.GUIDE_DRAFT === '1' && !fs.existsSync(path.join(__dirname, 'shots', ref.split(':')[0], ref.split(':')[1] + '.png'));
  const file = draft ? await placeholder(ref) : imageFile(ref);
  const meta = await sharp(file).metadata();
  const h = Math.min(meta.height, Math.round(meta.width * maxRatio));
  const buf = await sharp(file).extract({ left: 0, top: 0, width: meta.width, height: h })
    .resize({ width: Math.min(width, meta.width) }).flatten({ background: '#FFFFFF' }).jpeg({ quality: 80, mozjpeg: true }).toBuffer();
  const m2 = await sharp(buf).metadata();
  return { buf, w: m2.width, h: m2.height };
}

// ── text helpers ─────────────────────────────────────────────────────────────
function runs(text, base = {}) {
  // **bold** and `code` markup
  const out = [];
  for (const p of String(text).split(/(\*\*[^*]+\*\*|`[^`]+`)/g).filter(Boolean)) {
    if (p.startsWith('**')) out.push(new d.TextRun({ text: p.slice(2, -2), bold: true, color: NAVY, ...base }));
    else if (p.startsWith('`')) out.push(new d.TextRun({ text: p.slice(1, -1), font: MONO, color: TEAL_DARK, ...base, size: (base.size || 20) - 1 }));
    else out.push(new d.TextRun({ text: p, ...base }));
  }
  return out;
}
const para = (text, opts = {}) => new d.Paragraph({ children: runs(text, opts.run || {}), spacing: { after: 120, line: 300 }, ...opts.p });
const gap = (after = 160) => new d.Paragraph({ spacing: { after }, children: [] });

let numberingCount = 0;
const numberingConfigs = [];
function newNumbering() {
  const ref = `steps-${++numberingCount}`;
  numberingConfigs.push({
    reference: ref,
    levels: [{ level: 0, format: d.LevelFormat.DECIMAL, text: '%1.', alignment: d.AlignmentType.LEFT,
      style: { paragraph: { indent: { left: 460, hanging: 360 } }, run: { bold: true, color: TEAL_DARK } } }],
  });
  return ref;
}

const noBorder = { style: d.BorderStyle.NONE, size: 0, color: 'FFFFFF' };
const noBorders = { top: noBorder, bottom: noBorder, left: noBorder, right: noBorder };

function callout(kind, text) {
  const map = { note: ['NOTE', TEAL, 'EAF6F9'], tip: ['TIP', '15803D', 'ECFDF3'], important: ['IMPORTANT', 'B45309', 'FFF7ED'], warning: ['WARNING', 'B91C1C', 'FEF2F2'] };
  const [label, color, fill] = map[kind] || map.note;
  return new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: [BODY_W],
    rows: [new d.TableRow({ cantSplit: true, children: [new d.TableCell({
      width: { size: BODY_W, type: d.WidthType.DXA },
      shading: { type: d.ShadingType.CLEAR, color: 'auto', fill },
      margins: { top: 110, bottom: 110, left: 200, right: 200 },
      borders: { ...noBorders, left: { style: d.BorderStyle.SINGLE, size: 24, color } },
      children: [new d.Paragraph({ spacing: { after: 0, line: 290 }, children: [new d.TextRun({ text: label + '   ', bold: true, color, size: 18 }), ...runs(text, { size: 19 })] })],
    })] })],
  });
}

/** Monospace block (settings, commands, requests). */
function code(lines) {
  return new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: [BODY_W],
    rows: [new d.TableRow({ cantSplit: true, children: [new d.TableCell({
      width: { size: BODY_W, type: d.WidthType.DXA },
      shading: { type: d.ShadingType.CLEAR, color: 'auto', fill: 'F4F6F9' },
      margins: { top: 120, bottom: 120, left: 200, right: 200 },
      borders: { top: { style: d.BorderStyle.SINGLE, size: 4, color: LINE }, bottom: { style: d.BorderStyle.SINGLE, size: 4, color: LINE }, left: { style: d.BorderStyle.SINGLE, size: 4, color: LINE }, right: { style: d.BorderStyle.SINGLE, size: 4, color: LINE } },
      children: lines.map(l => new d.Paragraph({ spacing: { after: 0, line: 250 }, children: [new d.TextRun({ text: l.replace(/ /g, ' '), font: MONO, size: 17, color: NAVY })] })),
    })] })],
  });
}

function dataTable(head, rows, widths) {
  const total = widths.reduce((a, b) => a + b, 0);
  const w = widths.map(x => Math.round(x * BODY_W / total));
  w[w.length - 1] += BODY_W - w.reduce((a, b) => a + b, 0);
  const border = { style: d.BorderStyle.SINGLE, size: 4, color: LINE };
  const borders = { top: border, bottom: border, left: border, right: border };
  const cell = (text, i, hdr, zebra) => new d.TableCell({
    width: { size: w[i], type: d.WidthType.DXA }, borders,
    shading: hdr ? { type: d.ShadingType.CLEAR, color: 'auto', fill: NAVY } : zebra ? { type: d.ShadingType.CLEAR, color: 'auto', fill: 'F5F8FB' } : undefined,
    margins: { top: 70, bottom: 70, left: 110, right: 110 },
    verticalAlign: d.VerticalAlign.CENTER,
    children: [new d.Paragraph({ spacing: { after: 0, line: 270 },
      children: hdr ? [new d.TextRun({ text, bold: true, color: 'FFFFFF', size: 18 })] : runs(text, { size: 18 }) })],
  });
  return new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: w,
    rows: [new d.TableRow({ tableHeader: true, children: head.map((h, i) => cell(h, i, true)) }),
      ...rows.map((r, ri) => new d.TableRow({ cantSplit: true, children: r.map((c, i) => cell(String(c), i, false, ri % 2 === 1)) }))],
  });
}

const caption = (label, text) => new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { before: 80, after: 240 },
  children: [new d.TextRun({ text: label + '  ', bold: true, color: TEAL_DARK, size: 17 }), new d.TextRun({ text, italics: true, color: MUTED, size: 17 })] });

/** A wide picture (portal page, diagram). */
async function figure(ref, text, label) {
  const { buf, w, h } = await loadImage(ref, 0.95);
  const height = Math.round(IMG_W_PX * h / w);
  const border = { style: d.BorderStyle.SINGLE, size: 4, color: LINE };
  return [
    new d.Paragraph({ alignment: d.AlignmentType.CENTER, keepNext: true, spacing: { before: 120, after: 0 },
      border: ref.startsWith('diagram') ? undefined : { top: border, bottom: border, left: border, right: border },
      children: [new d.ImageRun({ type: 'jpg', data: buf, transformation: { width: IMG_W_PX, height }, altText: { title: text, description: text, name: label } })] }),
    caption(label, text),
  ];
}

/** One to three phone screens side by side, each with its own short title. */
async function phones(items, text, label) {
  const each = items.length === 1 ? 215 : items.length === 2 ? 200 : 165;
  const colW = Math.floor(BODY_W / items.length);
  const cells = [];
  for (const [ref, title] of items) {
    const { buf, w, h } = await loadImage(ref, 2.4, 720);
    const height = Math.round(each * h / w);
    cells.push(new d.TableCell({
      width: { size: colW, type: d.WidthType.DXA }, borders: noBorders, margins: { top: 40, bottom: 40, left: 60, right: 60 },
      children: [
        new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { after: 60 }, children: [new d.ImageRun({ type: 'jpg', data: buf, transformation: { width: each, height },
          altText: { title: title || text, description: title || text, name: ref } })] }),
        ...(title ? [new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { after: 0 }, children: [new d.TextRun({ text: title, bold: true, color: NAVY, size: 17 })] })] : []),
      ],
    }));
  }
  return [
    new d.Table({ width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: cells.map(() => colW), alignment: d.AlignmentType.CENTER,
      rows: [new d.TableRow({ cantSplit: true, children: cells })] }),
    caption(label, text),
  ];
}

// ── body ─────────────────────────────────────────────────────────────────────
async function buildChapters() {
  const children = [];
  let ch = 0;
  for (const chapter of content.chapters) {
    ch++;
    let fig = 0;
    children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true,
      children: [new d.TextRun({ text: `${ch}   ` }), new d.TextRun(chapter.chapter)] }));
    for (const t of chapter.intro || []) children.push(para(t));
    let sec = 0;
    for (const s of chapter.sections) {
      sec++;
      children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_2, keepNext: true, children: [new d.TextRun(`${ch}.${sec}   ${s.title}`)] }));
      if (s.who) children.push(new d.Paragraph({
        keepNext: true, spacing: { after: 160 }, shading: { type: d.ShadingType.CLEAR, color: 'auto', fill: SOFT },
        border: { left: { style: d.BorderStyle.SINGLE, size: 18, color: TEAL, space: 6 } },
        children: [new d.TextRun({ text: 'Who does this: ', bold: true, color: TEAL_DARK, size: 18 }), new d.TextRun({ text: s.who, color: INK, size: 18 })] }));
      const h3 = (t) => children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_3, keepNext: true, children: [new d.TextRun(t)] }));
      const bullets = (items) => { for (const b of items) children.push(new d.Paragraph({ bullet: { level: 0 }, spacing: { after: 80, line: 290 }, children: runs(b) })); };
      for (const block of s.body || []) {
        if (typeof block === 'string') { children.push(para(block)); continue; }
        if (block.h3) h3(block.h3);
        if (block.text) for (const t of [].concat(block.text)) children.push(para(t));
        if (block.bullets) bullets(block.bullets);
        if (block.steps) {
          const ref = newNumbering();
          for (const st of block.steps) children.push(new d.Paragraph({ numbering: { reference: ref, level: 0 }, spacing: { after: 90, line: 290 }, children: runs(st) }));
          children.push(gap(100));
        }
        if (block.table) { children.push(dataTable(block.table.head, block.table.rows, block.table.widths)); children.push(gap()); }
        if (block.code) { children.push(code(block.code)); children.push(gap()); }
        if (block.fig) { fig++; children.push(...await figure(block.fig, block.caption, `Figure ${ch}.${fig}`)); }
        if (block.phones) { fig++; children.push(...await phones(block.phones, block.caption, `Figure ${ch}.${fig}`)); }
        if (block.note) { children.push(callout(block.note[0], block.note[1])); children.push(gap()); }
      }
    }
  }
  return children;
}

// ── document ─────────────────────────────────────────────────────────────────
(async () => {
  const body = await buildChapters();
  const logoFull = fs.readFileSync(LOGO_DIR + '/tabsan-logo.png');
  const logoFullMeta = await sharp(logoFull).metadata();
  const logoMark = await sharp(fs.readFileSync(LOGO_DIR + '/tabsan-mark.png')).resize({ width: 96 }).png().toBuffer();
  // the app icon as phones show it: the mark on the navy background, rounded
  const bg = await sharp(path.join(APP_RES, 'AppIcon/appicon.svg')).resize(512, 512).png().toBuffer();
  const icon = await sharp(bg).composite([{ input: path.join(APP_RES, 'AppIcon/appiconfg.png') },
    { input: Buffer.from('<svg width="512" height="512"><rect width="512" height="512" rx="112" fill="#fff"/></svg>'), blend: 'dest-in' }]).png().toBuffer();
  const cover1 = await loadImage(content.cover[0], 2.4, 720);
  const cover2 = await loadImage(content.cover[1], 2.4, 720);

  const band = (children, padTop, padBottom) => new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: [BODY_W],
    rows: [new d.TableRow({ children: [new d.TableCell({
      width: { size: BODY_W, type: d.WidthType.DXA },
      shading: { type: d.ShadingType.CLEAR, color: 'auto', fill: NAVY },
      margins: { top: padTop, bottom: padBottom, left: 400, right: 400 },
      borders: { ...noBorders, bottom: { style: d.BorderStyle.SINGLE, size: 24, color: TEAL } },
      children,
    })] })],
  });
  const coverPhone = (img) => new d.TableCell({ width: { size: Math.floor(BODY_W / 3), type: d.WidthType.DXA }, borders: noBorders, verticalAlign: d.VerticalAlign.CENTER,
    children: [new d.Paragraph({ alignment: d.AlignmentType.CENTER, children: [new d.ImageRun({ type: 'jpg', data: img.buf, transformation: { width: 170, height: Math.round(170 * img.h / img.w) }, altText: { title: 'App screen', description: 'A screen of the mobile app', name: 'cover-phone' } })] })] });
  const cover = [
    band([new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { after: 0 }, children: [new d.ImageRun({ type: 'png', data: logoFull, transformation: { width: 400, height: Math.round(400 * logoFullMeta.height / logoFullMeta.width) }, altText: { title: 'Tabsan logo', description: 'Tabsan — Building Next Era of Software', name: 'logo' } })] })], 460, 460),
    new d.Paragraph({ spacing: { before: 440, after: 60 }, children: [new d.TextRun({ text: 'TABSAN EDUSPHERE', font: FONT_HEAD, size: 26, color: TEAL, characterSpacing: 60 })] }),
    new d.Paragraph({ spacing: { after: 120 }, children: [new d.TextRun({ text: 'Mobile App Connection Guide', font: FONT_HEAD, size: 64, color: NAVY })] }),
    new d.Paragraph({ spacing: { after: 300 }, border: { bottom: { style: d.BorderStyle.SINGLE, size: 18, color: TEAL, space: 12 } },
      children: [new d.TextRun({ text: 'Connecting the EduSphere app to your institute\'s server — set-up, accounts, sign-in and daily use', size: 28, color: MUTED })] }),
    new d.Table({ width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: [Math.floor(BODY_W / 3), Math.floor(BODY_W / 3), BODY_W - 2 * Math.floor(BODY_W / 3)], alignment: d.AlignmentType.CENTER,
      rows: [new d.TableRow({ children: [coverPhone(cover1),
        new d.TableCell({ width: { size: Math.floor(BODY_W / 3), type: d.WidthType.DXA }, borders: noBorders, verticalAlign: d.VerticalAlign.CENTER, children: [
          new d.Paragraph({ alignment: d.AlignmentType.CENTER, children: [new d.ImageRun({ type: 'png', data: icon, transformation: { width: 96, height: 96 }, altText: { title: 'App icon', description: 'The EduSphere app icon', name: 'icon' } })] }),
          new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { before: 80 }, children: [new d.TextRun({ text: 'EduSphere', bold: true, color: NAVY, size: 22 })] }),
          new d.Paragraph({ alignment: d.AlignmentType.CENTER, children: [new d.TextRun({ text: 'for Android and iOS', color: MUTED, size: 18 })] })] }),
        coverPhone(cover2)] })] }),
    new d.Paragraph({ spacing: { before: 300, after: 60 }, children: [new d.TextRun({ text: `Version ${VERSION}`, bold: true, color: NAVY, size: 22 }), new d.TextRun({ text: `   ·   ${DATE}`, color: MUTED, size: 22 })] }),
    new d.Paragraph({ children: [new d.TextRun({ text: 'Tabsan Nexora — Building the Next Era of Software', color: MUTED, size: 20 })] }),
  ];

  const docInfo = [
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('About This Guide')] }),
    ...content.about.map(t => para(t)),
    dataTable(['Item', 'Detail'], content.aboutTable, [2400, 6900]),
    gap(200),
    callout('note', content.aboutNote),
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('Contents')] }),
    new d.TableOfContents('Contents', { hyperlink: true, headingStyleRange: '1-2' }),
  ];

  const appendix = [];
  for (const a of content.appendices) {
    appendix.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun(a.title)] }));
    for (const block of a.body) {
      if (typeof block === 'string') appendix.push(para(block));
      else if (block.h3) appendix.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_3, keepNext: true, children: [new d.TextRun(block.h3)] }));
      else if (block.table) { appendix.push(dataTable(block.table.head, block.table.rows, block.table.widths)); appendix.push(gap()); }
      else if (block.code) { appendix.push(code(block.code)); appendix.push(gap()); }
    }
  }

  const header = new d.Header({ children: [new d.Paragraph({
    border: { bottom: { style: d.BorderStyle.SINGLE, size: 4, color: LINE, space: 4 } },
    tabStops: [{ type: d.TabStopType.RIGHT, position: BODY_W }],
    children: [new d.ImageRun({ type: 'png', data: logoMark, transformation: { width: 18, height: 18 }, altText: { title: 'Tabsan emblem', description: 'Tabsan emblem', name: 'header-mark' } }),
      new d.TextRun({ text: '  Tabsan EduSphere', bold: true, color: NAVY, size: 16 }), new d.TextRun({ text: `\tMobile App Connection Guide · Version ${VERSION}`, color: MUTED, size: 16 })] })] });
  const footer = new d.Footer({ children: [new d.Paragraph({
    border: { top: { style: d.BorderStyle.SINGLE, size: 4, color: LINE, space: 4 } },
    tabStops: [{ type: d.TabStopType.RIGHT, position: BODY_W }],
    children: [new d.TextRun({ text: '© 2026 Tabsan Nexora. All rights reserved.', color: MUTED, size: 16 }),
      new d.TextRun({ children: ['\tPage ', d.PageNumber.CURRENT, ' of ', d.PageNumber.TOTAL_PAGES], color: MUTED, size: 16 })] })] });

  const doc = new d.Document({
    creator: 'Tabsan Nexora', title: 'Tabsan EduSphere Mobile App Connection Guide', description: 'How to connect the EduSphere mobile app to your institute\'s EduSphere server',
    numbering: { config: numberingConfigs },
    styles: {
      default: { document: { run: { font: FONT, size: 20, color: INK } } },
      paragraphStyles: [
        { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { font: FONT_HEAD, size: 40, color: NAVY },
          paragraph: { spacing: { before: 0, after: 280 }, outlineLevel: 0, border: { bottom: { style: d.BorderStyle.SINGLE, size: 12, color: TEAL, space: 8 } } } },
        { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { font: FONT_HEAD, size: 28, color: TEAL_DARK }, paragraph: { spacing: { before: 360, after: 120 }, outlineLevel: 1 } },
        { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { font: FONT_HEAD, size: 21, color: NAVY }, paragraph: { spacing: { before: 160, after: 80 }, outlineLevel: 2 } },
        { id: 'TOC1', name: 'toc 1', basedOn: 'Normal', run: { font: FONT_HEAD, size: 21, color: NAVY }, paragraph: { spacing: { before: 140, after: 40 } } },
        { id: 'TOC2', name: 'toc 2', basedOn: 'Normal', run: { size: 19 }, paragraph: { spacing: { after: 20 }, indent: { left: 360 } } },
      ],
    },
    sections: [
      { properties: { page: { size: { width: PAGE_W, height: 16838 }, margin: { top: 1100, bottom: 1000, left: MARGIN, right: MARGIN } } }, children: cover },
      // Numbering carries on from the (unnumbered) cover, so "Page n of N" counts the same pages as N. Restarting at 1
      // made the total one too many (and LibreOffice adds a blank page to keep page 1 on a right-hand side).
      { properties: { page: { size: { width: PAGE_W, height: 16838 }, margin: { top: 1300, bottom: 1200, left: MARGIN, right: MARGIN, header: 600, footer: 600 } } },
        headers: { default: header }, footers: { default: footer }, children: [...docInfo, ...body, ...appendix] },
    ],
  });
  const packed = await d.Packer.toBuffer(doc);
  // docx-js gives every drawing docPr/cNvPr id 1; Word treats duplicates as damage. Renumber them.
  const zip = await require('jszip').loadAsync(packed);
  let n = 0, m = 0;
  for (const part of Object.keys(zip.files).filter(f => /^word\/(document|header\d*|footer\d*)\.xml$/.test(f))) {
    let xml = await zip.file(part).async('string');
    xml = xml.replace(/<wp:docPr id="[0-9]+"/g, () => `<wp:docPr id="${++n}"`).replace(/<pic:cNvPr id="[0-9]+"/g, () => `<pic:cNvPr id="${1000 + ++m}"`);
    zip.file(part, xml);
  }
  const buf = await zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' });
  fs.writeFileSync(OUT, buf);
  console.log('wrote', OUT, (buf.length / 1048576).toFixed(1), 'MB; procedures', numberingCount);
})().catch(e => { console.error(e); process.exit(1); });
