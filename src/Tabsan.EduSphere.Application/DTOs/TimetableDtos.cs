namespace Tabsan.EduSphere.Application.Dtos;

// =============================================================================
// Building & Room DTOs
// =============================================================================

/// <summary>Building catalogue item returned by API.</summary>
public record BuildingDto(
    Guid   Id,
    string Name,
    string Code,
    bool   IsActive
);

/// <summary>Payload to create a building.</summary>
public record CreateBuildingCommand(
    string Name,
    string Code
);

/// <summary>Payload to update a building.</summary>
public record UpdateBuildingCommand(
    string Name,
    string Code
);

/// <summary>Room catalogue item returned by API (includes parent building info).</summary>
public record RoomDto(
    Guid   Id,
    Guid   BuildingId,
    string BuildingName,
    string BuildingCode,
    string Number,
    int?   Capacity,
    bool   IsActive
);

/// <summary>Payload to create a room within a building.</summary>
public record CreateRoomCommand(
    Guid   BuildingId,
    string Number,
    int?   Capacity
);

/// <summary>Payload to update a room.</summary>
public record UpdateRoomCommand(
    string Number,
    int?   Capacity
);

// =============================================================================
// Timetable DTOs
// =============================================================================

/// <summary>A single scheduled slot in a timetable.</summary>
public record TimetableEntryDto(
    Guid    Id,
    int     DayOfWeek,
    string  DayName,           // "Monday", "Tuesday" etc. — computed in service
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid?   CourseId,
    string  SubjectName,       // Course.Title snapshot or free-text
    Guid?   FacultyUserId,
    string? FacultyName,       // User display name snapshot
    Guid?   RoomId,
    string? RoomNumber,        // Room.Number snapshot
    Guid?   BuildingId,
    string? BuildingName       // Building.Name snapshot
);

/// <summary>A full timetable including all entries.</summary>
public record TimetableDto(
    Guid   Id,
    Guid   DepartmentId,
    string DepartmentName,
    Guid   AcademicProgramId,
    string ProgramName,        // AcademicProgram.Name
    string ProgramCode,        // AcademicProgram.Code (e.g. "BSCS")
    Guid   SemesterId,
    string SemesterName,
    int    SemesterNumber,
    DateTime EffectiveDate,
    string Title,              // Auto-generated: "Timetable for 5th Semester of BSCS. Effective 01-Sep-2025"
    bool   IsPublished,
    DateTime? PublishedAt,
    IList<TimetableEntryDto> Entries,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Lightweight timetable summary without entries.</summary>
public record TimetableSummaryDto(
    Guid   Id,
    Guid   DepartmentId,
    string DepartmentName,
    Guid   AcademicProgramId,
    string ProgramCode,
    Guid   SemesterId,
    string SemesterName,
    int    SemesterNumber,
    DateTime EffectiveDate,
    string Title,
    bool   IsPublished,
    DateTime? PublishedAt
);

/// <summary>Payload to create a new timetable. Title is auto-generated from programme + semester + date.</summary>
public record CreateTimetableCommand(
    Guid     DepartmentId,
    Guid     AcademicProgramId,
    Guid     SemesterId,
    int      SemesterNumber,
    DateTime EffectiveDate
);

/// <summary>Payload to update the schedule metadata of an existing timetable.</summary>
public record UpdateTimetableCommand(
    int      SemesterNumber,
    DateTime EffectiveDate
);

/// <summary>Payload to add or update a timetable entry using FK-based dropdown selection.</summary>
public record UpsertTimetableEntryCommand(
    int      DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string   SubjectName,   // Required — used as fallback display if CourseId is null
    Guid?    CourseId,
    Guid?    FacultyUserId,
    string?  FacultyName,   // Fallback display if FacultyUserId is null
    Guid?    RoomId,
    string?  RoomNumber,    // Fallback display if RoomId is null
    Guid?    BuildingId
);

/// <summary>
/// Teacher-dashboard view of a single timetable slot.
/// Shows only the details relevant to the faculty member (no other teachers visible).
/// </summary>
public record TeacherTimetableEntryDto(
    Guid     TimetableId,
    Guid     EntryId,
    int      DayOfWeek,
    string   DayName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string   TimetableTitle,    // Auto-generated title of the parent timetable
    string   ProgramCode,       // e.g. "BSCS"
    string   SemesterName,      // e.g. "Fall 2025"
    int      SemesterNumber,
    string   SubjectName,
    bool     IsActive,
    string?  BuildingName,
    string?  RoomNumber
);
