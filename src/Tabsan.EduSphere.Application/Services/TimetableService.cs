using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Services;

/// <summary>
/// Orchestrates timetable CRUD, publish/unpublish, and delegates export to infrastructure exporters.
/// Title is auto-generated from AcademicProgram.Code + SemesterNumber + EffectiveDate.
/// </summary>
public class TimetableService : ITimetableService
{
    private readonly ITimetableRepository _repo;
    private readonly ITimetableExcelExporter _excelExporter;
    private readonly ITimetablePdfExporter _pdfExporter;

    public TimetableService(
        ITimetableRepository repo,
        ITimetableExcelExporter excelExporter,
        ITimetablePdfExporter pdfExporter)
    {
        _repo = repo;
        _excelExporter = excelExporter;
        _pdfExporter = pdfExporter;
    }

    // ── Read ──────────────────────────────────────────────────────────────

    public async Task<IList<TimetableSummaryDto>> GetByDepartmentAsync(
        Guid departmentId, bool publishedOnly, CancellationToken ct = default)
    {
        IList<Timetable> timetables = publishedOnly
            ? await _repo.GetPublishedByDepartmentAsync(departmentId, ct)
            : await _repo.GetByDepartmentAsync(departmentId, ct);

        return timetables.Select(MapSummary).ToList();
    }

    public async Task<TimetableDto> GetByIdAsync(Guid timetableId, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdWithEntriesAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");
        return MapFull(tt);
    }

    // ── Write ─────────────────────────────────────────────────────────────

    public async Task<TimetableDto> CreateAsync(CreateTimetableCommand cmd, CancellationToken ct = default)
    {
        var tt = new Timetable(
            cmd.DepartmentId, cmd.AcademicProgramId, cmd.SemesterId,
            cmd.SemesterNumber, cmd.EffectiveDate);

        await _repo.AddAsync(tt, ct);
        await _repo.SaveChangesAsync(ct);

        var loaded = await _repo.GetByIdWithEntriesAsync(tt.Id, ct)
            ?? throw new InvalidOperationException("Created timetable could not be reloaded.");
        return MapFull(loaded);
    }

    public async Task<TimetableDto> UpdateAsync(Guid timetableId, UpdateTimetableCommand cmd, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");

        tt.UpdateSchedule(cmd.SemesterNumber, cmd.EffectiveDate);
        _repo.Update(tt);
        await _repo.SaveChangesAsync(ct);

        var loaded = await _repo.GetByIdWithEntriesAsync(tt.Id, ct)
            ?? throw new InvalidOperationException("Updated timetable could not be reloaded.");
        return MapFull(loaded);
    }

    public async Task<TimetableEntryDto> AddEntryAsync(
        Guid timetableId, UpsertTimetableEntryCommand cmd, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");

        var entry = new TimetableEntry(
            tt.Id, cmd.DayOfWeek, cmd.StartTime, cmd.EndTime,
            cmd.SubjectName, cmd.CourseId, cmd.FacultyUserId, cmd.FacultyName,
            cmd.RoomId, cmd.RoomNumber, cmd.BuildingId);

        await _repo.AddEntryAsync(entry, ct);
        await _repo.SaveChangesAsync(ct);

        return MapEntry(entry);
    }

    public async Task<TimetableEntryDto> UpdateEntryAsync(
        Guid timetableId, Guid entryId, UpsertTimetableEntryCommand cmd, CancellationToken ct = default)
    {
        var entry = await _repo.GetEntryByIdAsync(entryId, ct)
            ?? throw new KeyNotFoundException($"Timetable entry {entryId} not found.");

        if (entry.TimetableId != timetableId)
            throw new InvalidOperationException("Entry does not belong to the specified timetable.");

        entry.Update(cmd.DayOfWeek, cmd.StartTime, cmd.EndTime,
                     cmd.SubjectName, cmd.CourseId, cmd.FacultyUserId, cmd.FacultyName,
                     cmd.RoomId, cmd.RoomNumber, cmd.BuildingId);
        _repo.UpdateEntry(entry);
        await _repo.SaveChangesAsync(ct);

        return MapEntry(entry);
    }

