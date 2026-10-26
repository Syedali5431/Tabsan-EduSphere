// 02-Learning-Activity.mjs — Tabsan EduSphere
// Classroom activity for the demo schools, created through the portal's API as the teachers, students and campus admins
// would create it (run 01-School-Records.sql first; the API must be running).
//
//   Every subject      a class test (quiz) taken by most of the class, two homework assignments (the first handed in and
//                      mostly marked), a term module (page, study link, homework, test), a discussion thread with replies
//                      and an upcoming live class
//   English, Maths     course and programme learning outcomes, aligned to test questions, with mastery calculated
//   Mathematics        a SCORM practice activity (scorm/maths-practice, zipped here)
//   Class 9 and 10     the Science stream for every student; a "Practice lab (LTI)" link in Computer Science when the
//                      installation has an LTI tool
//   Every campus       helpdesk tickets (open, in progress and resolved), an API token for the campus admin
//   The school system  result components (Quiz 20%, Midterm 30%, Final 50%) and grade points for School, live classes
//                      by meeting link, a learning record store credential, the Science / Commerce / Arts streams
//
// usage:  node 02-Learning-Activity.mjs [apiBaseUrl] [campusCode]     (default http://localhost:5181/api/v1, all campuses)
// Safe to re-run: what already exists (found by title) is left as it is.
import fs from 'node:fs';
const API = (process.argv[2] ?? process.env.EDUSPHERE_API ?? 'http://localhost:5181/api/v1').replace(/\/$/, '');
const ONLY = process.argv[3]?.toUpperCase();
const PASSWORD = process.env.DEMO_PASSWORD ?? 'EduSphere147';
const SA = 'superadmin';

