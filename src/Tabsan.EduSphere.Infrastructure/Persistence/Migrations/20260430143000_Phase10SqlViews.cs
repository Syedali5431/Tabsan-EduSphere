using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10SqlViews : Migration
    {
        /// <summary>
        /// Runs the statement through EXEC: CREATE VIEW must start its batch, and the idempotent migration script
        /// (dotnet ef migrations script --idempotent → Scripts/01-Schema-Current.sql) wraps every statement in IF … BEGIN … END.
        /// </summary>
        private static string Exec(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // View: student attendance summary per offering
            migrationBuilder.Sql(Exec(@"
CREATE VIEW vw_student_attendance_summary AS
SELECT
    ar.StudentProfileId,
    ar.CourseOfferingId,
    COUNT(*) AS TotalSessions,
    SUM(CASE WHEN ar.Status = 'Present' THEN 1 ELSE 0 END) AS AttendedSessions,
    CAST(
        CASE WHEN COUNT(*) = 0 THEN 0.0
             ELSE (SUM(CASE WHEN ar.Status = 'Present' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
        END AS decimal(5,2)
    ) AS AttendancePercentage
FROM attendance_records ar
GROUP BY ar.StudentProfileId, ar.CourseOfferingId;
"));

            // View: student results summary (published only)
            migrationBuilder.Sql(Exec(@"
CREATE VIEW vw_student_results_summary AS
SELECT
    r.StudentProfileId,
    r.CourseOfferingId,
    r.ResultType,
    r.MarksObtained,
    r.MaxMarks,
    CAST(
        CASE WHEN r.MaxMarks = 0 THEN 0.0
             ELSE (CAST(r.MarksObtained AS decimal(10,2)) / r.MaxMarks) * 100.0
        END AS decimal(5,2)
    ) AS Percentage,
    r.PublishedAt,
    co.CourseId,
    c.Code AS CourseCode,
    c.Title AS CourseTitle,
    co.SemesterId
FROM results r
INNER JOIN course_offerings co ON co.Id = r.CourseOfferingId
INNER JOIN courses c ON c.Id = co.CourseId
WHERE r.IsPublished = 1;
"));

            // View: course offering enrollment count and seat availability
            migrationBuilder.Sql(Exec(@"
CREATE VIEW vw_course_enrollment_summary AS
SELECT
    co.Id AS CourseOfferingId,
    co.CourseId,
    c.Code AS CourseCode,
    c.Title AS CourseTitle,
    co.SemesterId,
    co.MaxEnrollment,
    COUNT(e.Id) AS EnrolledCount,
    co.MaxEnrollment - COUNT(e.Id) AS AvailableSeats
FROM course_offerings co
INNER JOIN courses c ON c.Id = co.CourseId
LEFT JOIN enrollments e ON e.CourseOfferingId = co.Id AND e.Status = 'Active'
WHERE co.IsOpen = 1
GROUP BY co.Id, co.CourseId, c.Code, c.Title, co.SemesterId, co.MaxEnrollment;
"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_course_enrollment_summary;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_student_results_summary;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_student_attendance_summary;");
        }
    }
}
