// Demo data for the Plan M features in CS702 Machine Learning (faculty.it1), created through the real API so the user manual's
// screenshots show the features in use (usage: node demo-data.mjs, with the API on :5181 and the demo data set loaded).
// Safe to re-run: existing items are found by title, and steps already done are remembered in out/demo-state.json.
// SCORM packages are not uploaded here: upload them on LMS Manage first (the manual used Rustici's free "Golf Explained"
// samples: course in parts (SCORM 1.2), lessons and quizzes (SCORM 2004) and run-time basics (SCORM 1.2)).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const API = 'http://localhost:5181/api/v1';
const PASSWORD = 'EduSphere147';
const stateFile = path.join(here, 'out', 'demo-state.json');
fs.mkdirSync(path.dirname(stateFile), { recursive: true });
const state = fs.existsSync(stateFile) ? JSON.parse(fs.readFileSync(stateFile, 'utf8')) : {};
const save = () => fs.writeFileSync(stateFile, JSON.stringify(state, null, 1));

const tokens = {};
async function login(user) {
  if (tokens[user]) return tokens[user];
  const r = await fetch(`${API}/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username: user, password: PASSWORD }) });
  if (!r.ok) throw new Error(`login ${user}: ${r.status}`);
  return (tokens[user] = (await r.json()).accessToken);
}
async function call(user, method, url, body, form) {
  const headers = { Authorization: `Bearer ${await login(user)}` };
  let payload;
  if (form) payload = form;
  else if (body !== undefined) { headers['Content-Type'] = 'application/json'; payload = JSON.stringify(body); }
  const r = await fetch(`${API}/${url}`, { method, headers, body: payload });
  const text = await r.text();
  if (!r.ok) throw new Error(`${method} ${url} → ${r.status} ${text.slice(0, 300)}`);
  return text ? JSON.parse(text) : null;
}
const get = (u, url) => call(u, 'GET', url);
const post = (u, url, body) => call(u, 'POST', url, body ?? {});
const put = (u, url, body) => call(u, 'PUT', url, body);
const del = (u, url) => call(u, 'DELETE', url);
const days = n => new Date(Date.now() + n * 86400000).toISOString();

async function step(name, fn) {
  try { const out = await fn(); console.log('ok  ', name, out ?? ''); }
  catch (e) { console.log('FAIL', name, e.message); }
}
const F = 'faculty.it1', ADMIN = 'testadmin', SA = 'superadmin';
const students = ['bscs8s1', 'bscs8s2', 'bscs8s3', 'bscs8s4', 'bscs8s5'];

// The CS702 offering taught by faculty.it1, its course, and the BSCS program.
const cs702 = (await get(F, 'course/offerings')).find(o => o.courseCode === 'CS702');
if (!cs702) throw new Error('CS702 offering not found: load the demo data set first.');
const OFFERING = cs702.id, COURSE = cs702.courseId;
const PROGRAM = (await get(F, 'outcomes/programs')).find(p => p.code === 'BSCS')?.id;

// ── quiz bank and quiz ───────────────────────────────────────────────────────
await step('question bank', async () => {
  const banks = await get(F, 'quiz/banks');
  let bank = (banks.items ?? banks).find?.(b => b.title === 'Machine learning basics');
  if (!bank) {
    state.bankId = (await post(F, 'quiz/banks', { title: 'Machine learning basics', courseOfferingId: OFFERING, isShared: true })).bankId;
    const qs = [
      { text: 'Which of these is a supervised learning task?', type: 'MultipleChoice', marks: 1, options: [['Classifying emails as spam or not spam', true], ['Grouping customers with no labels', false], ['Reducing the number of features', false]], tags: 'supervised', difficulty: 1 },
      { text: 'Select every regularisation method.', type: 'MultipleAnswers', marks: 2, options: [['L1 (lasso)', true], ['L2 (ridge)', true], ['Dropout', true], ['Gradient descent', false]], tags: 'regularisation', difficulty: 2 },
      { text: 'A model that scores 99% on training data and 60% on new data is [[1]].', type: 'FillInBlank', marks: 1, configJson: JSON.stringify({ blanks: [{ id: '1', answers: ['overfitting', 'overfitted'] }] }), tags: 'evaluation', difficulty: 2 },
      { text: 'With 80 true positives and 20 false positives, what is the precision (0 to 1)?', type: 'Numeric', marks: 1, configJson: JSON.stringify({ answer: 0.8, tolerance: 0.001 }), tags: 'evaluation', difficulty: 3 },
      { text: 'k-means needs the number of clusters chosen in advance.', type: 'MultipleChoice', marks: 1, options: [['True', true], ['False', false]], tags: 'unsupervised', difficulty: 1 }
    ];
    for (const q of qs) await post(F, `quiz/banks/${state.bankId}/questions`, { ...q, options: q.options?.map(([text, isCorrect], i) => ({ text, isCorrect, orderIndex: i + 1 })) });
    save();
    return `created ${state.bankId}`;
  }
  state.bankId = bank.bankId ?? bank.id; save(); return 'exists';
});

await step('quiz', async () => {
  const list = await get(F, `quiz/by-offering/${OFFERING}`);
  const existing = (list.items ?? list).find?.(q => q.title === 'Quiz 1: Foundations of machine learning');
  if (existing) { state.quizId = existing.quizId ?? existing.id; save(); return 'exists'; }
  const quizId = (await post(F, 'quiz', { courseOfferingId: OFFERING, title: 'Quiz 1: Foundations of machine learning', instructions: 'Answer every question. You have 20 minutes and two attempts; your best attempt counts.', timeLimitMinutes: 20, maxAttempts: 2, availableFrom: null, availableUntil: days(14) })).quizId;
  state.quizId = quizId; save();
  const add = (text, type, marks, order, options, config) => post(F, 'quiz/question', { quizId, text, type, marks, orderIndex: order, configJson: config ?? null, options: options?.map(([t, c], i) => ({ text: t, isCorrect: c, orderIndex: i + 1 })) });
  await add('What does a learning rate control in gradient descent?', 'MultipleChoice', 2, 1, [['The size of each update step', true], ['The number of layers', false], ['The size of the test set', false]]);
  await add('Match each algorithm to its family.', 'Matching', 3, 2, null, JSON.stringify({ pairs: [{ prompt: 'Decision tree', answer: 'Supervised' }, { prompt: 'k-means', answer: 'Unsupervised' }, { prompt: 'Q-learning', answer: 'Reinforcement' }], distractors: ['Semi-supervised'] }));
  await add('Put the steps of a machine learning project in order.', 'Ordering', 2, 3, null, JSON.stringify({ items: [{ text: 'Collect and clean data' }, { text: 'Split into training and test sets' }, { text: 'Train the model' }, { text: 'Evaluate on the test set' }] }));
  await add('Explain the bias-variance trade-off in your own words, with an example.', 'Essay', 5, 4);
  await post(F, `quiz/${quizId}/groups`, { questionBankId: state.bankId, title: 'Two questions from the bank', pickCount: 2, marksPerQuestion: 2, orderIndex: 5, tagFilter: null });
  await put(F, `quiz/${quizId}/settings`, { shuffleQuestions: false, shuffleOptions: true, oneQuestionAtATime: false, lockAnsweredQuestions: false, accessCode: null, allowedIpRanges: null, reviewVisibility: 'CorrectAnswers', reviewAfterCloseOnly: false });
  await post(F, `quiz/${quizId}/publish`);
  return `created ${quizId}`;
});

await step('quiz attempts', async () => {
  if (state.quizAttempts) return 'done before';
  const detail = await get(F, `quiz/${state.quizId}`);
  const key = Object.fromEntries(detail.questions.map(q => [q.questionId, q]));
  const essays = [
    'High bias means the model is too simple and misses patterns (underfitting); high variance means it memorises noise (overfitting). A straight line through curved data is biased; a very deep tree is high variance.',
    'Bias is error from wrong assumptions, variance is error from sensitivity to the training data. We balance them with cross-validation and regularisation.',
    'A simple model underfits and a complex model overfits; the best model sits in between.',
    'It is about choosing the right model size.'
  ];
  let n = 0;
  for (const s of students.slice(0, 4)) {
    const attempt = await post(s, `quiz/${state.quizId}/start`, { accessCode: null });
    const paper = await get(s, `quiz/attempt/${attempt.attemptId}/paper`);
    const answers = [];
    for (const q of paper.questions) {
      const k = key[q.questionId];
      const right = n !== 3; // the last student gets most answers wrong
      if (q.type === 'MultipleChoice' || q.type === 'MultipleAnswers') {
        const opts = (k?.options ?? q.options ?? []);
        const correct = opts.filter(o => o.isCorrect).map(o => o.optionId);
        const wrong = opts.filter(o => !o.isCorrect).map(o => o.optionId).slice(0, 1);
        if (k) answers.push({ quizQuestionId: q.questionId, selectedOptionIds: right ? correct : wrong });
        else answers.push({ quizQuestionId: q.questionId, selectedOptionIds: [(q.options ?? [])[0]?.optionId].filter(Boolean) });
      } else if (q.type === 'Matching' && k) {
        const pairs = JSON.parse(k.configJson).pairs;
        const matches = Object.fromEntries(pairs.map((p, i) => [p.id, right || i === 0 ? p.answer : 'Semi-supervised']));
        answers.push({ quizQuestionId: q.questionId, responseJson: JSON.stringify({ matches }) });
      } else if (q.type === 'Ordering' && k) {
        const order = JSON.parse(k.configJson).items.map(i => i.id);
        answers.push({ quizQuestionId: q.questionId, responseJson: JSON.stringify({ order: right ? order : [...order].reverse() }) });
      } else if (q.type === 'Essay') answers.push({ quizQuestionId: q.questionId, textResponse: essays[n] });
      else if (q.type === 'FillInBlank') answers.push({ quizQuestionId: q.questionId, responseJson: JSON.stringify({ blanks: { 1: right ? 'overfitting' : 'underfitting' } }) });
      else if (q.type === 'Numeric') answers.push({ quizQuestionId: q.questionId, responseJson: JSON.stringify({ value: right ? '0.8' : '0.2' }) });
    }
    await post(s, 'quiz/attempt/submit', { attemptId: attempt.attemptId, answers });
    n++;
  }
  // Grade the first two essays; the others wait in the grading queue.
  const queue = await get(F, `quiz/${state.quizId}/grading-queue`);
  for (const [i, item] of queue.slice(0, 2).entries())
    await call(F, 'POST', 'quiz/attempt/grade-answer', { answerId: item.answerId, marksAwarded: i === 0 ? 5 : 4, feedback: i === 0 ? 'Clear explanation with a good example.' : 'Good definitions; add an example next time.' });
  state.quizAttempts = true; save();
  return `${n} attempts, ${queue.length} essays queued`;
});

await step('grade essays', async () => {
  if (state.essaysGraded) return 'done before';
  const queue = await get(F, 'quiz/' + state.quizId + '/grading-queue');
  for (const [i, item] of queue.slice(0, 2).entries())
    await call(F, 'POST', 'quiz/attempt/grade-answer', { answerId: item.answerId, marksAwarded: i === 0 ? 5 : 4, feedback: i === 0 ? 'Clear explanation with a good example.' : 'Good definitions; add an example next time.' });
  state.essaysGraded = true; save();
  return 'graded 2 of ' + queue.length;
});

// ── assignments: late policy, options, override, similarity, peer review, groups ─
await step('late policy', () => put(F, `assignment/offerings/${OFFERING}/late-policy`, { lateDeductionEnabled: true, deductionPercent: 10, interval: 'Day', lowestGradePercent: 40, missingEnabled: false, missingGradePercent: 0 }).then(() => '10% per day, floor 40%'));

async function assignment(title, description, due, maxMarks) {
  const list = await get(F, `assignment/by-offering/${OFFERING}`);
  const found = (list.items ?? list).find?.(a => a.title === title);
  if (found) return found.id;
  const id = (await post(F, 'assignment', { courseOfferingId: OFFERING, title, description, dueDate: due, maxMarks })).id;
  await post(F, `assignment/${id}/publish`);
  await put(F, `assignment/${id}/settings`, { allowedAttempts: 2, allowedFileTypes: null, maxFileSizeMb: 10, anonymousGrading: false, postingPolicy: 'Automatic', moderatedGrading: false });
  return id;
}

await step('assignment: literature review', async () => {
  state.reviewId = await assignment('Literature review: model evaluation', 'Write 300-500 words comparing two ways of evaluating a classifier (for example accuracy and F1). Cite at least two sources.', days(5), 20);
  save();
  await put(F, `assignment/${state.reviewId}/options`, { availableFrom: null, availableUntil: days(9), groupSetId: null, gradeGroupIndividually: false, peerReviewCount: 1, peerReviewDueDate: days(8), peerReviewAnonymous: true });
  await put(F, `similarity/assignments/${state.reviewId}/settings`, { enabled: true, visibleToStudents: true });
  const rubrics = await get(F, `rubric/by-assignment/${state.reviewId}`).catch(() => null);
  if (!rubrics || (Array.isArray(rubrics) && rubrics.length === 0) || rubrics?.criteria === undefined && !Array.isArray(rubrics))
    await post(F, 'rubric', { assignmentId: state.reviewId, title: 'Literature review rubric', criteria: [
      { name: 'Understanding', maxPoints: 10, displayOrder: 1, levels: [{ label: 'Limited', pointsAwarded: 3, displayOrder: 1 }, { label: 'Good', pointsAwarded: 7, displayOrder: 2 }, { label: 'Excellent', pointsAwarded: 10, displayOrder: 3 }] },
      { name: 'Use of sources', maxPoints: 10, displayOrder: 2, levels: [{ label: 'None', pointsAwarded: 0, displayOrder: 1 }, { label: 'Some', pointsAwarded: 5, displayOrder: 2 }, { label: 'Well cited', pointsAwarded: 10, displayOrder: 3 }] }
    ] }).catch(e => { if (!String(e.message).includes('already')) throw e; });
  return state.reviewId;
});

await step('extension for bscs8s2', async () => {
  const roster = await get(F, `assignment/offerings/${OFFERING}/roster`);
  state.profiles = Object.fromEntries(roster.map(r => [r.registrationNumber, r.studentProfileId])); save();
  const existing = await get(F, `assignment/${state.reviewId}/overrides`);
  if ((existing.items ?? existing).length) return 'exists';
  await post(F, `assignment/${state.reviewId}/overrides`, { studentProfileId: state.profiles['BSCS-8-02'], dueDate: days(8), availableUntil: days(10), note: 'Medical certificate received' });
  return 'added';
});

const essay = 'Accuracy counts how many predictions are right, but on unbalanced data a model that always predicts the majority class can look accurate while being useless. ' +
  'Precision and recall look at the positive class: precision asks how many predicted positives are real, recall asks how many real positives were found. ' +
  'The F1 score combines them as a harmonic mean, so it is low when either is low. For fraud detection, where fraud is rare, F1 or the area under the precision-recall curve is a better guide than accuracy (Saito and Rehmsmeier, 2015; Powers, 2011).';
await step('submissions', async () => {
  if (state.submitted) return 'done before';
  const texts = [essay, essay.replace('Accuracy counts', 'Accuracy simply counts').replace('useless', 'not useful'),
    'Cross-validation splits the data into k folds and trains k models, each tested on a different fold. This gives a more reliable estimate than one train/test split. A confusion matrix then shows where the model makes mistakes (Kohavi, 1995).',
    'ROC curves plot the true positive rate against the false positive rate at every threshold; the area under the curve summarises how well the model ranks positives above negatives (Fawcett, 2006).'];
  for (const [i, s] of students.slice(0, 4).entries()) await post(s, 'assignment/submit', { assignmentId: state.reviewId, textContent: texts[i] });
  state.submitted = true; save();
  return '4 submissions';
});

await step('peer reviews', async () => {
  if (state.peerAllocated) return 'done before';
  await post(F, `assignment/${state.reviewId}/peer-reviews/allocate`);
  state.peerAllocated = true; save();
  // bscs8s1 completes their review
  const extras = await get('bscs8s1', `assignment/${state.reviewId}/my-extras`);
  const todo = extras.peerReviews ?? extras.reviewsToDo ?? [];
  if (todo[0]) {
    const task = await get('bscs8s1', `assignment/peer-reviews/${todo[0].id}`);
    const rubric = (task.rubric ?? []).map(c => ({ criterionId: c.criterionId, levelId: c.levels[c.levels.length - 1].levelId }));
    await post('bscs8s1', `assignment/peer-reviews/${todo[0].id}`, { score: rubric.length ? null : 16, comment: 'Clear comparison. Add one more source on ROC curves.', rubric: rubric.length ? rubric : undefined });
  }
  return `allocated; bscs8s1 reviews: ${todo.length}`;
});

await step('grade one submission with comments', async () => {
  if (state.graded) return 'done before';
  const subs = await get(F, `assignment/${state.reviewId}/submissions`);
  const first = (subs.items ?? subs).find(s => s.registrationNumber === 'BSCS-8-01' || s.studentProfileId === state.profiles['BSCS-8-01']) ?? (subs.items ?? subs)[0];
  const form = new FormData(); form.append('body', 'Well argued, and the fraud example works. Check the second citation format.'); form.append('isPrivate', 'false');
  await call(F, 'POST', `assignment/submissions/${first.id ?? first.submissionId}/comments`, undefined, form);
  await put(F, 'assignment/submissions/grade', { assignmentId: state.reviewId, studentProfileId: first.studentProfileId, marksAwarded: 17, feedback: 'Strong work: clear and well cited.' });
  state.graded = true; save();
  return 'graded 17/20';
});

await step('group set and team assignment', async () => {
  const sets = await get(F, `assignment/offerings/${OFFERING}/group-sets`);
  let set = (sets.items ?? sets).find?.(s => s.name === 'Project teams');
  if (!set) {
    set = await post(F, `assignment/offerings/${OFFERING}/group-sets`, { name: 'Project teams', selfSignup: false, maxSize: null });
    await post(F, `assignment/group-sets/${set.id}/random-split`, { groupCount: 6, groupSize: null });
  }
  state.groupSetId = set.id; save();
  const teamId = await assignment('Team project plan', 'As a team, agree a dataset and a question, and hand in a one-page plan. One member submits for the team.', days(12), 30);
  await put(F, `assignment/${teamId}/options`, { availableFrom: null, availableUntil: null, groupSetId: set.id, gradeGroupIndividually: false, peerReviewCount: 0, peerReviewDueDate: null, peerReviewAnonymous: true });
  state.teamId = teamId; save();
  const buddies = (sets.items ?? sets).find?.(s => s.name === 'Study buddies');
  if (!buddies) {
    const b = await post(F, `assignment/offerings/${OFFERING}/group-sets`, { name: 'Study buddies', selfSignup: true, maxSize: 3 });
    for (const name of ['Morning group', 'Evening group']) await post(F, `assignment/group-sets/${b.id}/groups`, { name });
  }
  return `set ${set.id}, team assignment ${teamId}`;
});

// ── modules and progress ─────────────────────────────────────────────────────
await step('modules', async () => {
  if (state.modules) return 'done before';
  const packages = await get(F, `scorm/offerings/${OFFERING}/packages`);
  const parts = packages.find(p => p.title.includes('course in parts (SCORM 1.2)'));
  const module = async (title, week, body) => {
    const id = (await post(F, 'lms/module', { offeringId: OFFERING, title, weekNumber: week, body })).id;
    await post(F, `lms/module/${id}/publish`);
    return id;
  };
  const w1 = await module('What is machine learning?', 1, 'Start here. Read the overview, work through the golf course sample, and take Quiz 1.');
  const w2 = await module('Evaluating models', 2, 'Precision, recall and cross-validation, then the literature review.');
  const item = (m, body) => post(F, `lms/modules/${m}/items`, body);
  await item(w1, { itemType: 'Header', title: 'Start here' });
  await item(w1, { itemType: 'Page', title: 'Course overview', body: 'Welcome to CS702. Each week has readings, an activity and a check of your understanding. Items with a requirement count towards your progress.', requirementType: 'MustView' });
  await item(w1, { itemType: 'ExternalUrl', title: 'Reading: A few useful things to know about machine learning', url: 'https://homes.cs.washington.edu/~pedrod/papers/cacm12.pdf', requirementType: 'MustMarkDone' });
  if (parts) await item(w1, { itemType: 'ScormPackage', targetId: parts.id, requirementType: 'MustSubmit' });
  await item(w1, { itemType: 'Quiz', targetId: state.quizId, requirementType: 'MustScoreAtLeast', minScorePercent: 60 });
  await item(w2, { itemType: 'Page', title: 'Notes: precision, recall and F1', body: 'Precision = TP / (TP + FP). Recall = TP / (TP + FN). F1 is their harmonic mean.', requirementType: 'MustView' });
  await item(w2, { itemType: 'Assignment', targetId: state.reviewId, requirementType: 'MustSubmit' });
  await item(w2, { itemType: 'Assignment', targetId: state.teamId, requirementType: 'MustSubmit' });
  await put(F, `lms/modules/${w2}/release`, { requireSequentialProgress: true, completionMode: 'All', prerequisiteModuleIds: [w1] });
  state.modules = [w1, w2]; save();
  return `${w1}, ${w2}`;
});

await step('student progress', async () => {
  if (state.progress) return 'done before';
  for (const s of ['bscs8s1', 'bscs8s2']) {
    const view = await get(s, `lms/offerings/${OFFERING}/my-modules`);
    for (const item of view.modules[0].items) {
      if (item.itemType === 'Page') await post(s, `lms/module-items/${item.id}/open`);
      if (item.itemType === 'ExternalUrl' && s === 'bscs8s1') await post(s, `lms/module-items/${item.id}/done`);
    }
  }
  state.progress = true; save();
  return 'bscs8s1, bscs8s2';
});

// ── outcomes ─────────────────────────────────────────────────────────────────
await step('outcomes', async () => {
  if (state.clo) return 'done before';
  const all = await get(ADMIN, 'outcomes');
  const list = all.items ?? all;
  let plo = list.find?.(o => o.code === 'PLO-3');
  if (!plo) plo = await post(ADMIN, 'outcomes', { level: 'Program', programId: PROGRAM, code: 'PLO-3', title: 'Apply machine learning methods to real problems', calculationMethod: 'Average' });
  const ploId = plo.id ?? plo.outcomeId;
  let clo = list.find?.(o => o.code === 'CS702-CLO1');
  if (!clo) clo = await post(F, 'outcomes', { level: 'Course', courseId: COURSE, code: 'CS702-CLO1', title: 'Evaluate a classifier with suitable metrics', calculationMethod: 'Highest' });
  const cloId = clo.id ?? clo.outcomeId;
  await put(F, `outcomes/${cloId}/mappings`, { programOutcomeIds: [ploId] });
  const clo2 = list.find?.(o => o.code === 'CS702-CLO2') ?? await post(F, 'outcomes', { level: 'Course', courseId: COURSE, code: 'CS702-CLO2', title: 'Explain core machine learning concepts', calculationMethod: 'Average' });
  const clo2Id = clo2.id ?? clo2.outcomeId;
  await put(F, `outcomes/${clo2Id}/mappings`, { programOutcomeIds: [ploId] });
  await put(F, 'outcomes/alignments', { targetType: 'Assignment', targetId: state.reviewId, outcomeIds: [cloId] });
  const detail = await get(F, `quiz/${state.quizId}`);
  for (const q of detail.questions.slice(0, 3)) await put(F, 'outcomes/alignments', { targetType: 'QuizQuestion', targetId: q.questionId, outcomeIds: [clo2Id] });
  await call(F, 'POST', `outcomes/mastery/offerings/${OFFERING}/recalculate`);
  state.clo = cloId; save();
  return `PLO ${ploId}, CLOs ${cloId}, ${clo2Id}`;
});

// ── live class ───────────────────────────────────────────────────────────────
await step('live class settings', () => put(ADMIN, 'live/settings', { provider: 'ExternalLink', isEnabled: true, baseUrl: null, secret: null, accountId: null, clientId: null, organizerUserId: null, joinEarlyMinutes: 15 }).then(r => r.provider));
await step('live class session', async () => {
  const sessions = await get(F, `live/offerings/${OFFERING}/sessions`);
  if ((sessions.items ?? sessions).some?.(s => s.title === 'Live Q&A: Week 1')) return 'exists';
  const start = new Date(Date.now() + 86400000); start.setUTCHours(5, 0, 0, 0);
  await post(F, `live/offerings/${OFFERING}/sessions`, { title: 'Live Q&A: Week 1', description: 'Bring your questions about Quiz 1 and the golf sample course.', startsAt: start.toISOString(), durationMinutes: 45, autoAttendance: true, minimumMinutes: 20, meetingUrl: 'https://meet.jit.si/EduSphere-CS702-Week1' });
  const later = new Date(start.getTime() + 7 * 86400000);
  await post(F, `live/offerings/${OFFERING}/sessions`, { title: 'Live Q&A: Week 2', description: 'Model evaluation clinic.', startsAt: later.toISOString(), durationMinutes: 45, autoAttendance: true, minimumMinutes: 20, meetingUrl: 'https://meet.jit.si/EduSphere-CS702-Week2' });
  return 'two sessions';
});

// ── external tool link ───────────────────────────────────────────────────────
await step('LTI tool on', async () => { const t = (await get(SA, 'lti/tools'))[0]; if (t && !t.isActive) await post(SA, 'lti/tools/' + t.id + '/enable'); return t?.name; });
await step('LTI link', async () => {
  const tools = await get(F, `lti/offerings/${OFFERING}/tools`);
  const tool = (tools.items ?? tools)[0];
  if (!tool) return 'no tool available';
  const links = await get(F, `lti/offerings/${OFFERING}/links`);
  if ((links.items ?? links).some?.(l => l.title === 'Practice lab (LTI)')) return 'exists';
  await post(F, `lti/offerings/${OFFERING}/links`, { toolId: tool.toolId ?? tool.id, title: 'Practice lab (LTI)', presentationTarget: 'window', gradeMaximum: 10 });
  return tool.name;
});

// ── SCORM: keep three of the Golf samples (the others only tested the player) ─────────
await step('SCORM tidy', async () => {
  const packages = await get(F, `scorm/offerings/${OFFERING}/packages`);
  const drop = packages.filter(p => /run-time basics \(SCORM 2004\)|advanced run-time|course in parts \(SCORM 2004\)/.test(p.title));
  for (const p of drop) await del(F, `scorm/packages/${p.id}`);
  return `removed ${drop.length}`;
});

// ── xAPI credentials and an API token ────────────────────────────────────────
await step('xAPI credentials', async () => {
  const creds = await get(ADMIN, 'xapi/credentials');
  if ((creds.items ?? creds).some?.(c => c.name === 'Articulate Rise courses')) return 'exists';
  await post(ADMIN, 'xapi/credentials', { name: 'Articulate Rise courses', offeringId: null });
  return 'created (secret not kept)';
});
await step('API token', async () => {
  const mine = await get(ADMIN, 'api-tokens');
  if ((mine.items ?? mine).some?.(t => t.name === 'Grades export script')) return 'exists';
  await post(ADMIN, 'api-tokens', { name: 'Grades export script', scopes: ['read'], expiresInDays: 90 });
  return 'created (secret not kept)';
});
console.log(JSON.stringify(state));