// ── API helpers ──────────────────────────────────────────────────────────────────────
const tokens = new Map();
const sleep = ms => new Promise(r => setTimeout(r, ms));
async function login(user) {
  if (tokens.has(user)) return tokens.get(user);
  const p = (async () => {
    for (let i = 0; ; i++) {
      const r = await fetch(`${API}/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username: user, password: PASSWORD }) });
      if (r.status === 429 && i < 10) { await sleep(2000 * (i + 1)); continue; }
      if (!r.ok) throw new Error(`sign-in ${user}: ${r.status}`);
      return (await r.json()).accessToken;
    }
  })();
  tokens.set(user, p);
  return p;
}
async function call(user, method, url, body) {
  for (let i = 0; ; i++) {
    const headers = { Authorization: `Bearer ${await login(user)}` };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    const r = await fetch(`${API}/${url}`, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    if (r.status === 429 && i < 10) { await sleep(1000 * Number(r.headers.get('retry-after') ?? 2)); continue; }
    const text = await r.text();
    if (!r.ok) throw new Error(`${user} ${method} ${url} -> ${r.status} ${text.slice(0, 200)}`);
    return text ? JSON.parse(text) : null;
  }
}
async function upload(user, url, form) {
  for (let i = 0; ; i++) {
    const r = await fetch(`${API}/${url}`, { method: 'POST', headers: { Authorization: `Bearer ${await login(user)}` }, body: form });
    if (r.status === 429 && i < 10) { await sleep(1000 * Number(r.headers.get('retry-after') ?? 2)); continue; }
    const text = await r.text();
    if (!r.ok) throw new Error(`${user} POST ${url} -> ${r.status} ${text.slice(0, 200)}`);
    return text ? JSON.parse(text) : null;
  }
}

// A zip file of the given [name, Buffer] entries, stored without compression (all a SCORM package needs).
const crcTable = Array.from({ length: 256 }, (_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
const crc32 = buf => { let c = 0xFFFFFFFF; for (const b of buf) c = crcTable[(c ^ b) & 0xFF] ^ (c >>> 8); return (c ^ 0xFFFFFFFF) >>> 0; };
function zip(entries) {
  const dosDate = ((2026 - 1980) << 9) | (1 << 5) | 1;
  const parts = [], central = [];
  let offset = 0;
  for (const [name, data] of entries) {
    const n = Buffer.from(name), crc = crc32(data);
    const local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0); local.writeUInt16LE(20, 4); local.writeUInt16LE(0x0800, 6); local.writeUInt16LE(dosDate, 12);
    local.writeUInt32LE(crc, 14); local.writeUInt32LE(data.length, 18); local.writeUInt32LE(data.length, 22); local.writeUInt16LE(n.length, 26);
    const dir = Buffer.alloc(46);
    dir.writeUInt32LE(0x02014b50, 0); dir.writeUInt16LE(20, 4); dir.writeUInt16LE(20, 6); dir.writeUInt16LE(0x0800, 8); dir.writeUInt16LE(dosDate, 14);
    dir.writeUInt32LE(crc, 16); dir.writeUInt32LE(data.length, 20); dir.writeUInt32LE(data.length, 24); dir.writeUInt16LE(n.length, 28); dir.writeUInt32LE(offset, 42);
    parts.push(local, n, data);
    central.push(dir, n);
    offset += local.length + n.length + data.length;
  }
  const dir = Buffer.concat(central), end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0); end.writeUInt16LE(entries.length, 8); end.writeUInt16LE(entries.length, 10); end.writeUInt32LE(dir.length, 12); end.writeUInt32LE(offset, 16);
  return Buffer.concat([...parts, dir, end]);
}
const practiceDir = new URL('./scorm/maths-practice/', import.meta.url);
const mathsPractice = zip(['imsmanifest.xml', 'index.html'].map(f => [f, fs.readFileSync(new URL(f, practiceDir))]));

// The signed-in user's id, read from the access token.
async function userIdOf(user) {
  const claims = JSON.parse(Buffer.from((await login(user)).split('.')[1], 'base64url').toString());
  return claims.sub ?? claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
}
const get = (u, url) => call(u, 'GET', url);
const post = (u, url, body) => call(u, 'POST', url, body ?? {});
const put = (u, url, body) => call(u, 'PUT', url, body);
const list = x => (Array.isArray(x) ? x : x?.items ?? []);
const days = (n, hourUtc) => { const d = new Date(Date.now() + n * 86400000); if (hourUtc !== undefined) d.setUTCHours(hourUtc, 0, 0, 0); return d.toISOString(); };

const counts = {};
const failures = [];
const count = key => { counts[key] = (counts[key] ?? 0) + 1; };
async function attempt(label, fn) {
  try { return await fn(); } catch (e) { failures.push(`${label}: ${e.message}`); return null; }
}

// ── Subject content ──────────────────────────────────────────────────────────────────
const tf = (text, answer) => ({ text, type: 'MultipleChoice', marks: 1, options: [['True', answer], ['False', !answer]] });
const mc = (text, right, ...wrong) => ({ text, type: 'MultipleChoice', marks: 2, options: [[right, true], ...wrong.map(w => [w, false])] });
function questions(subject, cls) {
  const band = cls <= 5 ? 'A' : cls <= 8 ? 'B' : 'C';
  switch (subject) {
    case 'ENG': return {
      A: [mc('Which word is a noun in "The dog runs fast"?', 'dog', 'runs', 'fast'), mc('Choose the correct spelling.', 'beautiful', 'beutiful', 'beautifull'),
          mc('What is the plural of "child"?', 'children', 'childs', 'childes'), tf('Every sentence starts with a capital letter.', true)],
      B: [mc('Choose the past tense of "go".', 'went', 'goed', 'gone'), mc('Which word is an adjective in "She wore a red scarf"?', 'red', 'wore', 'scarf'),
          mc('Pick a word that means the same as "happy".', 'joyful', 'angry', 'tired'), tf('"Their" and "there" mean the same thing.', false)],
      C: [mc('Which sentence is in the passive voice?', 'The letter was written by Ali.', 'Ali wrote the letter.', 'Ali is writing a letter.'),
          mc('Complete: "He is good ___ mathematics."', 'at', 'in', 'on'),
          mc('Which is a complex sentence?', 'I stayed home because it was raining.', 'I stayed home.', 'Stay home!'), tf('A paragraph should have one main idea.', true)]
    }[band];
    case 'URD': return [mc('اردو حروفِ تہجی کا پہلا حرف کون سا ہے؟', 'ا', 'ب', 'پ'), mc('"کتاب" کی جمع کیا ہے؟', 'کتابیں', 'کتابا', 'کتابہ'),
      mc('"دن" کا متضاد کیا ہے؟', 'رات', 'صبح', 'دوپہر'), tf('علامہ اقبال پاکستان کے قومی شاعر ہیں۔', true)];
    case 'MTH': {
      const a = cls + 3, b = cls + 5;
      return [mc(`What is ${a} × ${b}?`, String(a * b), String(a * b + a), String(a * b - b)),
        { text: `Solve: x + ${a} = ${a + b}. What is x?`, type: 'Numeric', marks: 2, configJson: JSON.stringify({ answer: b, tolerance: 0 }) },
        mc(`What is ${cls * 10}% of 200?`, String(cls * 20), String(cls * 10), String(cls * 20 + 10)), tf('A square has four equal sides.', true)];
    }
    case 'SCI': return [mc('Which planet is closest to the Sun?', 'Mercury', 'Venus', 'Mars'), mc('At what temperature does water boil at sea level?', '100 °C', '50 °C', '0 °C'),
      mc('Which part of a plant makes food?', 'Leaf', 'Root', 'Stem'), tf('Sound travels faster than light.', false)];
    case 'PHY': return [mc('What is the SI unit of force?', 'newton', 'joule', 'watt'), mc('Speed = distance ÷ ?', 'time', 'mass', 'force'),
      mc('Which of these is a vector quantity?', 'velocity', 'mass', 'temperature'), tf('Acceleration due to gravity on Earth is about 9.8 m/s².', true)];
    case 'ISL': return [mc('How many pillars of Islam are there?', 'Five', 'Three', 'Seven'), mc('In which month do Muslims fast?', 'Ramadan', 'Shawwal', 'Muharram'),
      mc('How many daily prayers are obligatory?', 'Five', 'Three', 'Two'), tf('Zakat is one of the pillars of Islam.', true)];
    case 'SST': return [mc('What is the capital of Pakistan?', 'Islamabad', 'Karachi', 'Lahore'), mc('Which is the longest river in Pakistan?', 'Indus', 'Ravi', 'Jhelum'),
      mc('Who is known as the founder of Pakistan?', 'Quaid-e-Azam Muhammad Ali Jinnah', 'Allama Muhammad Iqbal', 'Liaquat Ali Khan'), tf('K2 is the second-highest mountain in the world.', true)];
    case 'CSC': return [mc('Which of these is an input device?', 'Keyboard', 'Monitor', 'Printer'), mc('What does CPU stand for?', 'Central Processing Unit', 'Computer Personal Unit', 'Central Program Utility'),
      mc('Which number system do computers use?', 'Binary', 'Decimal', 'Roman'), tf('RAM keeps its data after the computer is switched off.', false)];
  }
  return [];
}
const homework = {
  ENG: [['Homework 1: My favourite place', 'Write a paragraph of 8 to 10 sentences about your favourite place. Use at least three adjectives.'],
        ['Homework 2: Letter to a friend', 'Write a short letter to a friend about your school sports day.']],
  URD: [['Homework 1: مضمون "میرا گھر"', 'اپنے گھر کے بارے میں آٹھ سے دس جملے لکھیں۔'], ['Homework 2: کہانی لکھیں', 'کسی جانور کے بارے میں ایک چھوٹی کہانی لکھیں۔']],
  MTH: [['Homework 1: Exercise 2.1, questions 1-10', 'Solve questions 1 to 10 of Exercise 2.1. Show your working for every question.'],
        ['Homework 2: Word problems', 'Solve the five word problems on page 34. Write the answer with its unit.']],
  SCI: [['Homework 1: Parts of a plant', 'Draw a plant and label the root, stem, leaf and flower. Write one line about what each part does.'],
        ['Homework 2: States of matter', 'Give two examples each of solids, liquids and gases from your home.']],
  PHY: [['Homework 1: Speed and velocity', 'Answer questions 1 to 6 on speed, distance and time. Show the formula you use.'],
        ['Homework 2: Newton\'s laws', 'Give one everyday example for each of Newton\'s three laws of motion.']],
  ISL: [['Homework 1: Pillars of Islam', 'Write the five pillars of Islam with one sentence about each.'], ['Homework 2: Good manners', 'Write five good manners taught in Islam.']],
  SST: [['Homework 1: Map of Pakistan', 'Draw a map of Pakistan and mark the four provinces and their capitals.'],
        ['Homework 2: Our national heroes', 'Write a paragraph about one national hero of Pakistan.']],
  CSC: [['Homework 1: Parts of a computer', 'List five input devices and five output devices with a picture of each.'],
        ['Homework 2: Binary numbers', 'Convert the decimal numbers 5, 9 and 12 into binary.']]
};
const answers = {
  ENG: n => `My favourite place is the park near my house. It has tall green trees and colourful flowers. Every evening I go there with my ${n % 2 ? 'brother' : 'sister'}. We play cricket on the soft grass. The air is fresh and cool. Many birds sing in the trees. I feel happy and calm there. I love my favourite place.`,
  URD: () => 'میرا گھر بہت خوبصورت ہے۔ اس میں تین کمرے اور ایک صحن ہے۔ صحن میں ایک آم کا درخت ہے۔ میں اپنے امی ابو اور بہن بھائیوں کے ساتھ رہتا ہوں۔ ہم سب مل کر کھانا کھاتے ہیں۔ مجھے اپنا گھر بہت پسند ہے۔',
  MTH: n => `Q1 = 12, Q2 = 45, Q3 = 7, Q4 = 81, Q5 = 3/4, Q6 = 0.25, Q7 = 150, Q8 = 9, Q9 = ${16 + (n % 3)}, Q10 = 64. Working is shown in my notebook (photo attached in class).`,
  SCI: () => 'Root: takes water from the soil. Stem: carries water to the leaves. Leaf: makes food using sunlight. Flower: makes seeds.',
  PHY: n => `1) speed = 120 / 2 = 60 km/h. 2) distance = 15 × 4 = 60 m. 3) time = 100 / 25 = 4 s. 4) ${20 + n} m/s. 5) velocity has direction. 6) average speed = total distance / total time.`,
  ISL: () => 'Shahadah: faith in one Allah. Salah: five daily prayers. Zakat: giving to the poor. Sawm: fasting in Ramadan. Hajj: pilgrimage to Makkah.',
  SST: () => 'Punjab - Lahore, Sindh - Karachi, Khyber Pakhtunkhwa - Peshawar, Balochistan - Quetta. The federal capital is Islamabad.',
  CSC: () => 'Input: keyboard, mouse, scanner, microphone, webcam. Output: monitor, printer, speakers, projector, headphones.'
};
const outcomeTitles = {
  ENG: ['Read a text and answer questions about it', 'Write clear sentences and paragraphs'],
  MTH: ['Calculate accurately with numbers', 'Solve equations and word problems']
};
const programOutcomes = [['PLO-1', 'Communicate clearly in reading and writing', 'ENG'], ['PLO-2', 'Use mathematics to solve everyday problems', 'MTH']];
const slotOf = { ENG: 'english', URD: 'urdu', MTH: 'maths', SCI: 'science', PHY: 'science', ISL: 'social', SST: 'social', CSC: 'social' };
const studentLink = { ENG: 'https://learnenglishteens.britishcouncil.org/', URD: 'https://www.rekhta.org/', MTH: 'https://www.khanacademy.org/math',
  SCI: 'https://www.khanacademy.org/science', PHY: 'https://www.khanacademy.org/science/physics', ISL: 'https://www.bbc.co.uk/bitesize/subjects/zh3rkqt',
  SST: 'https://www.bbc.co.uk/bitesize/subjects/zk26n39', CSC: 'https://www.khanacademy.org/computing/computer-programming' };

// ── School-wide settings ─────────────────────────────────────────────────────────────
await attempt('result components', async () => {
  const current = await get(SA, 'result-calculation?institutionType=1');
  if (current.componentRules.length && current.gpaScaleRules.length) return;
  await post(SA, 'result-calculation', {
    institutionType: 1,
    gpaScaleRules: [[4.0, 85], [3.7, 80], [3.3, 75], [3.0, 70], [2.7, 65], [2.3, 60], [2.0, 55], [1.0, 50], [0, 0]].map(([gradePoint, minimumScore], i) => ({ id: null, gradePoint, minimumScore, displayOrder: i + 1 })),
    componentRules: [['Quiz', 20], ['Midterm', 30], ['Final', 50]].map(([name, weightage], i) => ({ id: null, name, weightage, displayOrder: i + 1, isActive: true }))
  });
  count('result components');
});
await attempt('live class settings', async () => {
  const current = await get(SA, 'live/settings');
  if (current?.isEnabled) return; // a provider chosen on the Live Classes settings page is kept
  await put(SA, 'live/settings', { provider: 'ExternalLink', isEnabled: true, baseUrl: null, secret: null, accountId: null, clientId: null, organizerUserId: null, joinEarlyMinutes: 15 });
  count('live class settings');
});
await attempt('xAPI credential', async () => {
  const creds = list(await get(SA, 'xapi/credentials'));
  if (creds.some(c => c.name === 'Interactive lessons (xAPI)')) return;
  await post(SA, 'xapi/credentials', { name: 'Interactive lessons (xAPI)', offeringId: null });
  count('xAPI credentials');
});

// Streams for Class 9 and 10 (the portal shows these students the subjects of their stream).
const streams = {};
await attempt('school streams', async () => {
  const all = list(await get(SA, 'school-streams'));
  for (const [name, description] of [['Science', 'Physics, Chemistry, Biology or Computer Science, with Mathematics'],
                                     ['Commerce', 'Accounting, Business Studies and Economics, with Mathematics'],
                                     ['Arts', 'History, Geography and Literature']]) {
    let stream = all.find(s => s.name === name);
    if (!stream) { stream = await put(SA, 'school-streams/00000000-0000-0000-0000-000000000000', { name, description, isActive: true }); count('school streams'); }
    streams[name] = stream.id ?? stream.streamId;
  }
});

// ── Offerings by campus ──────────────────────────────────────────────────────────────
const offerings = list(await get(SA, 'course/offerings'))
  .map(o => { const m = /^([A-Z]+-[A-Z]+)-([A-Z]{3})(\d{2})$/.exec(o.courseCode ?? ''); return m ? { ...o, campusCode: m[1], subject: m[2], cls: Number(m[3]) } : null; })
  .filter(o => o && (!ONLY || o.campusCode === ONLY));
const campuses = [...new Set(offerings.map(o => o.campusCode))].sort();
console.log(`${offerings.length} subject offerings in ${campuses.length} campuses`);
const keyOf = code => code.toLowerCase().replace('-', '');
const studentOf = (campusCode, regNo) => { const [, , cls, n] = regNo.split('-'); return `${keyOf(campusCode)}.c${Number(cls)}s${Number(n)}`; };

async function offeringActivity(o) {
  const teacher = `${keyOf(o.campusCode)}.${slotOf[o.subject]}`;
  const roster = (await get(teacher, `assignment/offerings/${o.id}/roster`)).sort((a, b) => a.registrationNumber.localeCompare(b.registrationNumber));
  const students = roster.map(r => ({ ...r, user: studentOf(o.campusCode, r.registrationNumber), n: Number(r.registrationNumber.split('-').pop()) }));
  const title = o.courseTitle;

  // Class test
  const quizzes = list(await get(teacher, `quiz/by-offering/${o.id}`));
  let quiz = quizzes.find(q => q.title === `Class test 1: ${title}`);
  let quizId = quiz?.quizId ?? quiz?.id;
  if (!quizId) {
    quizId = (await post(teacher, 'quiz', { courseOfferingId: o.id, title: `Class test 1: ${title}`, instructions: 'Answer all questions. You have 15 minutes.', timeLimitMinutes: 15, maxAttempts: 1, availableFrom: null, availableUntil: days(30) })).quizId;
    for (const [i, q] of questions(o.subject, o.cls).entries())
      await post(teacher, 'quiz/question', { quizId, text: q.text, type: q.type, marks: q.marks, orderIndex: i + 1, configJson: q.configJson ?? null, options: q.options?.map(([text, isCorrect], k) => ({ text, isCorrect, orderIndex: k + 1 })) });
    await put(teacher, `quiz/${quizId}/settings`, { shuffleQuestions: false, shuffleOptions: true, oneQuestionAtATime: false, lockAnsweredQuestions: false, accessCode: null, allowedIpRanges: null, reviewVisibility: 'CorrectAnswers', reviewAfterCloseOnly: false });
    await post(teacher, `quiz/${quizId}/publish`);
    count('class tests');
    // Five of the six students take it; the stronger the student (lower roll number), the more answers right.
    const detail = await get(teacher, `quiz/${quizId}`);
    const key = Object.fromEntries(detail.questions.map(q => [q.questionId, q]));
    for (const s of students.filter(s => s.n <= 5)) {
      await attempt(`${s.user} takes ${o.courseCode} test`, async () => {
        const started = await post(s.user, `quiz/${quizId}/start`, { accessCode: null });
        const paper = await get(s.user, `quiz/attempt/${started.attemptId}/paper`);
        const answersOut = paper.questions.map((q, i) => {
          const right = (i + s.n) % 5 !== 0 && !(s.n === 5 && i % 2 === 0);
          const k = key[q.questionId];
          if (q.type === 'Numeric') return { quizQuestionId: q.questionId, responseJson: JSON.stringify({ value: String(right ? JSON.parse(k.configJson).answer : 0) }) };
          const opts = k?.options ?? q.options ?? [];
          const pick = right ? opts.filter(x => x.isCorrect) : opts.filter(x => !x.isCorrect).slice(0, 1);
          return { quizQuestionId: q.questionId, selectedOptionIds: pick.map(x => x.optionId) };
        });
        await post(s.user, 'quiz/attempt/submit', { attemptId: started.attemptId, answers: answersOut });
        count('test attempts');
      });
    }
  }

  // Homework
  const [hw1, hw2] = homework[o.subject];
  const existing = list(await get(teacher, `assignment/by-offering/${o.id}`));
  const assignment = async ([t, d], dueInDays) => {
    const found = existing.find(a => a.title === t);
    if (found) return [found.id, false];
    const id = (await post(teacher, 'assignment', { courseOfferingId: o.id, title: t, description: d, dueDate: days(dueInDays, 12), maxMarks: 20 })).id;
    await post(teacher, `assignment/${id}/publish`);
    count('homework');
    return [id, true];
  };
  const [hw1Id, hw1New] = await assignment(hw1, 3);
  await assignment(hw2, 12);
  if (hw1New) {
    if (['ENG', 'MTH'].includes(o.subject)) await attempt(`rubric ${o.courseCode}`, async () => {
      await post(teacher, 'rubric', { assignmentId: hw1Id, title: `${hw1[0]} rubric`, criteria: [
        { name: 'Content', maxPoints: 10, displayOrder: 1, levels: [{ label: 'Beginning', pointsAwarded: 4, displayOrder: 1 }, { label: 'Developing', pointsAwarded: 7, displayOrder: 2 }, { label: 'Excellent', pointsAwarded: 10, displayOrder: 3 }] },
        { name: o.subject === 'MTH' ? 'Working shown' : 'Spelling and grammar', maxPoints: 10, displayOrder: 2, levels: [{ label: 'Beginning', pointsAwarded: 4, displayOrder: 1 }, { label: 'Developing', pointsAwarded: 7, displayOrder: 2 }, { label: 'Excellent', pointsAwarded: 10, displayOrder: 3 }] }
      ] });
      count('rubrics');
    });
    for (const s of students.filter(s => s.n <= 5))
      await attempt(`${s.user} hands in ${o.courseCode}`, async () => { await post(s.user, 'assignment/submit', { assignmentId: hw1Id, textContent: answers[o.subject](s.n) }); count('hand-ins'); });
    const marks = { 1: 18, 2: 16, 3: 15, 4: 12 };
    const feedback = { 18: 'Excellent work. Keep it up!', 16: 'Very good. Check your spelling.', 15: 'Good effort. Add more detail next time.', 12: 'Satisfactory. Please see me about the corrections.' };
    for (const s of students.filter(s => marks[s.n]))
      await attempt(`mark ${s.user} ${o.courseCode}`, async () => { await put(teacher, 'assignment/submissions/grade', { assignmentId: hw1Id, studentProfileId: s.studentProfileId, marksAwarded: marks[s.n], feedback: feedback[marks[s.n]] }); count('marked'); });
  }

  // Term module
  const modules = list(await get(teacher, `lms/modules/${o.id}`));
  if (!modules.some(m => m.title === 'Term 1: Weeks 1-6')) await attempt(`module ${o.courseCode}`, async () => {
    const moduleId = (await post(teacher, 'lms/module', { offeringId: o.id, title: 'Term 1: Weeks 1-6', weekNumber: 1, body: `What we cover in ${title} this term, with the homework and the class test.` })).id;
    await post(teacher, `lms/module/${moduleId}/publish`);
    const item = body => post(teacher, `lms/modules/${moduleId}/items`, body);
    await item({ itemType: 'Header', title: 'Start here' });
    await item({ itemType: 'Page', title: 'Term plan', body: `Weeks 1-2: revision of last year. Weeks 3-4: new chapters. Week 5: class test. Week 6: homework review. Bring your ${title} book to every class.`, requirementType: 'MustView' });
    await item({ itemType: 'ExternalUrl', title: 'Practise online', url: studentLink[o.subject], requirementType: 'MustMarkDone' });
    await item({ itemType: 'Assignment', targetId: hw1Id, requirementType: 'MustSubmit' });
    await item({ itemType: 'Quiz', targetId: quizId, requirementType: 'MustScoreAtLeast', minScorePercent: 50 });
    count('modules');
    for (const s of students.filter(s => s.n <= 3)) await attempt(`${s.user} opens module`, async () => {
      const view = await get(s.user, `lms/offerings/${o.id}/my-modules`);
      for (const it of view.modules?.[0]?.items ?? []) {
        if (it.itemType === 'Page') await post(s.user, `lms/module-items/${it.id}/open`);
        if (it.itemType === 'ExternalUrl' && s.n <= 2) await post(s.user, `lms/module-items/${it.id}/done`);
      }
    });
  });

  // Discussion
  const threads = list(await get(teacher, `discussion/${o.id}/threads`));
  if (!threads.some(t => t.title === `Questions about ${hw1[0]}`)) await attempt(`discussion ${o.courseCode}`, async () => {
    const thread = await post(teacher, 'discussion/thread', { offeringId: o.id, authorId: '00000000-0000-0000-0000-000000000000', title: `Questions about ${hw1[0]}`, threadType: 'FAQ' });
    const threadId = thread.id ?? thread.threadId;
    await post(teacher, 'discussion/reply', { threadId, authorId: threadId, body: 'Ask your questions about the homework here. Check this thread before you hand it in.' });
    if (students[0]) await post(students[0].user, 'discussion/reply', { threadId, authorId: threadId, body: 'Can we write more than ten sentences?' });
    await post(teacher, 'discussion/reply', { threadId, authorId: threadId, body: 'Yes, but keep it neat and on topic.' });
    if (students[2]) await post(students[2].user, 'discussion/reply', { threadId, authorId: threadId, body: 'Thank you, Miss/Sir.' });
    count('discussions');
  });

  // Live class
  const sessions = list(await get(teacher, `live/offerings/${o.id}/sessions`));
  if (!sessions.some(s => s.title === `Revision class: ${title}`)) await attempt(`live class ${o.courseCode}`, async () => {
    const dayOffset = 1 + (o.cls + o.subject.length) % 6;
    await post(teacher, `live/offerings/${o.id}/sessions`, { title: `Revision class: ${title}`, description: 'An online revision class before the class test. Join from your dashboard.', startsAt: days(dayOffset, 10), durationMinutes: 40, autoAttendance: true, minimumMinutes: 20, meetingUrl: `https://meet.jit.si/TabsanSchool-${o.courseCode}` });
    count('live classes');
  });

  // Mathematics: an interactive practice (SCORM 1.2) that saves its score to the course.
  if (o.subject === 'MTH') await attempt(`SCORM ${o.courseCode}`, async () => {
    const packages = list(await get(teacher, `scorm/offerings/${o.id}/packages`));
    if (packages.some(p => p.title === 'Mathematics practice')) return;
    const form = new FormData();
    form.append('file', new Blob([mathsPractice], { type: 'application/zip' }), 'maths-practice.zip');
    for (const [k, v] of Object.entries({ title: 'Mathematics practice', graded: 'true', maxMarks: '10', maxAttempts: '3', publish: 'true' })) form.append(k, v);
    await upload(teacher, `scorm/offerings/${o.id}/packages`, form);
    count('SCORM practice activities');
  });

  // Class 9 and 10 Computer Science: a link to the installation's LTI tool, if it has one.
  if (o.subject === 'CSC') await attempt(`LTI ${o.courseCode}`, async () => {
    const tools = list(await get(teacher, `lti/offerings/${o.id}/tools`));
    if (!tools.length) return;
    const links = list(await get(teacher, `lti/offerings/${o.id}/links`));
    if (links.some(l => l.title === 'Practice lab (LTI)')) return;
    await post(teacher, `lti/offerings/${o.id}/links`, { toolId: tools[0].toolId ?? tools[0].id, moduleId: null, title: 'Practice lab (LTI)', description: 'Practise in an external tool; your mark comes back to the course.', url: null, customParameters: null, presentationTarget: 'window', gradeMaximum: 10 });
    count('LTI links');
  });

  return { quizId, hw1Id, teacher };
}

