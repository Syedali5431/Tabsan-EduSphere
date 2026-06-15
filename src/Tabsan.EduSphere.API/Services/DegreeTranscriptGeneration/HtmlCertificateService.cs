using System.Text;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Generates professional HTML certificates using the reference designs.
/// Replaces OpenXML DOCX approach with clean HTML + placeholder replacement.
/// </summary>
public sealed class HtmlCertificateService
{
    public string GenerateCompletionCertificate(CompletionData data)
    {
        var html = CompletionTemplate;
        html = ReplaceCommon(html, data);
        html = html.Replace("{{ProgramName}}", data.ProgramName);
        html = html.Replace("{{ClassName}}", data.ClassName);
        html = html.Replace("{{FinalPercentage}}", data.FinalPercentage + "%");
        html = html.Replace("{{EnrollmentYear}}", data.EnrollmentYear);
        html = html.Replace("{{CompletionYear}}", data.CompletionYear);
        return html;
    }

    public string GenerateReportCard(ReportCardData data)
    {
        var html = ReportCardTemplate;
        html = ReplaceCommon(html, data);
        html = html.Replace("{{ProgramName}}", data.ProgramName);
        html = html.Replace("{{ClassName}}", data.ClassName);
        html = html.Replace("{{FinalPercentage}}", data.FinalPercentage + "%");
        html = html.Replace("{{ClassesCompleted}}", data.ClassesCompleted.ToString());
        html = html.Replace("{{SubjectsPassed}}", data.SubjectsPassed.ToString());
        html = html.Replace("{{AttendancePercent}}", data.AttendancePercent + "%");
        html = html.Replace("{{Strengths}}", data.Strengths);
        html = html.Replace("{{Remarks}}", data.Remarks);
        html = html.Replace("{{ACADEMIC_TABLE}}", BuildClasswiseTable(data.ClassRows));
        return html;
    }

    public string GenerateDegree(DegreeData data)
    {
        var html = DegreeTemplate;
        html = ReplaceCommon(html, data);
        html = html.Replace("{{ProgramName}}", data.ProgramName);
        html = html.Replace("{{DegreeTitle}}", data.DegreeTitle);
        html = html.Replace("{{CGPA}}", data.Cgpa);
        html = html.Replace("{{MaxGpa}}", "4.00");
        html = html.Replace("{{Duration}}", data.Duration);
        html = html.Replace("{{FypTitle}}", data.FypTitle);
        html = html.Replace("{{FypGrade}}", data.FypGrade);
        return html;
    }

    public string GenerateTranscript(TranscriptData data)
    {
        var html = TranscriptTemplate;
        html = ReplaceCommon(html, data);
        html = html.Replace("{{ProgramName}}", data.ProgramName);
        html = html.Replace("{{CGPA}}", data.Cgpa);
        html = html.Replace("{{SemestersCount}}", data.SemestersCount.ToString());
        html = html.Replace("{{CoursesPassed}}", data.CoursesPassed.ToString());
        html = html.Replace("{{FypGradePoint}}", data.FypGradePoint);
        html = html.Replace("{{FypMarks}}", data.FypMarks);
        html = html.Replace("{{SEMESTER_TABLE}}", BuildTranscriptTable(data.SemesterRows));
        return html;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static string ReplaceCommon(string html, BaseCertificateData data)
    {
        return html
            .Replace("{{StudentName}}", data.StudentName)
            .Replace("{{RegistrationNumber}}", data.RegistrationNumber)
            .Replace("{{DepartmentName}}", data.DepartmentName)
            .Replace("{{IssueDate}}", data.IssueDate)
            .Replace("{{SerialNumber}}", data.SerialNumber);
    }

    private static string BuildClasswiseTable(List<ClasswiseRow> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            var gradeClass = row.Average >= 90 ? "grade-APlus" : row.Average >= 85 ? "grade-A" : row.Average >= 80 ? "grade-AMinus"
                : row.Average >= 75 ? "grade-BPlus" : row.Average >= 70 ? "grade-B" : row.Average >= 65 ? "grade-BMinus"
                : row.Average >= 60 ? "grade-CPlus" : row.Average >= 55 ? "grade-C" : row.Average >= 50 ? "grade-CMinus"
                : row.Average >= 40 ? "grade-D" : "grade-F";
            sb.AppendLine($"<tr><td><strong>{row.ClassName}</strong></td>" +
                $"<td>{row.English}</td><td>{row.Math}</td><td>{row.Science}</td>" +
                $"<td>{row.SocialStudies}</td><td>{row.Urdu}</td>" +
                $"<td>{row.Average:F1}</td><td class=\"{gradeClass}\">{row.Grade}</td>" +
                $"<td>{row.Attendance}%</td><td>✅ Passed</td></tr>");
        }
        return sb.ToString();
    }

