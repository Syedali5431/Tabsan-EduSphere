using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// Phase 26 — School and College Functional Expansion

public class SchoolStreamRepository : ISchoolStreamRepository
{
    private readonly ApplicationDbContext _db;

    public SchoolStreamRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SchoolStream>> GetAllStreamsAsync(CancellationToken ct = default)
        => await _db.SchoolStreams.OrderBy(s => s.Name).ToListAsync(ct);

    public Task<SchoolStream?> GetStreamByIdAsync(Guid id, CancellationToken ct = default)
        => _db.SchoolStreams.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task AddStreamAsync(SchoolStream stream, CancellationToken ct = default)
        => _db.SchoolStreams.AddAsync(stream, ct).AsTask();

    public void UpdateStream(SchoolStream stream)
        => _db.SchoolStreams.Update(stream);

    public Task<StudentStreamAssignment?> GetStudentAssignmentAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.StudentStreamAssignments.FirstOrDefaultAsync(x => x.StudentProfileId == studentProfileId, ct);

    public async Task UpsertStudentAssignmentAsync(StudentStreamAssignment assignment, CancellationToken ct = default)
    {
        var existing = await GetStudentAssignmentAsync(assignment.StudentProfileId, ct);
        if (existing is null)
        {
            await _db.StudentStreamAssignments.AddAsync(assignment, ct);
            return;
        }

        _db.StudentStreamAssignments.Remove(existing);
        await _db.StudentStreamAssignments.AddAsync(assignment, ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}

public class ReportCardRepository : IReportCardRepository
{
    private readonly ApplicationDbContext _db;

    public ReportCardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(StudentReportCard reportCard, CancellationToken ct = default)
        => _db.StudentReportCards.AddAsync(reportCard, ct).AsTask();

    public Task<StudentReportCard?> GetLatestForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.StudentReportCards
            .Where(x => x.StudentProfileId == studentProfileId)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StudentReportCard>> GetForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.StudentReportCards
            .Where(x => x.StudentProfileId == studentProfileId)
            .OrderByDescending(x => x.GeneratedAt)
            .ToListAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}

public class BulkPromotionRepository : IBulkPromotionRepository
{
    private readonly ApplicationDbContext _db;

    public BulkPromotionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task AddBatchAsync(BulkPromotionBatch batch, CancellationToken ct = default)
        => _db.BulkPromotionBatches.AddAsync(batch, ct).AsTask();

    public Task<BulkPromotionBatch?> GetBatchByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BulkPromotionBatches.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<BulkPromotionBatch>> GetRecentBatchesAsync(int take = 20, CancellationToken ct = default)
        => await _db.BulkPromotionBatches
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public void UpdateBatch(BulkPromotionBatch batch)
        => _db.BulkPromotionBatches.Update(batch);

    public Task AddEntryAsync(BulkPromotionEntry entry, CancellationToken ct = default)
        => _db.BulkPromotionEntries.AddAsync(entry, ct).AsTask();

    public async Task<IReadOnlyList<BulkPromotionEntry>> GetEntriesAsync(Guid batchId, CancellationToken ct = default)
        => await _db.BulkPromotionEntries
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

    public void UpdateEntry(BulkPromotionEntry entry)
        => _db.BulkPromotionEntries.Update(entry);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}

public class ParentStudentLinkRepository : IParentStudentLinkRepository
{
    private readonly ApplicationDbContext _db;

    public ParentStudentLinkRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ParentStudentLink>> GetByParentUserIdAsync(Guid parentUserId, CancellationToken ct = default)
        => await _db.ParentStudentLinks
            .Where(x => x.ParentUserId == parentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<ParentStudentLink?> GetByParentAndStudentAsync(
        Guid parentUserId,
        Guid studentProfileId,
        CancellationToken ct = default)
        => _db.ParentStudentLinks
            .FirstOrDefaultAsync(x => x.ParentUserId == parentUserId && x.StudentProfileId == studentProfileId, ct);

    public async Task<IReadOnlyList<Guid>> GetActiveParentUserIdsByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.ParentStudentLinks
            .Where(x => x.StudentProfileId == studentProfileId && x.IsActive)
            .Select(x => x.ParentUserId)
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetActiveParentUserIdsByStudentsAsync(IReadOnlyList<Guid> studentProfileIds, CancellationToken ct = default)
    {
        if (studentProfileIds.Count == 0)
            return [];

        return await _db.ParentStudentLinks
            .Where(x => x.IsActive && studentProfileIds.Contains(x.StudentProfileId))
            .Select(x => x.ParentUserId)
            .Distinct()
            .ToListAsync(ct);
    }

    public Task AddAsync(ParentStudentLink link, CancellationToken ct = default)
        => _db.ParentStudentLinks.AddAsync(link, ct).AsTask();

    public void Update(ParentStudentLink link)
        => _db.ParentStudentLinks.Update(link);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