// Learning outcomes for English and Mathematics: programme outcomes per campus, course outcomes per subject.
async function outcomes(campusCode, done) {
  const admin = `admin.${keyOf(campusCode)}`;
  const programs = list(await get(admin, 'outcomes/programs'));
  const program = programs.find(p => p.code === `${campusCode}-CLS`);
  if (!program) return;
  const all = list(await get(admin, 'outcomes'));
  const plo = {};
  for (const [code, t, subject] of programOutcomes) {
    const found = all.find(x => x.code === `${campusCode}-${code}`);
    const outcome = found ?? await post(admin, 'outcomes', { level: 'Program', programId: program.id, code: `${campusCode}-${code}`, title: t, calculationMethod: 'Average' });
    plo[subject] = outcome.id ?? outcome.outcomeId;
    if (!found) count('programme outcomes');
  }
  for (const { o, quizId, teacher } of done.filter(d => ['ENG', 'MTH'].includes(d.o.subject))) await attempt(`outcomes ${o.courseCode}`, async () => {
    if (all.some(x => x.code === `${o.courseCode}-CLO1`)) return;
    const ids = [];
    for (const [i, t] of outcomeTitles[o.subject].entries()) {
      const clo = await post(teacher, 'outcomes', { level: 'Course', courseId: o.courseId, code: `${o.courseCode}-CLO${i + 1}`, title: t, calculationMethod: 'Average' });
      const cloId = clo.id ?? clo.outcomeId;
      await put(teacher, `outcomes/${cloId}/mappings`, { programOutcomeIds: [plo[o.subject]] });
      ids.push(cloId);
      count('course outcomes');
    }
    const detail = await get(teacher, `quiz/${quizId}`);
    for (const [i, q] of detail.questions.entries()) await put(teacher, 'outcomes/alignments', { targetType: 'QuizQuestion', targetId: q.questionId, outcomeIds: [ids[i < 2 ? 0 : 1]] });
    await post(teacher, `outcomes/mastery/offerings/${o.id}/recalculate`);
  });
}

