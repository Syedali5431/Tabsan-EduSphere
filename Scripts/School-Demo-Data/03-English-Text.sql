-- School demo data in English only: databases filled before 2026-10-31 had an Urdu greeting in the subject announcements
-- and Urdu-script homework, quiz questions and answers for the Urdu subject. This puts the English text in their place.
-- Safe to run more than once (it only changes rows that still hold the old text).
-- Run with UTF-8:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d Tabsan-EduSphere -f 65001 -i 03-English-Text.sql
SET NOCOUNT ON;

-- Subject announcements: "Assalam-o-Alaikum! This term in …" → "This term in …"
UPDATE course_announcements SET Body = STUFF(Body, 1, LEN(N'Assalam-o-Alaikum! '), N'') WHERE Body LIKE N'Assalam-o-Alaikum! %';
PRINT CONCAT(N'Announcements: ', @@ROWCOUNT);

DECLARE @map TABLE (Old nvarchar(400) NOT NULL, New nvarchar(400) NOT NULL);
INSERT INTO @map (Old, New) VALUES
 (N'مضمون "میرا گھر"', N'Essay "My Home"'),
 (N'کہانی لکھیں', N'Write a story'),
 (N'اپنے گھر کے بارے میں آٹھ سے دس جملے لکھیں۔', N'Write eight to ten sentences about your home.'),
 (N'کسی جانور کے بارے میں ایک چھوٹی کہانی لکھیں۔', N'Write a short story about an animal.'),
 (N'میرا گھر بہت خوبصورت ہے۔ اس میں تین کمرے اور ایک صحن ہے۔ صحن میں ایک آم کا درخت ہے۔ میں اپنے امی ابو اور بہن بھائیوں کے ساتھ رہتا ہوں۔ ہم سب مل کر کھانا کھاتے ہیں۔ مجھے اپنا گھر بہت پسند ہے۔',
  N'My home is very beautiful. It has three rooms and a courtyard. There is a mango tree in the courtyard. I live with my parents, brothers and sisters. We all eat together. I love my home.'),
 (N'اردو حروفِ تہجی کا پہلا حرف کون سا ہے؟', N'Which is the first letter of the Urdu alphabet?'),
 (N'"کتاب" کی جمع کیا ہے؟', N'What is the plural of "kitab" (book)?'),
 (N'"دن" کا متضاد کیا ہے؟', N'What is the opposite of "din" (day)?'),
 (N'علامہ اقبال پاکستان کے قومی شاعر ہیں۔', N'Allama Iqbal is the national poet of Pakistan.'),
 (N'ا', N'Alif'), (N'ب', N'Bay'), (N'پ', N'Pay'),
 (N'کتابیں', N'Kitabein'), (N'کتابا', N'Kitaba'), (N'کتابہ', N'Kitabah'),
 (N'رات', N'Night'), (N'صبح', N'Morning'), (N'دوپہر', N'Noon');

-- Whole values
UPDATE q SET Text = m.New FROM quiz_questions q JOIN @map m ON q.Text = m.Old;
PRINT CONCAT(N'Quiz questions: ', @@ROWCOUNT);
UPDATE o SET Text = m.New FROM quiz_options o JOIN @map m ON o.Text = m.Old;
PRINT CONCAT(N'Quiz options: ', @@ROWCOUNT);
UPDATE a SET Description = m.New FROM assignments a JOIN @map m ON a.Description = m.Old;
PRINT CONCAT(N'Assignment descriptions: ', @@ROWCOUNT);
UPDATE s SET TextContent = m.New FROM assignment_submissions s JOIN @map m ON s.TextContent = m.Old;
PRINT CONCAT(N'Submissions: ', @@ROWCOUNT);
UPDATE s SET TextContent = m.New FROM submission_attempts s JOIN @map m ON s.TextContent = m.Old;
PRINT CONCAT(N'Submission attempts: ', @@ROWCOUNT);

-- Titles that contain them ("Homework 1: …", "Questions about Homework 1: …")
DECLARE @old nvarchar(400), @new nvarchar(400);
DECLARE titles CURSOR LOCAL FAST_FORWARD FOR SELECT Old, New FROM @map WHERE Old IN (N'مضمون "میرا گھر"', N'کہانی لکھیں');
OPEN titles;
FETCH NEXT FROM titles INTO @old, @new;
WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE assignments SET Title = REPLACE(Title, @old, @new) WHERE CHARINDEX(@old, Title) > 0;
    UPDATE module_items SET Title = REPLACE(Title, @old, @new) WHERE CHARINDEX(@old, Title) > 0;
    UPDATE discussion_threads SET Title = REPLACE(Title, @old, @new) WHERE CHARINDEX(@old, Title) > 0;
    UPDATE notifications SET Title = REPLACE(Title, @old, @new), Body = REPLACE(Body, @old, @new) WHERE CHARINDEX(@old, Title) > 0 OR CHARINDEX(@old, Body) > 0;
    FETCH NEXT FROM titles INTO @old, @new;
END
CLOSE titles;
DEALLOCATE titles;
PRINT N'Titles updated.';
