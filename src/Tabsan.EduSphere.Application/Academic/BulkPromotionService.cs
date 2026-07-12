using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

// Phase 26 — Stage 26.2

public class BulkPromotionService : IBulkPromotionService
{
    private readonly IBulkPromotionRepository _repo;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IProgressionService _progression;

    public BulkPromotionService(
        IBulkPromotionRepository repo,
        IStudentProfileRepository studentRepo,
        IProgressionService progression)
    {
        _repo = repo;
        _studentRepo = studentRepo;
        _progression = progression;
    }

    public async Task<BulkPromotionBatchDto> CreateBatchAsync(
        CreateBulkPromotionBatchRequest request,
        CancellationToken ct = default)
    {
        var batch = new BulkPromotionBatch(request.Title, request.CreatedByUserId);
        await _repo.AddBatchAsync(batch, ct);
        await _repo.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(batch.Id, ct);
    }

    public async Task<BulkPromotionBatchDto> AddEntriesAsync(
        AddBulkPromotionEntriesRequest request,
        CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchByIdAsync(request.BatchId, ct)
            ?? throw new KeyNotFoundException($"Batch {request.BatchId} not found.");
        var existing = await _repo.GetEntriesAsync(batch.Id, ct);

        foreach (var entry in request.Entries)
        {
            if (existing.Any(e => e.StudentProfileId == entry.StudentProfileId))
                throw new InvalidOperationException(
                    $"Student {entry.StudentProfileId} is already in this batch.");

            _ = await _studentRepo.GetByIdAsync(entry.StudentProfileId, ct)
                ?? throw new KeyNotFoundException($"Student profile {entry.StudentProfileId} not found.");

            batch.AddEntry(entry.StudentProfileId, entry.Decision);

            var created = batch.Entries.Last();
            if (!string.IsNullOrWhiteSpace(entry.Reason))
                created.UpdateDecision(entry.Decision, entry.Reason);

            await _repo.AddEntryAsync(created, ct);
        }

        _repo.UpdateBatch(batch);
        await _repo.SaveChangesAsync(ct);

        return await GetRequiredDtoAsync(batch.Id, ct);
    }

    public async Task<BulkPromotionBatchDto> SubmitAsync(Guid batchId, CancellationToken ct = default)
    {
        // Load entries first — the EF configuration ignores the Entries navigation,
        // so the domain model's _entries list is always empty after materialization.
        // We validate against the persisted entries instead.
        var entries = await _repo.GetEntriesAsync(batchId, ct);
        if (entries.Count == 0)
            throw new InvalidOperationException("Cannot submit an empty batch.");

        var batch = await _repo.GetBatchByIdAsync(batchId, ct)
            ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

        if (batch.Status != BulkPromotionStatus.Draft)
            throw new InvalidOperationException("Only a Draft batch can be submitted.");

        // Populate the in-memory _entries list so the domain Submit() check passes.
        // These are transient objects used only to satisfy the domain guard clause;
        // EF ignores the Entries navigation so they won't be persisted.
        // Skip entries already present (e.g., from in-memory test stubs).
        foreach (var entry in entries)
        {
            if (!batch.Entries.Any(e => e.StudentProfileId == entry.StudentProfileId))
                batch.AddEntry(entry.StudentProfileId, entry.Decision);
        }

        batch.Submit();
        _repo.UpdateBatch(batch);
        await _repo.SaveChangesAsync(ct);

        return await GetRequiredDtoAsync(batch.Id, ct);
    }

    public async Task<BulkPromotionBatchDto> ReviewAsync(
        ReviewBulkPromotionBatchRequest request,
        CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchByIdAsync(request.BatchId, ct)
            ?? throw new KeyNotFoundException($"Batch {request.BatchId} not found.");

        if (request.Approve)
            batch.Approve(request.ReviewerUserId, request.Note);
        else
            batch.Reject(request.ReviewerUserId, request.Note ?? "Rejected without note.");

        _repo.UpdateBatch(batch);
        await _repo.SaveChangesAsync(ct);

        return await GetRequiredDtoAsync(batch.Id, ct);
    }

    public async Task<BulkPromotionBatchDto> ApplyAsync(ApplyBulkPromotionBatchRequest request, CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchByIdAsync(request.BatchId, ct)
            ?? throw new KeyNotFoundException($"Batch {request.BatchId} not found.");

        var entries = await _repo.GetEntriesAsync(batch.Id, ct);

        foreach (var entry in entries.Where(e => e.Decision == EntryDecision.Promote && !e.IsApplied))
        {
            var student = await _studentRepo.GetByIdAsync(entry.StudentProfileId, ct)
                ?? throw new KeyNotFoundException($"Student profile {entry.StudentProfileId} not found.");

            var institutionType = student.Department?.InstitutionType ?? InstitutionType.University;
            var decision = await _progression.EvaluateAsync(
                new ProgressionEvaluationRequest(student.Id, institutionType),
                ct);

            if (!decision.CanProgress)
            {
                var reason = institutionType == InstitutionType.College
                    ? $"Supplementary required before year progression. {decision.Remarks}"
                    : $"Promotion criteria not met. {decision.Remarks}";

                entry.UpdateDecision(EntryDecision.Hold, reason);
                _repo.UpdateEntry(entry);
                continue;
            }

            await _progression.PromoteAsync(new ProgressionEvaluationRequest(student.Id, institutionType), ct);

            entry.MarkApplied();
            _repo.UpdateEntry(entry);
        }

        batch.MarkApplied();
        _repo.UpdateBatch(batch);

        await _repo.SaveChangesAsync(ct);

        return await GetRequiredDtoAsync(batch.Id, ct);
    }

    public async Task<BulkPromotionBatchDto?> GetByIdAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchByIdAsync(batchId, ct);
        if (batch is null)
            return null;

        var entries = await _repo.GetEntriesAsync(batch.Id, ct);
        return ToDto(batch, entries);
    }

    private async Task<BulkPromotionBatchDto> GetRequiredDtoAsync(Guid batchId, CancellationToken ct)
        => await GetByIdAsync(batchId, ct) ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

    private static BulkPromotionBatchDto ToDto(BulkPromotionBatch batch, IReadOnlyList<BulkPromotionEntry> entries)
        => new(
            batch.Id,
            batch.Title,
            batch.Status,
            batch.CreatedByUserId,
            batch.ApprovedByUserId,
            batch.ReviewedAt,
            batch.AppliedAt,
            batch.ReviewNote,
            entries.Select(e => new BulkPromotionEntryDto(
                e.Id,
                e.BatchId,
                e.StudentProfileId,
                e.Decision,
                e.Reason,
                e.IsApplied,
                e.AppliedAt)).ToList());
}