// Helpdesk tickets and an API token per campus.
async function campusDesk(campusCode, departmentId) {
  const key = keyOf(campusCode);
  const admin = `admin.${key}`;
  const existing = list(await get(admin, 'helpdesk/tickets?pageSize=100'));
  const tickets = [
    [`${key}.c5s1`, 1, 'Cannot see my Science homework', 'The Science homework is not showing on my Assignments page.', 'resolve', 'It is visible now. The teacher published it this morning.'],
    [`${key}.c9s2`, 2, 'Homework upload keeps failing', 'My file does not upload when I hand in my English homework.', 'reply', 'Please try a smaller photo or paste the text into the box.'],
    [`${key}.english`, 3, 'Projector in Room 7 not working', 'The projector in Room 7 does not turn on. I need it for Thursday\'s lesson.', 'assign', null],
    [`${key}.c7s3`, 3, 'Fee receipt shows the wrong month', 'My September receipt says August.', null, null],
    [`${key}.c9s1`, 1, 'Class test marks not showing', 'I took the Physics class test but I cannot see my marks.', 'reply', 'Your teacher is checking the answers. The marks will show by Friday.'],
    [`${key}.maths`, 2, 'Smart board pen not working in Room 3', 'The smart board pen in Room 3 stopped writing during my lesson.', 'resolve', 'The pen has been replaced. Spare batteries are in the staff room.'],
    [`${key}.urdu`, 3, 'Urdu dictionaries for the library', 'Can we order ten Urdu dictionaries for Class 6 to 8?', null, null],
    [`${key}.science`, 3, 'Science lab supplies running low', 'We need more test tubes and litmus paper before the practical next week.', 'assign', null],
    [`${key}.social`, 2, 'Cannot attach a map to the homework', 'Students say the map file is too big to attach to the Social Studies homework.', 'reply', 'Please save the map as a smaller JPG and attach it again.'],
    [`finance.${key}`, 3, 'Bank deposit slips for September', 'Please send the September bank deposit slips so I can confirm the paid receipts.', 'reply', 'The slips are in the accounts office. I will send copies today.']
  ];
  for (const [user, category, subject, body, action, reply] of tickets) await attempt(`ticket ${user}`, async () => {
    if (existing.some(t => t.subject === subject)) return;
    const t = await post(user, 'helpdesk/tickets', { departmentId, category, subject, body });
    const id = t.id ?? t.ticketId;
    count('helpdesk tickets');
    if (reply) await post(admin, `helpdesk/tickets/${id}/messages`, { body: reply, isInternalNote: false });
    if (action === 'assign') await put(admin, `helpdesk/tickets/${id}/assign`, { assignedToId: await userIdOf(admin) });
    if (action === 'resolve') await put(admin, `helpdesk/tickets/${id}/resolve`);
  });
  await attempt(`API token ${admin}`, async () => {
    const mine = list(await get(admin, 'api-tokens'));
    if (mine.some(t => t.name === 'Attendance export')) return;
    await post(admin, 'api-tokens', { name: 'Attendance export', scopes: ['read'], expiresInDays: 90 });
    count('API tokens');
  });
}

