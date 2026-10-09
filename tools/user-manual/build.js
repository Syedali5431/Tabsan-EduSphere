// Builds the Tabsan EduSphere user manual (.docx). Word COM then refreshes TOC/index and exports PDF.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const d = require('docx');
const content = require('./content.js');

const OUT = process.argv[2] || 'manual.docx';
const NAVY = '0F2A4A', TEAL = '1590A8', TEAL_DARK = '146C8A', INK = '1F2937', MUTED = '64748B', LINE = 'CBD5E1', SOFT = 'EEF6F9';
const FONT = 'Segoe UI', FONT_HEAD = 'Segoe UI Semibold';
const PAGE_W = 11906, MARGIN = 1247; // A4, 2.2 cm margins
const BODY_W = PAGE_W - 2 * MARGIN;   // 9412 DXA
const IMG_W_PX = 610;                // ~6.35 in at 96 dpi

// ── image lookup ──────────────────────────────────────────────────────────────
function resolveImage(ref) {
  const [kind, rest] = ref.split(':');
  if (kind === 'flow') {
    const [name, n] = rest.split('/');
    const dir = path.join('flows', name);
    const f = fs.readdirSync(dir).find(x => x.startsWith(String(n).padStart(2, '0') + '-'));
    if (!f) throw new Error('missing flow image ' + ref);
    return path.join(dir, f);
  }
  const dir = path.join('out', kind);
  const files = fs.readdirSync(dir);
  const f = files.find(x => x === rest + '.png') || files.find(x => new RegExp(`^\\d+-${rest}\\.png$`).test(x));
  if (!f) throw new Error('missing page image ' + ref);
  return path.join(dir, f);
}

async function loadImage(file, maxH = 1100) {
  const img = sharp(file);
  const meta = await img.metadata();
  // crop very tall full-page captures so a figure never exceeds about 2/3 of a page
  const buf = await sharp(file).extract({ left: 0, top: 0, width: meta.width, height: Math.min(meta.height, Math.round(meta.width * 0.95)) })
    .resize({ width: 1240 }).jpeg({ quality: 76, mozjpeg: true }).toBuffer();
  const m2 = await sharp(buf).metadata();
  return { buf, w: m2.width, h: m2.height };
}

// ── text helpers ─────────────────────────────────────────────────────────────
function runs(text, base = {}) {
  // **bold** markup
  const out = [];
  const parts = String(text).split(/(\*\*[^*]+\*\*)/g).filter(Boolean);
  for (const p of parts) {
    if (p.startsWith('**')) out.push(new d.TextRun({ text: p.slice(2, -2), bold: true, color: NAVY, ...base }));
    else out.push(new d.TextRun({ text: p, ...base }));
  }
  return out;
}
const para = (text, opts = {}) => new d.Paragraph({ children: runs(text, opts.run || {}), spacing: { after: 120, line: 300 }, ...opts.p });

const indexTerms = [];
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

