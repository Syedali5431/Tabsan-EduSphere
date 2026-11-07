// Draws the guide's two diagrams (SVG → PNG): the connection chain, and two institutes on one server.
// usage: node diagram.js   (writes shots/diagram/chain.png and shots/diagram/two-institutes.png)
const fs = require('fs');
const sharp = require('sharp');
const NAVY = '#0F2A4A', TEAL = '#1590A8', SKY = '#2F80C8', AMBER = '#B97912', INK = '#1F2937', MUTED = '#64748B', LINE = '#CBD5E1', SOFT = '#EEF6F9';
fs.mkdirSync('shots/diagram', { recursive: true });

const text = (x, y, s, { size = 21, color = MUTED, weight = '', anchor = 'middle', mono = false } = {}) =>
  `<text x="${x}" y="${y}" text-anchor="${anchor}" font-family="${mono ? 'Consolas, monospace' : weight ? 'Segoe UI Semibold, Segoe UI' : 'Segoe UI'}" font-size="${size}" fill="${color}">${s}</text>`;
const box = (x, y, w, h, title, lines, stroke = NAVY, fill = '#FFFFFF') => `
  <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="18" fill="${fill}" stroke="${stroke}" stroke-width="3"/>
  ${text(x + w / 2, y + 44, title, { size: 26, color: NAVY, weight: 1 })}
  ${lines.map((l, i) => text(x + w / 2, y + 80 + i * 28, l, { size: 20 })).join('')}`;
const markers = [TEAL, SKY, AMBER, NAVY].map(c => `
  <marker id="a-${c.slice(1)}" markerUnits="userSpaceOnUse" markerWidth="18" markerHeight="18" refX="16" refY="9" orient="auto"><path d="M0,0 L18,9 L0,18 z" fill="${c}"/></marker>`).join('');
const arrow = (x1, y1, x2, y2, color = TEAL, dashed = false) =>
  `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="4" ${dashed ? 'stroke-dasharray="10 8"' : ''} marker-end="url(#a-${color.slice(1)})"/>`;
// A setting name on a white pill, so it reads clearly on top of a line.
const pill = (x, y, lines, color) => {
  const w = Math.max(...lines.map(l => l.length)) * 11.5 + 28, h = lines.length * 26 + 14;
  return `<rect x="${x - w / 2}" y="${y - 20}" width="${w}" height="${h}" rx="10" fill="#FFFFFF" stroke="${color}" stroke-width="2"/>` +
    lines.map((l, i) => text(x, y + 2 + i * 26, l, { size: 19, color, mono: /[:_]/.test(l) && !/ /.test(l), weight: 1 })).join('');
};
const frame = (w, h, title, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>${markers}</defs><rect width="${w}" height="${h}" fill="#FFFFFF"/>
  <rect x="20" y="20" width="${w - 40}" height="${h - 40}" rx="24" fill="${SOFT}"/>
  ${text(w / 2, 74, title, { size: 32, color: NAVY, weight: 1 })}${body}</svg>`;

// 1. Who connects to whom, and which setting makes each connection.
const chain = frame(1600, 900, 'Four parts, three connections', `
  ${box(70, 150, 360, 200, 'Mobile app', ['Android · iOS', 'on each phone'])}
  ${box(70, 560, 360, 200, 'Web portal', ['Tabsan.EduSphere.Web', 'port 5063 by default'])}
  ${box(640, 330, 360, 240, 'EduSphere API', ['Tabsan.EduSphere.API', 'port 5181 by default', 'the only part that', 'opens the database'], TEAL)}
  ${box(1200, 350, 330, 200, 'SQL Server', ['one database', 'per institute'])}
  ${arrow(434, 270, 636, 390, TEAL)}${pill(578, 222, ['server address typed', 'in the app'], TEAL)}
  ${arrow(434, 640, 636, 520, TEAL)}${pill(578, 704, ['EduApi:BaseUrl', 'EDUSPHERE_APP_BASE_URL'], TEAL)}
  ${arrow(1004, 450, 1196, 450, SKY)}${pill(1100, 250, ['EDUSPHERE_DB_CONNECTION', 'or ConnectionStrings:', 'DefaultConnection'], SKY)}
  <line x1="1100" y1="328" x2="1100" y2="444" stroke="${SKY}" stroke-width="2" stroke-dasharray="4 5"/>
  ${arrow(250, 354, 250, 556, AMBER, true)}${pill(250, 450, ['Mobile:PortalBaseUrl', 'quizzes · sign-on'], AMBER)}
  ${text(800, 820, 'Phones and browsers never reach the database: everything goes through the API.', { size: 22, color: INK })}`);

// 2. Two institutes on one server: separate ports, separate databases, one SQL Server.
const inst = (x, name, api, portal, db, color) => `
  <rect x="${x}" y="130" width="640" height="470" rx="20" fill="#FFFFFF" stroke="${color}" stroke-width="3" stroke-dasharray="2 0"/>
  ${text(x + 320, 175, name, { size: 26, color, weight: 1 })}
  ${box(x + 30, 210, 270, 150, 'API', [api])}
  ${box(x + 340, 210, 270, 150, 'Portal', [portal])}
  ${arrow(x + 340, 285, x + 304, 285, color)}
  ${box(x + 150, 420, 340, 150, 'Database', [db], SKY)}
  ${arrow(x + 165, 364, x + 260, 416, SKY)}`;
const two = frame(1600, 900, 'Two institutes on one server', `
  ${inst(70, 'Institute A — the main installation', 'http://0.0.0.0:5181', 'http://0.0.0.0:5063', 'Tabsan-EduSphere', NAVY)}
  ${inst(890, 'Institute B — Northfield Model School', 'http://0.0.0.0:5281', 'http://0.0.0.0:5163', 'EduSphere-Northfield', TEAL)}
  <rect x="190" y="410" width="1220" height="210" rx="22" fill="none" stroke="${MUTED}" stroke-width="2" stroke-dasharray="8 8"/>
  ${text(560, 650, 'one SQL Server (localhost\\SQLEXPRESS) holds both databases', { size: 20, color: MUTED })}
  ${pill(200, 740, ['phones of A type', '192.168.1.109:5181'], NAVY)}
  ${pill(1020, 740, ['phones of B type', '192.168.1.109:5281'], TEAL)}
  ${arrow(130, 718, 130, 366, NAVY)}${arrow(950, 718, 950, 366, TEAL)}
  ${text(800, 840, 'Nothing is shared but the SQL Server: each institute has its own accounts, data and address.', { size: 22, color: INK })}`);

Promise.all([
  sharp(Buffer.from(chain)).png().toFile('shots/diagram/chain.png'),
  sharp(Buffer.from(two)).png().toFile('shots/diagram/two-institutes.png'),
]).then(r => console.log('diagrams', r.map(i => i.width + 'x' + i.height).join(', ')));
