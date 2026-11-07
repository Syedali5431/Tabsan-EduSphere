// Draws the "how the app connects" diagram for the guide (SVG → PNG).
const sharp = require('sharp');
const NAVY = '#0F2A4A', TEAL = '#1590A8', SKY = '#2F80C8', INK = '#1F2937', MUTED = '#64748B', LINE = '#CBD5E1', SOFT = '#EEF6F9';
const box = (x, y, w, h, title, lines, fill = '#FFFFFF', stroke = NAVY) => `
  <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="18" fill="${fill}" stroke="${stroke}" stroke-width="3"/>
  <text x="${x + w / 2}" y="${y + 46}" text-anchor="middle" font-family="Segoe UI Semibold, Segoe UI" font-size="27" fill="${NAVY}">${title}</text>
  ${lines.map((l, i) => `<text x="${x + w / 2}" y="${y + 84 + i * 30}" text-anchor="middle" font-family="Segoe UI" font-size="21" fill="${MUTED}">${l}</text>`).join('')}`;
const arrow = (x1, y1, x2, y2, label, color = TEAL, dy = -12, both = true, dx = 0) => `
  <line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="4" marker-end="url(#a-${color.slice(1)})" ${both ? `marker-start="url(#s-${color.slice(1)})"` : ''}/>
  <text x="${(x1 + x2) / 2 + dx}" y="${(y1 + y2) / 2 + dy}" text-anchor="middle" font-family="Segoe UI Semibold, Segoe UI" font-size="20" fill="${color}" paint-order="stroke" stroke="#EEF6F9" stroke-width="10" stroke-linejoin="round">${label}</text>`;
const label = (x, y, lines, color) => lines.map((l, i) => `<text x="${x}" y="${y + i * 24}" text-anchor="middle" font-family="Segoe UI Semibold, Segoe UI" font-size="20" fill="${color}">${l}</text>`).join('');
const markers = [TEAL, SKY, '#B97912'].map(c => `
  <marker id="a-${c.slice(1)}" markerUnits="userSpaceOnUse" markerWidth="18" markerHeight="18" refX="16" refY="9" orient="auto"><path d="M0,0 L18,9 L0,18 z" fill="${c}"/></marker>
  <marker id="s-${c.slice(1)}" markerUnits="userSpaceOnUse" markerWidth="18" markerHeight="18" refX="2" refY="9" orient="auto"><path d="M18,0 L0,9 L18,18 z" fill="${c}"/></marker>`).join('');
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="860" viewBox="0 0 1600 860">
  <defs>${markers}</defs>
  <rect width="1600" height="860" fill="#FFFFFF"/>
  <rect x="20" y="20" width="1560" height="820" rx="24" fill="${SOFT}"/>
  <text x="800" y="76" text-anchor="middle" font-family="Segoe UI Semibold, Segoe UI" font-size="32" fill="${NAVY}">One EduSphere server — two ways in</text>
  ${box(70, 150, 360, 210, 'Mobile app', ['Students · Teachers · Parents', 'Android and iOS', 'server address entered once'])}
  ${box(70, 520, 360, 210, 'Web portal', ['Any browser', 'Admins and all users', 'https://portal.your-school'])}
  ${box(620, 300, 380, 280, 'EduSphere API', ['edu.your-school.com', 'or 192.168.1.10:5181', 'accounts · courses · results', 'attendance · notifications'], '#FFFFFF', TEAL)}
  ${box(1180, 340, 330, 200, 'Database', ['one copy of every record', '(SQL Server)'])}
  ${box(1180, 620, 330, 160, 'Firebase (optional)', ['push notifications', 'to phones'], '#FFFFFF', LINE)}
  ${arrow(434, 300, 616, 380, '', TEAL)}${label(522, 222, ['HTTPS, or HTTP', 'on the institute’s', 'own network'], TEAL)}
  ${arrow(434, 600, 616, 520, '', TEAL)}${label(522, 604, ['portal pages', 'call the API'], TEAL)}
  ${arrow(1004, 440, 1176, 440, 'reads &amp; writes', SKY, -18)}
  ${arrow(1002, 560, 1176, 690, 'push', '#B97912', 36, false, -30)}
  <path d="M250,362 C250,440 250,440 250,516" stroke="${SKY}" stroke-width="4" fill="none" stroke-dasharray="10 8" marker-end="url(#a-2F80C8)" marker-start="url(#s-2F80C8)"/>
  <text x="268" y="430" font-family="Segoe UI Semibold, Segoe UI" font-size="20" fill="${SKY}">single sign-on in the</text>
  <text x="268" y="456" font-family="Segoe UI Semibold, Segoe UI" font-size="20" fill="${SKY}">browser · quizzes</text>
  <text x="800" y="810" text-anchor="middle" font-family="Segoe UI" font-size="21" fill="${INK}">Same accounts, same data, at the same moment: nothing is copied between the app and the portal.</text>
</svg>`;
sharp(Buffer.from(svg)).png().toFile('shots/diagram/connection.png').then(i => console.log('diagram', i.width + 'x' + i.height));