function callout(kind, text) {
  const map = { note: ['NOTE', TEAL, 'EAF6F9'], tip: ['TIP', '15803D', 'ECFDF3'], important: ['IMPORTANT', 'B45309', 'FFF7ED'] };
  const [label, color, fill] = map[kind] || map.note;
  return new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: [BODY_W],
    rows: [new d.TableRow({ children: [new d.TableCell({
      width: { size: BODY_W, type: d.WidthType.DXA },
      shading: { type: d.ShadingType.CLEAR, color: 'auto', fill },
      margins: { top: 110, bottom: 110, left: 200, right: 200 },
      borders: { left: { style: d.BorderStyle.SINGLE, size: 24, color }, top: { style: d.BorderStyle.NONE, size: 0, color: 'FFFFFF' }, bottom: { style: d.BorderStyle.NONE, size: 0, color: 'FFFFFF' }, right: { style: d.BorderStyle.NONE, size: 0, color: 'FFFFFF' } },
      children: [new d.Paragraph({ spacing: { after: 0, line: 290 }, children: [new d.TextRun({ text: label + '   ', bold: true, color, size: 18 }), ...runs(text, { size: 19 })] })],
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
    children: [new d.Paragraph({ spacing: { after: 0, line: 270 }, alignment: hdr || text.length > 2 ? d.AlignmentType.LEFT : d.AlignmentType.CENTER,
      children: hdr ? [new d.TextRun({ text, bold: true, color: 'FFFFFF', size: 18 })] : runs(text, { size: 18 }) })],
  });
  return new d.Table({
    width: { size: BODY_W, type: d.WidthType.DXA }, columnWidths: w,
    rows: [new d.TableRow({ tableHeader: true, children: head.map((h, i) => cell(h, i, true)) }),
      ...rows.map((r, ri) => new d.TableRow({ cantSplit: true, children: r.map((c, i) => cell(String(c), i, false, ri % 2 === 1)) }))],
  });
}

async function figure(ref, caption, label) {
  const { buf, w, h } = await loadImage(resolveImage(ref));
  const width = IMG_W_PX, height = Math.round(IMG_W_PX * h / w);
  const border = { style: d.BorderStyle.SINGLE, size: 4, color: LINE };
  return [
    new d.Paragraph({ alignment: d.AlignmentType.CENTER, keepNext: true, spacing: { before: 120, after: 0 },
      border: { top: border, bottom: border, left: border, right: border },
      children: [new d.ImageRun({ type: 'jpg', data: buf, transformation: { width, height },
        altText: { title: caption, description: caption, name: label } })] }),
    new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { before: 80, after: 240 },
      children: [new d.TextRun({ text: label + '  ', bold: true, color: TEAL_DARK, size: 17 }), new d.TextRun({ text: caption, italics: true, color: MUTED, size: 17 })] }),
  ];
}

function rolesLine(roles) {
  return new d.Paragraph({
    spacing: { after: 160 }, shading: { type: d.ShadingType.CLEAR, color: 'auto', fill: SOFT },
    border: { left: { style: d.BorderStyle.SINGLE, size: 18, color: TEAL, space: 6 } },
    children: [new d.TextRun({ text: 'Available to: ', bold: true, color: TEAL_DARK, size: 18 }), new d.TextRun({ text: roles, color: INK, size: 18 })],
  });
}

// ── build body ────────────────────────────────────────────────────────────────
async function buildChapters() {
  const children = [];
  let ch = 0;
  for (const chapter of content) {
    ch++;
    let fig = 0;
    children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true,
      children: [new d.TextRun({ text: `${ch}   ` }), new d.TextRun(chapter.chapter)] }));
    for (const t of chapter.intro || []) children.push(para(t));
    let sec = 0;
    for (const s of chapter.sections) {
      sec++;
      children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_2, keepNext: true,
        children: [new d.TextRun(`${ch}.${sec}   ${s.title}`)] }));
      if (s.index) indexTerms.push({ heading: `${ch}.${sec}   ${s.title}`, terms: s.index });
      if (s.roles) children.push(rolesLine(s.roles));
      for (const t of s.intro || []) children.push(para(t));
      if (s.bullets) for (const b of s.bullets) children.push(new d.Paragraph({ bullet: { level: 0 }, spacing: { after: 80, line: 290 }, children: runs(b) }));
      if (s.table) { children.push(dataTable(s.table.head, s.table.rows, s.table.widths)); children.push(new d.Paragraph({ spacing: { after: 160 }, children: [] })); }
      if (s.steps) {
        children.push(new d.Paragraph({ heading: d.HeadingLevel.HEADING_3, keepNext: true, children: [new d.TextRun('Procedure')] }));
        const ref = newNumbering();
        for (const st of s.steps) children.push(new d.Paragraph({ numbering: { reference: ref, level: 0 }, spacing: { after: 90, line: 290 }, children: runs(st) }));
        children.push(new d.Paragraph({ spacing: { after: 100 }, children: [] }));
      }
      for (const [ref, cap] of s.figs || []) { fig++; children.push(...await figure(ref, cap, `Figure ${ch}.${fig}`)); }
      for (const [kind, text] of s.notes || []) { children.push(callout(kind, text)); children.push(new d.Paragraph({ spacing: { after: 160 }, children: [] })); }
    }
  }
  return { children, chapters: ch };
}

function accessMatrix() {
  const all = {};
  for (const r of ['superadmin', 'testadmin', 'faculty', 'student', 'finance']) {
    const m = JSON.parse(fs.readFileSync(`out/${r}/meta.json`, 'utf8'));
    all[r] = new Map(m.sidebar.map(s => [s.action, s]));
  }
  const rows = [];
  for (const [action, item] of all.superadmin) {
    const yes = (r) => (all[r].has(action) ? '✓' : '–');
    rows.push([item.group ? item.group.replace(/\b\w/g, c => c.toUpperCase()).replace(/\B\w+/g, w => w.toLowerCase()) : '', item.text, '✓', yes('testadmin'), yes('faculty'), yes('student'), yes('finance')]);
  }
  // student-only items not in the SuperAdmin menu
  for (const [action, item] of all.student) if (!all.superadmin.has(action)) rows.push(['Student Related', item.text, '–', '–', '–', '✓', '–']);
  return rows;
}