    private static string BuildTranscriptTable(List<TranscriptSemester> semesters)
    {
        var sb = new StringBuilder();
        foreach (var sem in semesters)
        {
            sb.AppendLine($"<tr class=\"sem-header\"><td colspan=\"9\">📖 {sem.SemesterName} (GPA: {sem.SemesterGpa})</td></tr>");
            foreach (var course in sem.Courses)
            {
                sb.AppendLine($"<tr><td>{sem.SemesterLabel}</td><td class=\"course-col\">{course.Code}</td>" +
                    $"<td class=\"course-col\">{course.Title}</td><td>{course.Credits}</td>" +
                    $"<td>{course.Marks}</td><td>{course.MaxMarks}</td><td>{course.Percent}%</td>" +
                    $"<td>{course.Grade}</td><td>{course.GradePoint}</td></tr>");
            }
        }
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Data classes
    // ═══════════════════════════════════════════════════════════════════════

    public class BaseCertificateData
    {
        public string StudentName { get; set; } = "";
        public string RegistrationNumber { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string SerialNumber { get; set; } = "";
    }

    public class CompletionData : BaseCertificateData
    {
        public string ProgramName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string FinalPercentage { get; set; } = "";
        public string EnrollmentYear { get; set; } = "";
        public string CompletionYear { get; set; } = "";
    }

    public class ReportCardData : BaseCertificateData
    {
        public string ProgramName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string FinalPercentage { get; set; } = "";
        public int ClassesCompleted { get; set; }
        public int SubjectsPassed { get; set; }
        public string AttendancePercent { get; set; } = "";
        public string Strengths { get; set; } = "";
        public string Remarks { get; set; } = "";
        public List<ClasswiseRow> ClassRows { get; set; } = new();
    }

    public class DegreeData : BaseCertificateData
    {
        public string ProgramName { get; set; } = "";
        public string DegreeTitle { get; set; } = "";
        public string Cgpa { get; set; } = "";
        public string Duration { get; set; } = "";
        public string FypTitle { get; set; } = "";
        public string FypGrade { get; set; } = "";
    }

    public class TranscriptData : BaseCertificateData
    {
        public string ProgramName { get; set; } = "";
        public string Cgpa { get; set; } = "";
        public int SemestersCount { get; set; }
        public int CoursesPassed { get; set; }
        public string FypGradePoint { get; set; } = "";
        public string FypMarks { get; set; } = "";
        public List<TranscriptSemester> SemesterRows { get; set; } = new();
    }

    public class ClasswiseRow
    {
        public string ClassName { get; set; } = "";
        public int English { get; set; }
        public int Math { get; set; }
        public int Science { get; set; }
        public int SocialStudies { get; set; }
        public int Urdu { get; set; }
        public decimal Average { get; set; }
        public string Grade { get; set; } = "";
        public int Attendance { get; set; }
    }

    public class TranscriptSemester
    {
        public string SemesterName { get; set; } = "";
        public string SemesterLabel { get; set; } = "";
        public string SemesterGpa { get; set; } = "";
        public List<TranscriptCourse> Courses { get; set; } = new();
    }

    public class TranscriptCourse
    {
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int Credits { get; set; }
        public int Marks { get; set; }
        public int MaxMarks { get; set; }
        public int Percent { get; set; }
        public string Grade { get; set; } = "";
        public string GradePoint { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HTML Templates
    // ═══════════════════════════════════════════════════════════════════════

    private const string CompletionTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><title>Completion Certificate — {{StudentName}}</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Great+Vibes&family=Playfair+Display:ital,wght@0,400;0,700;1,400&display=swap');
  *{margin:0;padding:0;box-sizing:border-box}
  body{background:#f0f2f5;display:flex;justify-content:center;align-items:center;min-height:100vh;font-family:'Playfair Display',serif}
  .certificate{width:900px;max-width:95vw;background:linear-gradient(135deg,#fff 0%,#fafbfc 100%);border:8px double #1a3a5c;padding:50px 60px;position:relative;box-shadow:0 10px 40px rgba(0,0,0,0.15)}
  .certificate::before{content:'';position:absolute;top:12px;left:12px;right:12px;bottom:12px;border:2px solid #c9a84c;pointer-events:none}
  .header{text-align:center;margin-bottom:30px}
  .logo{font-size:14px;color:#666;letter-spacing:3px;text-transform:uppercase;margin-bottom:5px}
  .school-name{font-size:32px;font-weight:700;color:#1a3a5c;letter-spacing:1px}
  .badge{display:inline-block;background:linear-gradient(135deg,#c9a84c,#e6c85c);color:#fff;padding:8px 30px;border-radius:30px;font-size:13px;letter-spacing:3px;text-transform:uppercase;margin:20px 0}
  .title{font-family:'Great Vibes',cursive;font-size:48px;color:#1a3a5c;text-align:center;margin:15px 0 5px}
  .subtitle{text-align:center;font-size:16px;color:#555;margin-bottom:30px;letter-spacing:1px}
  .body-text{text-align:center;font-size:16px;line-height:2;color:#333;margin-bottom:25px}
  .student-name{font-family:'Great Vibes',cursive;font-size:42px;color:#1a3a5c;display:block;margin:8px 0}
  .details{text-align:center;margin:20px 0 30px}
  .details span{display:inline-block;margin:0 20px;font-size:14px;color:#555}
  .details strong{color:#1a3a5c}
  .signatures{display:flex;justify-content:space-between;margin-top:50px;padding-top:40px;border-top:1px solid #e0e0e0}
  .sig-block{text-align:center;flex:1}
  .sig-line{width:180px;margin:0 auto 8px;border-bottom:2px solid #1a3a5c}
  .sig-name{font-size:14px;color:#333;font-weight:600}
  .sig-role{font-size:12px;color:#888}
  .seal{position:absolute;bottom:25px;right:50px;width:90px;height:90px;border:3px solid #c9a84c;border-radius:50%;display:flex;align-items:center;justify-content:center;text-align:center;font-size:10px;color:#c9a84c;transform:rotate(-15deg);opacity:0.7;line-height:1.3}
  .date{text-align:center;font-size:13px;color:#888;margin-top:15px}
  @media print{body{background:#fff}.certificate{box-shadow:none;border:8px double #1a3a5c}}
</style></head>
<body><div class=""certificate"">
<div class=""header""><div class=""logo"">★ Tabsan EduSphere ★</div><div class=""school-name"">{{DepartmentName}}</div></div>
<div style=""text-align:center""><span class=""badge"">Certificate of Completion</span></div>
<div class=""title"">Completion Certificate</div>
<div class=""subtitle"">This is proudly presented to</div>
<div class=""body-text"">
<span class=""student-name"">{{StudentName}}</span>
Registration No: <strong>{{RegistrationNumber}}</strong><br>
for successfully completing the <strong>{{ProgramName}}</strong><br>
from <strong>{{ClassName}}</strong><br>
with an Overall Percentage of<br>
<strong style=""font-size:20px"">{{FinalPercentage}}</strong>
</div>
<div class=""details"">
<span>📅 Enrollment: <strong>{{EnrollmentYear}}</strong></span>
<span>🎓 Completion: <strong>{{CompletionYear}}</strong></span>
<span>📊 Overall: <strong>{{FinalPercentage}}</strong></span>
</div>
<div style=""text-align:center;font-size:14px;color:#555;margin:15px 0"">Has demonstrated consistent academic performance and discipline throughout the school journey.</div>
<div class=""signatures"">
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Dr. Sarah Khan</div><div class=""sig-role"">Principal</div></div>
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Mr. Bilal Ahmed</div><div class=""sig-role"">Class Teacher</div></div>
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Mrs. Fatima Noor</div><div class=""sig-role"">Examination Controller</div></div>
</div>
<div class=""date"">Issued on: {{IssueDate}} &nbsp;|&nbsp; Certificate No: {{SerialNumber}}</div>
<div class=""seal"">TABSAN<br>SCHOOL<br>SEAL<br>★</div>
</div></body></html>";

    private const string ReportCardTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><title>Report Card — {{StudentName}}</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700&family=Inter:wght@400;500;600&display=swap');
  *{margin:0;padding:0;box-sizing:border-box}
  body{background:#f5f6fa;display:flex;justify-content:center;min-height:100vh;font-family:'Inter',sans-serif;padding:20px}
  .report-card{width:1000px;max-width:100%;background:#fff;border-radius:8px;box-shadow:0 4px 20px rgba(0,0,0,0.1);overflow:hidden}
  .rc-header{background:linear-gradient(135deg,#1a3a5c,#2c5282);color:#fff;padding:30px 40px;display:flex;justify-content:space-between;align-items:center}
  .rc-header h1{font-family:'Playfair Display',serif;font-size:26px}
  .rc-header .badge{background:#c9a84c;padding:6px 16px;border-radius:20px;font-size:12px;letter-spacing:2px;text-transform:uppercase}
  .student-info{display:grid;grid-template-columns:repeat(4,1fr);gap:15px;padding:25px 40px;background:#fafbfc;border-bottom:1px solid #e8ecf1}
  .info-item{}
  .info-label{font-size:11px;color:#888;text-transform:uppercase;letter-spacing:1px}
  .info-value{font-size:15px;color:#1a3a5c;font-weight:600;margin-top:3px}
  .section-title{padding:15px 40px;font-family:'Playfair Display',serif;font-size:18px;color:#1a3a5c;background:#f0f4f8;border-bottom:2px solid #c9a84c}
  table{width:100%;border-collapse:collapse}
  th{background:#1a3a5c;color:#fff;font-size:12px;padding:10px 12px;text-align:left;letter-spacing:.5px}
  td{padding:10px 12px;font-size:13px;border-bottom:1px solid #e8ecf1}
  tr:nth-child(even){background:#fafbfc}
  tr:hover{background:#eef2f7}
  .grade-APlus{color:#1b5e20;font-weight:700}
  .grade-A{color:#2e7d32;font-weight:700}
  .grade-AMinus{color:#388e3c;font-weight:700}
  .grade-BPlus{color:#1565c0;font-weight:700}
  .grade-B{color:#1976d2;font-weight:700}
  .grade-BMinus{color:#1e88e5;font-weight:700}
  .grade-CPlus{color:#e65100;font-weight:700}
  .grade-C{color:#ef6c00;font-weight:700}
  .grade-CMinus{color:#f57c00;font-weight:700}
  .grade-D{color:#c62828;font-weight:700}
  .grade-F{color:#b71c1c;font-weight:700}
  .summary-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:20px;padding:25px 40px}
  .summary-card{text-align:center;padding:20px;background:#f0f4f8;border-radius:8px;border:1px solid #e0e4ea}
  .summary-card .value{font-size:28px;font-weight:700;color:#1a3a5c;font-family:'Playfair Display',serif}
  .summary-card .label{font-size:11px;color:#888;text-transform:uppercase;letter-spacing:1px;margin-top:5px}
  .footer{padding:20px 40px;text-align:center;font-size:11px;color:#aaa;border-top:1px solid #e8ecf1}
  @media print{body{background:#fff;padding:0}.report-card{box-shadow:none}th{background:#1a3a5c!important;color:#fff!important;-webkit-print-color-adjust:exact}}
</style></head>
<body><div class=""report-card"">
<div class=""rc-header""><div><h1>{{DepartmentName}}</h1><div style=""font-size:13px;opacity:0.85;margin-top:4px"">Academic Report Card — {{ProgramName}}</div></div><span class=""badge"">Report Card</span></div>
<div class=""student-info"">
<div class=""info-item""><div class=""info-label"">Student Name</div><div class=""info-value"">{{StudentName}}</div></div>
<div class=""info-item""><div class=""info-label"">Registration No</div><div class=""info-value"">{{RegistrationNumber}}</div></div>
<div class=""info-item""><div class=""info-label"">Program</div><div class=""info-value"">{{ProgramName}}</div></div>
<div class=""info-item""><div class=""info-label"">Class / Year</div><div class=""info-value"">{{ClassName}}</div></div>
</div>
<div class=""summary-grid"">
<div class=""summary-card""><div class=""value"">{{FinalPercentage}}</div><div class=""label"">Final Percentage</div></div>
<div class=""summary-card""><div class=""value"">{{ClassesCompleted}}</div><div class=""label"">Classes Completed</div></div>
<div class=""summary-card""><div class=""value"">{{SubjectsPassed}}</div><div class=""label"">Subjects Passed</div></div>
<div class=""summary-card""><div class=""value"">{{AttendancePercent}}</div><div class=""label"">Attendance</div></div>
</div>
<div class=""section-title"">📚 Class-wise Performance Summary</div>
<div style=""overflow-x:auto""><table><thead><tr><th>Class</th><th>English</th><th>Mathematics</th><th>Science</th><th>Social Studies</th><th>Urdu</th><th>Average</th><th>Grade</th><th>Attendance</th><th>Status</th></tr></thead><tbody>{{ACADEMIC_TABLE}}</tbody></table></div>
<div class=""section-title"">📋 Overall Assessment</div>
<div style=""padding:20px 40px;display:grid;grid-template-columns:1fr 1fr;gap:15px"">
<div style=""font-size:13px;line-height:1.8""><strong>🏆 Strengths:</strong><br>{{Strengths}}</div>
<div style=""font-size:13px;line-height:1.8""><strong>📝 Remarks:</strong><br>{{Remarks}}</div>
</div>
<div style=""display:flex;justify-content:space-between;padding:25px 40px;border-top:1px solid #e8ecf1;margin-top:10px"">
<div style=""text-align:center""><div style=""border-bottom:2px solid #1a3a5c;width:160px;margin:0 auto 5px""></div><div style=""font-size:13px;font-weight:600"">Dr. Sarah Khan</div><div style=""font-size:11px;color:#888"">Principal</div></div>
<div style=""text-align:center""><div style=""border-bottom:2px solid #1a3a5c;width:160px;margin:0 auto 5px""></div><div style=""font-size:13px;font-weight:600"">Mr. Bilal Ahmed</div><div style=""font-size:11px;color:#888"">Class Teacher</div></div>
<div style=""text-align:center""><div style=""border-bottom:2px solid #1a3a5c;width:160px;margin:0 auto 5px""></div><div style=""font-size:13px;font-weight:600"">Mrs. Fatima Noor</div><div style=""font-size:11px;color:#888"">Exam Controller</div></div>
</div>
<div class=""footer"">Generated by Tabsan EduSphere &nbsp;|&nbsp; {{IssueDate}} &nbsp;|&nbsp; {{SerialNumber}}<br>This is a computer-generated official document.</div>
</div></body></html>";

    private const string DegreeTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><title>Degree — {{StudentName}}</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Great+Vibes&family=Playfair+Display:ital,wght@0,400;0,700;1,400&display=swap');
  *{margin:0;padding:0;box-sizing:border-box}
  body{background:#e8ecf1;display:flex;justify-content:center;align-items:center;min-height:100vh;font-family:'Playfair Display',serif}
  .degree{width:950px;max-width:95vw;background:linear-gradient(135deg,#fdfcf8 0%,#f7f3e8 50%,#fdfcf8 100%);border:10px double #8b0000;padding:50px 60px;position:relative;box-shadow:0 10px 50px rgba(0,0,0,0.2)}
  .degree::before{content:'';position:absolute;top:16px;left:16px;right:16px;bottom:16px;border:1px solid #c9a84c;pointer-events:none}
  .crest{text-align:center;margin-bottom:10px;font-size:40px;color:#8b0000}
  .header{text-align:center;margin-bottom:25px}
  .uni-name{font-size:28px;font-weight:700;color:#8b0000;letter-spacing:2px;text-transform:uppercase}
  .uni-sub{font-size:13px;color:#666;letter-spacing:4px;text-transform:uppercase;margin-top:4px}
  .ornament{text-align:center;color:#c9a84c;font-size:24px;margin:15px 0;letter-spacing:8px}
  .title{font-family:'Great Vibes',cursive;font-size:52px;color:#1a1a1a;text-align:center;margin:10px 0}
  .body-text{text-align:center;font-size:16px;line-height:2.2;color:#333;margin:20px 0}
  .student-name{font-family:'Great Vibes',cursive;font-size:46px;color:#8b0000;display:block;margin:5px 0}
  .degree-name{font-size:22px;font-weight:700;color:#1a1a1a;letter-spacing:1px}
  .details-row{display:flex;justify-content:center;gap:30px;margin:20px 0;font-size:14px;color:#555}
  .details-row span{padding:8px 18px;background:#faf7f0;border:1px solid #e0d8c8;border-radius:4px}
  .details-row strong{color:#8b0000}
  .honors{text-align:center;font-size:15px;color:#8b0000;font-style:italic;margin:15px 0}
  .signatures{display:flex;justify-content:space-between;margin-top:40px;padding-top:30px;border-top:2px solid #c9a84c}
  .sig-block{text-align:center;flex:1}
  .sig-line{width:180px;margin:0 auto 8px;border-bottom:2px solid #333}
  .sig-name{font-size:14px;color:#333;font-weight:600}
  .sig-role{font-size:11px;color:#888;margin-top:2px}
  .seal{position:absolute;bottom:30px;right:60px;width:100px;height:100px;border:4px solid #8b0000;border-radius:50%;display:flex;align-items:center;justify-content:center;text-align:center;font-size:10px;color:#8b0000;transform:rotate(-12deg);line-height:1.3;font-weight:700}
  .date{text-align:center;font-size:13px;color:#888;margin-top:20px}
  @media print{body{background:#fff}.degree{box-shadow:none}}
</style></head>
<body><div class=""degree"">
<div class=""crest"">🏛️</div>
<div class=""header""><div class=""uni-name"">{{DepartmentName}}</div><div class=""uni-sub"">{{ProgramName}}</div></div>
<div class=""ornament"">◆ ◇ ◆</div>
<div class=""title"">Degree of Bachelor</div>
<div class=""body-text"">This is to certify that<br><span class=""student-name"">{{StudentName}}</span>
has been awarded the degree of<br><span class=""degree-name"">{{DegreeTitle}}</span>
having completed all prescribed requirements<br>with a Cumulative Grade Point Average of<br>
<strong style=""font-size:22px;color:#8b0000"">{{CGPA}} / {{MaxGpa}}</strong></div>
<div class=""details-row"">
<span>🎓 <strong>{{ProgramName}}</strong></span><span>📅 <strong>{{Duration}}</strong></span><span>📊 <strong>CGPA {{CGPA}}</strong></span><span>🏆 <strong>FYP: {{FypGrade}}</strong></span>
</div><div class=""honors"">""{{FypTitle}}"" — Final Year Project</div>
<div class=""signatures"">
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Prof. Dr. Hassan Raza</div><div class=""sig-role"">Vice Chancellor</div></div>
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Dr. Ayesha Siddiqui</div><div class=""sig-role"">Dean</div></div>
<div class=""sig-block""><div class=""sig-line""></div><div class=""sig-name"">Mr. Kamran Ali</div><div class=""sig-role"">Controller of Examinations</div></div>
</div>
<div class=""date"">Conferred on: {{IssueDate}} &nbsp;|&nbsp; Degree No: {{SerialNumber}}</div>
<div class=""seal"">TABSAN<br>UNIVERSITY<br>★<br>SEAL</div>
</div></body></html>";

    private const string TranscriptTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><title>Official Transcript — {{StudentName}}</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700&family=Inter:wght@400;500;600&display=swap');
  *{margin:0;padding:0;box-sizing:border-box}
  body{background:#f5f6fa;display:flex;justify-content:center;min-height:100vh;font-family:'Inter',sans-serif;padding:20px}
  .transcript{width:1050px;max-width:100%;background:#fff;border-radius:4px;box-shadow:0 4px 20px rgba(0,0,0,0.1);overflow:hidden}
  .t-header{background:linear-gradient(135deg,#1a1a2e,#16213e);color:#fff;padding:25px 40px;display:flex;justify-content:space-between;align-items:center}
  .t-header h1{font-family:'Playfair Display',serif;font-size:24px}
  .t-header .badge{background:#c9a84c;padding:5px 14px;border-radius:4px;font-size:11px;letter-spacing:2px}
  .student-info{display:grid;grid-template-columns:repeat(5,1fr);gap:12px;padding:20px 40px;background:#fafbfc;border-bottom:2px solid #c9a84c}
  .info-label{font-size:10px;color:#888;text-transform:uppercase;letter-spacing:1px}
  .info-value{font-size:14px;color:#1a1a2e;font-weight:600;margin-top:2px}
  .section-title{padding:12px 40px;font-family:'Playfair Display',serif;font-size:16px;color:#1a1a2e;background:#f0f2f5;border-bottom:1px solid #ddd}
  table{width:100%;border-collapse:collapse;font-size:11px}
  th{background:#1a1a2e;color:#c9a84c;font-size:10px;padding:8px 6px;text-align:center;letter-spacing:.5px}
  td{padding:7px 6px;text-align:center;border-bottom:1px solid #eee}
  tr:nth-child(even){background:#fafbfc}
  .course-col{text-align:left}
  .sem-header{background:#e8ecf1;font-weight:700;font-size:12px}
  .sem-header td{padding:6px 8px;text-align:left;color:#1a1a2e}
  .gpa-row{background:#fef9e7;font-weight:600}
  .fyp-row{background:#e8f5e9}
  .summary-grid{display:grid;grid-template-columns:repeat(5,1fr);gap:15px;padding:20px 40px;background:#1a1a2e;color:#fff}
  .summary-card{text-align:center;padding:12px;background:rgba(255,255,255,0.08);border-radius:4px}
  .summary-card .value{font-size:24px;font-weight:700;font-family:'Playfair Display',serif}
  .summary-card .label{font-size:10px;opacity:0.7;text-transform:uppercase;letter-spacing:1px;margin-top:4px}
  .footer{padding:15px 40px;text-align:center;font-size:10px;color:#aaa;border-top:1px solid #e8ecf1}
  .official{text-align:center;padding:10px;color:#8b0000;font-size:12px;font-weight:600;letter-spacing:2px}
  @media print{body{background:#fff;padding:0}.transcript{box-shadow:none}th{background:#1a1a2e!important;color:#c9a84c!important;-webkit-print-color-adjust:exact}.t-header{-webkit-print-color-adjust:exact}.summary-grid{-webkit-print-color-adjust:exact}}
</style></head>
<body><div class=""transcript"">
<div class=""t-header""><div><h1>{{DepartmentName}}</h1><div style=""font-size:12px;opacity:0.8"">{{ProgramName}}</div></div><div style=""text-align:right""><span class=""badge"">OFFICIAL TRANSCRIPT</span><div style=""font-size:11px;opacity:0.7;margin-top:6px"">{{SerialNumber}}</div></div></div>
<div class=""student-info"">
<div><div class=""info-label"">Student Name</div><div class=""info-value"">{{StudentName}}</div></div>
<div><div class=""info-label"">Registration No</div><div class=""info-value"">{{RegistrationNumber}}</div></div>
<div><div class=""info-label"">Program</div><div class=""info-value"">{{ProgramName}}</div></div>
<div><div class=""info-label"">Department</div><div class=""info-value"">{{DepartmentName}}</div></div>
<div><div class=""info-label"">Period</div><div class=""info-value"">{{Duration}}</div></div>
</div>
<div class=""summary-grid"">
<div class=""summary-card""><div class=""value"">{{CGPA}}</div><div class=""label"">Cumulative GPA</div></div>
<div class=""summary-card""><div class=""value"">{{SemestersCount}}</div><div class=""label"">Semesters</div></div>
<div class=""summary-card""><div class=""value"">{{CoursesPassed}}</div><div class=""label"">Courses Passed</div></div>
<div class=""summary-card""><div class=""value"">{{FypGradePoint}}</div><div class=""label"">FYP Grade Point</div></div>
<div class=""summary-card""><div class=""value"">{{FypMarks}}</div><div class=""label"">FYP Marks</div></div>
</div>
<div class=""section-title"">📚 Semester-wise Academic Record</div>
<table><thead><tr><th>Semester</th><th class=""course-col"">Course Code</th><th class=""course-col"">Course Title</th><th>Credits</th><th>Marks</th><th>Max</th><th>%</th><th>Grade</th><th>GP</th></tr></thead><tbody>{{SEMESTER_TABLE}}</tbody></table>
<div class=""section-title"">📋 Grading Scale</div>
<div style=""padding:10px 40px;display:flex;gap:15px;font-size:11px;flex-wrap:wrap""><span>A = 4.0 (90-100%)</span><span>A- = 3.7 (85-89%)</span><span>B+ = 3.3 (80-84%)</span><span>B = 3.0 (75-79%)</span><span>B- = 2.7 (70-74%)</span><span>C+ = 2.3 (65-69%)</span></div>
<div class=""official"">🔒 OFFICIAL TRANSCRIPT — {{DepartmentName}}</div>
<div class=""footer"">Generated by Tabsan EduSphere &nbsp;|&nbsp; {{IssueDate}} &nbsp;|&nbsp; {{SerialNumber}}<br>This is a computer-generated official transcript.</div>
</div></body></html>";
}
