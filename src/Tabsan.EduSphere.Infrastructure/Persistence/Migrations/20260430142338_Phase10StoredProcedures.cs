using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10StoredProcedures : Migration
    {
        /// <summary>
        /// Runs the statement through EXEC: CREATE PROCEDURE must start its batch, and the idempotent migration script
        /// (dotnet ef migrations script --idempotent → Scripts/01-Schema-Current.sql) wraps every statement in IF … BEGIN … END.
        /// </summary>
        private static string Exec(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stored procedure: get students below attendance threshold for a given offering
            migrationBuilder.Sql(Exec(@"
CREATE OR ALTER PROCEDURE sp_get_attendance_below_threshold
    @ThresholdPercent DECIMAL(5,2) = 75.0,
    @CourseOfferingId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ar.StudentProfileId,
        ar.CourseOfferingId,
        COUNT(*) AS TotalSessions,
        SUM(CASE WHEN ar.Status = 'Present' THEN 1 ELSE 0 END) AS AttendedSessions,
        CAST(
            CASE WHEN COUNT(*) = 0 THEN 0.0
                 ELSE (SUM(CASE WHEN ar.Status = 'Present' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
            END AS DECIMAL(5,2)
        ) AS AttendancePercentage
    FROM attendance_records ar
    WHERE (@CourseOfferingId IS NULL OR ar.CourseOfferingId = @CourseOfferingId)
    GROUP BY ar.StudentProfileId, ar.CourseOfferingId
    HAVING
        CASE WHEN COUNT(*) = 0 THEN 0.0
             ELSE (SUM(CASE WHEN ar.Status = 'Present' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
        END < @ThresholdPercent;
END;
"));

            // Stored procedure: recalculate and update CGPA for a student from published results
            migrationBuilder.Sql(Exec(@"
CREATE OR ALTER PROCEDURE sp_recalculate_student_cgpa
    @StudentProfileId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalWeightedMarks DECIMAL(18,4) = 0;
    DECLARE @TotalMaxMarks DECIMAL(18,4) = 0;
    DECLARE @NewCgpa DECIMAL(4,2) = 0;

    SELECT
        @TotalWeightedMarks = SUM(CAST(r.MarksObtained AS DECIMAL(18,4))),
        @TotalMaxMarks = SUM(CAST(r.MaxMarks AS DECIMAL(18,4)))
    FROM results r
    WHERE r.StudentProfileId = @StudentProfileId
      AND r.IsPublished = 1
      AND r.MaxMarks > 0;

    IF @TotalMaxMarks > 0
    BEGIN
        -- Convert percentage to 4.0 GPA scale (proportional mapping: 100% -> 4.0)
        SET @NewCgpa = CAST((@TotalWeightedMarks / @TotalMaxMarks) * 4.0 AS DECIMAL(4,2));
        IF @NewCgpa > 4.0 SET @NewCgpa = 4.0;
    END

    UPDATE student_profiles
    SET Cgpa = @NewCgpa
    WHERE Id = @StudentProfileId;

    SELECT @NewCgpa AS NewCgpa;
END;
"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_recalculate_student_cgpa;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_get_attendance_below_threshold;");
        }
    }
}