const glossary = [
  ['CGPA', 'Cumulative Grade Point Average across all completed semesters.'],
  ['Course offering', 'A course taught in a specific semester or class, usually by one faculty member. Students enrol in offerings.'],
  ['Degree rule', 'The credit and GPA requirements a University program must meet before a student can graduate.'],
  ['FYP', 'Final Year Project – the capstone project of a University program.'],
  ['Institution type', 'University, College or School. Departments, grading and progression follow the institution type.'],
  ['Level', 'A semester (University) or class (School and College) within a program.'],
  ['License (.tablic)', 'The encrypted, signed file that activates the portal and sets its expiry date.'],
  ['Module', 'A feature area (for example Attendance or Reports) that SuperAdmin can activate or deactivate.'],
  ['Prerequisite', 'A course a student must pass before enrolling in another course.'],
  ['Read-only mode', 'The state in which the portal allows viewing but blocks changes, used when the license is not valid.'],
  ['Tenant', 'A separate institution in the installation with its own campuses, departments, users and data.'],
  ['Two-factor authentication (2FA)', 'A second sign-in step using a 6-digit code from an authenticator app.'],
];

// ── document ─────────────────────────────────────────────────────────────────
(async () => {
  const { children: body } = await buildChapters();
  const logo = fs.readFileSync(require('path').join(__dirname, '../../src/Tabsan.EduSphere.API/wwwroot/branding/tabsan-logo.png'));
  const logoMeta = await sharp(logo).metadata();
  const hero = await loadImage(resolveImage('testadmin:Departments'));
  const today = '9 October 2026';

  const cover = [
    new d.Paragraph({ spacing: { before: 600, after: 300 }, children: [new d.ImageRun({ type: 'png', data: logo, transformation: { width: 150, height: Math.round(150 * logoMeta.height / logoMeta.width) }, altText: { title: 'Tabsan logo', description: 'Tabsan logo', name: 'logo' } })] }),
    new d.Paragraph({ spacing: { after: 60 }, children: [new d.TextRun({ text: 'TABSAN EDUSPHERE', font: FONT_HEAD, size: 26, color: TEAL, characterSpacing: 60 })] }),
    new d.Paragraph({ spacing: { after: 120 }, children: [new d.TextRun({ text: 'User Manual', font: FONT_HEAD, size: 80, color: NAVY })] }),
    new d.Paragraph({ spacing: { after: 480 }, border: { bottom: { style: d.BorderStyle.SINGLE, size: 18, color: TEAL, space: 12 } },
      children: [new d.TextRun({ text: 'Campus Portal for Universities, Colleges and Schools', size: 30, color: MUTED })] }),
    new d.Paragraph({ alignment: d.AlignmentType.CENTER, spacing: { after: 480 }, children: [new d.ImageRun({ type: 'jpg', data: hero.buf, transformation: { width: 560, height: Math.round(560 * hero.h / hero.w) }, altText: { title: 'Portal screenshot', description: 'The Departments page of the portal', name: 'hero' } })] }),
    new d.Paragraph({ spacing: { after: 60 }, children: [new d.TextRun({ text: 'Version 1.0', bold: true, color: NAVY, size: 22 }), new d.TextRun({ text: `   ·   ${today}`, color: MUTED, size: 22 })] }),
    new d.Paragraph({ children: [new d.TextRun({ text: 'Tabsan Nexora — Building the Next Era of Software', color: MUTED, size: 20 })] }),
  ];

  const docInfo = [
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('About This Manual')] }),
    para('This manual describes Tabsan EduSphere as installed from the main branch on 9 October 2026. Screenshots were captured from the live application using the demonstration data set.'),
    dataTable(['Item', 'Detail'], [
      ['Product', 'Tabsan EduSphere Campus Portal'],
      ['Document', 'User Manual'],
      ['Version', '1.0'],
      ['Date', today],
      ['Audience', 'SuperAdmin, Admin, Faculty, Student and Finance users'],
      ['Publisher', 'Tabsan Nexora'],
    ], [2400, 6900]),
    new d.Paragraph({ spacing: { after: 200 }, children: [] }),
    callout('note', 'Screens may differ slightly from these screenshots depending on your role, your institution type, the theme you choose and which modules your administrator has enabled.'),
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('Contents')] }),
    new d.TableOfContents('Contents', { hyperlink: true, headingStyleRange: '1-2' }),
  ];

  const appendix = [
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('Appendix A   Menu Access by Role')] }),
    para('The table lists every sidebar menu and the roles that see it with the default settings. SuperAdmin can change Admin, Faculty and Student visibility in Sidebar Settings. Admin access was captured with the testadmin account.'),
    dataTable(['Section', 'Menu', 'Super Admin', 'Admin', 'Faculty', 'Student', 'Finance'], accessMatrix(), [1900, 2700, 950, 900, 900, 900, 900]),
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('Appendix B   Glossary')] }),
    dataTable(['Term', 'Meaning'], glossary, [2600, 6700]),
    new d.Paragraph({ heading: d.HeadingLevel.HEADING_1, pageBreakBefore: true, children: [new d.TextRun('Index')] }),
    new d.Paragraph({ children: [new d.TextRun('INDEX_PLACEHOLDER')] }),
  ];

  const header = new d.Header({ children: [new d.Paragraph({
    border: { bottom: { style: d.BorderStyle.SINGLE, size: 4, color: LINE, space: 4 } },
    tabStops: [{ type: d.TabStopType.RIGHT, position: BODY_W }],
    children: [new d.TextRun({ text: 'Tabsan EduSphere', bold: true, color: NAVY, size: 16 }), new d.TextRun({ text: '\tUser Manual · Version 1.0', color: MUTED, size: 16 })] })] });
  const footer = new d.Footer({ children: [new d.Paragraph({
    border: { top: { style: d.BorderStyle.SINGLE, size: 4, color: LINE, space: 4 } },
    tabStops: [{ type: d.TabStopType.RIGHT, position: BODY_W }],
    children: [new d.TextRun({ text: '© 2026 Tabsan Nexora. All rights reserved.', color: MUTED, size: 16 }),
      new d.TextRun({ children: ['\tPage ', d.PageNumber.CURRENT, ' of ', d.PageNumber.TOTAL_PAGES], color: MUTED, size: 16 })] })] });

  const finalDoc = new d.Document({
    creator: 'Tabsan Nexora', title: 'Tabsan EduSphere User Manual', description: 'User manual for the Tabsan EduSphere campus portal',
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
        { id: 'Index1', name: 'index 1', basedOn: 'Normal', run: { size: 18 }, paragraph: { spacing: { after: 0 } } },
        { id: 'IndexHeading', name: 'index heading', basedOn: 'Normal', run: { font: FONT_HEAD, size: 22, color: TEAL_DARK }, paragraph: { spacing: { before: 160, after: 40 } } },
      ],
    },
    sections: [
      { properties: { page: { size: { width: PAGE_W, height: 16838 }, margin: { top: 1300, bottom: 1200, left: MARGIN, right: MARGIN } } }, children: cover },
      { properties: { page: { size: { width: PAGE_W, height: 16838 }, margin: { top: 1300, bottom: 1200, left: MARGIN, right: MARGIN, header: 600, footer: 600 }, pageNumbers: { start: 1 } } },
        headers: { default: header }, footers: { default: footer }, children: [...docInfo, ...body, ...appendix] },
    ],
  });
  const packed = await d.Packer.toBuffer(finalDoc);
  // docx-js gives every drawing docPr/cNvPr id 1; Word treats duplicates as damage. Renumber them.
  const zip = await require('jszip').loadAsync(packed);
  let xml = await zip.file('word/document.xml').async('string');
  let n = 0;
  xml = xml.replace(/<wp:docPr id="[0-9]+"/g, () => `<wp:docPr id="${++n}"`);
  let m = 0;
  xml = xml.replace(/<pic:cNvPr id="[0-9]+"/g, () => `<pic:cNvPr id="${1000 + ++m}"`);
  zip.file('word/document.xml', xml);
  const buf = await zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' });
  fs.writeFileSync(OUT, buf);
  indexTerms.push({ heading: 'Appendix A   Menu Access by Role', terms: ['Access matrix'] });
  fs.writeFileSync('index-terms.json', JSON.stringify(indexTerms, null, 1));
  console.log('wrote', OUT, (buf.length / 1048576).toFixed(1), 'MB', 'procedures', numberingCount);
})().catch(e => { console.error(e); process.exit(1); });