    public async Task DeleteEntryAsync(Guid timetableId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await _repo.GetEntryByIdAsync(entryId, ct)
            ?? throw new KeyNotFoundException($"Timetable entry {entryId} not found.");

        if (entry.TimetableId != timetableId)
            throw new InvalidOperationException("Entry does not belong to the specified timetable.");

        _repo.RemoveEntry(entry);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid timetableId, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");

        tt.Publish();
        _repo.Update(tt);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task UnpublishAsync(Guid timetableId, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");

        tt.Unpublish();
        _repo.Update(tt);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid timetableId, CancellationToken ct = default)
    {
        var tt = await _repo.GetByIdAsync(timetableId, ct)
            ?? throw new KeyNotFoundException($"Timetable {timetableId} not found.");

        tt.SoftDelete();
        _repo.Update(tt);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Teacher Dashboard ─────────────────────────────────────────────────

    public async Task<IList<TeacherTimetableEntryDto>> GetForTeacherAsync(
        Guid facultyUserId,
        Guid? tenantId,
        Guid? campusId,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var entries = await _repo.GetTeacherEntriesAsync(facultyUserId, tenantId, campusId, includeInactive, ct);
        return entries.Select(MapTeacherEntry).ToList();
    }

    // ── Export ────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportExcelAsync(Guid timetableId, CancellationToken ct = default)
    {
        var dto = await GetByIdAsync(timetableId, ct);
        return _excelExporter.Export(dto);
    }

    public async Task<byte[]> ExportPdfAsync(Guid timetableId, CancellationToken ct = default)
    {
        var dto = await GetByIdAsync(timetableId, ct);
        return _pdfExporter.Export(dto);
    }

    // ── Mapping ───────────────────────────────────────────────────────────

    private static TimetableSummaryDto MapSummary(Timetable t) => new(
        t.Id,
        t.DepartmentId,
        t.Department?.Name ?? string.Empty,
        t.AcademicProgramId,
        t.AcademicProgram?.Code ?? string.Empty,
        t.SemesterId,
        t.Semester?.Name ?? string.Empty,
        t.SemesterNumber,
        t.EffectiveDate,
        t.GetTitle(),
        t.IsPublished,
        t.PublishedAt
    );

    private static TimetableDto MapFull(Timetable t) => new(
        t.Id,
        t.DepartmentId,
        t.Department?.Name ?? string.Empty,
        t.AcademicProgramId,
        t.AcademicProgram?.Name ?? string.Empty,
        t.AcademicProgram?.Code ?? string.Empty,
        t.SemesterId,
        t.Semester?.Name ?? string.Empty,
        t.SemesterNumber,
        t.EffectiveDate,
        t.GetTitle(),
        t.IsPublished,
        t.PublishedAt,
        t.Entries.Select(MapEntry).ToList(),
        t.CreatedAt,
        t.UpdatedAt
    );

    private static TimetableEntryDto MapEntry(TimetableEntry e) => new(
        e.Id,
        e.DayOfWeek,
        ((DayOfWeek)e.DayOfWeek).ToString(),
        e.StartTime,
        e.EndTime,
        e.CourseId,
        e.SubjectName,
        e.FacultyUserId,
        e.FacultyName,
        e.RoomId,
        e.RoomNumber,
        e.BuildingId,
        e.Building?.Name ?? e.Room?.Building?.Name
    );

    private static TeacherTimetableEntryDto MapTeacherEntry(TimetableEntry e) => new(
        e.TimetableId,
        e.Id,
        e.DayOfWeek,
        ((DayOfWeek)e.DayOfWeek).ToString(),
        e.StartTime,
        e.EndTime,
        e.Timetable?.GetTitle() ?? string.Empty,
        e.Timetable?.AcademicProgram?.Code ?? string.Empty,
        e.Timetable?.Semester?.Name ?? string.Empty,
        e.Timetable?.SemesterNumber ?? 0,
        e.SubjectName,
        e.Timetable?.IsPublished == true,
        e.Building?.Name ?? e.Room?.Building?.Name,
        e.RoomNumber ?? e.Room?.Number
    );
}
