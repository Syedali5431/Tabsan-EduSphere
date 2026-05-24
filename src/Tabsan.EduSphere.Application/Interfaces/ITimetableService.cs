using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Service contract for managing timetables and their entries.
/// Admins create, populate, publish, and export timetables.
/// Students and faculty read published timetables.
/// </summary>
public interface ITimetableService
{
    // ── Read ──────────────────────────────────────────────────────────────
    /// <summary>Returns summary list of timetables for a department (admins see drafts too).</summary>
    Task<IList<TimetableSummaryDto>> GetByDepartmentAsync(Guid departmentId, bool publishedOnly, CancellationToken ct = default);

    /// <summary>Returns a full timetable with all entries.</summary>
    Task<TimetableDto> GetByIdAsync(Guid timetableId, CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────
    /// <summary>Creates a new draft timetable.</summary>
    Task<TimetableDto> CreateAsync(CreateTimetableCommand cmd, CancellationToken ct = default);

    /// <summary>Updates the timetable title.</summary>
    Task<TimetableDto> UpdateAsync(Guid timetableId, UpdateTimetableCommand cmd, CancellationToken ct = default);

    /// <summary>Adds a new entry to a timetable.</summary>
    Task<TimetableEntryDto> AddEntryAsync(Guid timetableId, UpsertTimetableEntryCommand cmd, CancellationToken ct = default);

    /// <summary>Updates an existing timetable entry.</summary>
    Task<TimetableEntryDto> UpdateEntryAsync(Guid timetableId, Guid entryId, UpsertTimetableEntryCommand cmd, CancellationToken ct = default);

    /// <summary>Removes a timetable entry.</summary>
    Task DeleteEntryAsync(Guid timetableId, Guid entryId, CancellationToken ct = default);

    /// <summary>Publishes the timetable so it's visible to all department members.</summary>
    Task PublishAsync(Guid timetableId, CancellationToken ct = default);

    /// <summary>Unpublishes (returns timetable to draft mode for editing).</summary>
    Task UnpublishAsync(Guid timetableId, CancellationToken ct = default);

    /// <summary>Soft-deletes the timetable (data preserved, hidden from queries).</summary>
    Task DeleteAsync(Guid timetableId, CancellationToken ct = default);

    // ── Export ────────────────────────────────────────────────────────────
    /// <summary>Generates a .xlsx file and returns it as a byte array.</summary>
    Task<byte[]> ExportExcelAsync(Guid timetableId, CancellationToken ct = default);

    /// <summary>Generates a .pdf file and returns it as a byte array.</summary>
    Task<byte[]> ExportPdfAsync(Guid timetableId, CancellationToken ct = default);

    // ── Teacher Dashboard ─────────────────────────────────────────────────
    /// <summary>
    /// Returns all published timetable slots where the given faculty user is assigned.
    /// Used by the teacher dashboard — filters to only the teacher's own sessions.
    /// </summary>
    Task<IList<TeacherTimetableEntryDto>> GetForTeacherAsync(
        Guid facultyUserId,
        Guid? tenantId,
        Guid? campusId,
        bool includeInactive,
        CancellationToken ct = default);
}