// Class 9 and 10 students see their subjects once they have a stream; all of them take Science (Physics, Computer Science).
async function assignStreams(campusCode, mine) {
  const admin = `admin.${keyOf(campusCode)}`;
  const adminId = await userIdOf(admin);
  for (const o of mine.filter(o => o.cls >= 9 && o.subject === 'ENG'))
    for (const s of await get(`${keyOf(campusCode)}.english`, `assignment/offerings/${o.id}/roster`)) await attempt(`stream ${s.registrationNumber}`, async () => {
      const current = await get(admin, `school-streams/student/${s.studentProfileId}`).catch(() => null);
      if (current) return;
      await post(admin, 'school-streams/assign', { studentProfileId: s.studentProfileId, streamId: streams.Science, assignedByUserId: adminId });
      count('stream assignments');
    });
}

// Campuses in parallel; each campus works through its subjects in turn.
const started = Date.now();
await Promise.all(campuses.map(async campusCode => {
  const mine = offerings.filter(o => o.campusCode === campusCode).sort((a, b) => a.cls - b.cls || a.subject.localeCompare(b.subject));
  if (streams.Science) await attempt(`streams ${campusCode}`, () => assignStreams(campusCode, mine));
  const done = [];
  for (const o of mine) {
    const r = await attempt(o.courseCode, () => offeringActivity(o));
    if (r) done.push({ o, ...r });
  }
  await attempt(`outcomes ${campusCode}`, () => outcomes(campusCode, done));
  await attempt(`helpdesk ${campusCode}`, () => campusDesk(campusCode, mine[0]?.departmentId ?? null));
  console.log(`${campusCode}: ${done.length}/${mine.length} subjects done (${Math.round((Date.now() - started) / 1000)} s)`);
}));

console.log(Object.keys(counts).length ? '\nCreated:' : '\nNothing new: everything was already there.');
for (const [k, v] of Object.entries(counts)) console.log(`  ${k}: ${v}`);
if (failures.length) { console.log(`\n${failures.length} problem(s):`); for (const f of failures.slice(0, 40)) console.log('  ' + f); process.exitCode = 1; }
