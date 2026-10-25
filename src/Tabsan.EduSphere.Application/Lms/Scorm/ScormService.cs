using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>
/// Plan M13.1 — SCORM 1.2 / 2004 packages: upload (checked and extracted), the learner's launch (new attempt, resume or
/// review), the runtime's commits (CMI data kept for resume; status, score and time recorded), the best score of finished
/// attempts posted to the package's gradebook assignment, and the teacher's report. Module progress reads the attempts.
/// </summary>
public sealed class ScormService
{
    private readonly IScormRepository _repo;
    private readonly IScormContentStore _store;
    private readonly IScormPlayTokens _tokens;
    private readonly ScormOptions _options;
    private readonly IAssignmentRepository _assignments;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IAuditService _audit;
    private readonly ModuleItemService? _modules;
    private readonly IAssignmentGradebookSync? _gradebook;
    private readonly IAssignmentPolicyService? _policies;
    private readonly ILogger<ScormService>? _logger;
    private readonly Func<DateTime> _now;

    public ScormService(IScormRepository repo, IScormContentStore store, IScormPlayTokens tokens, IOptions<ScormOptions> options,
        IAssignmentRepository assignments, ISubmissionWorkspaceRepository submissions, IAuditService audit, ModuleItemService? modules = null,
        IAssignmentGradebookSync? gradebook = null, IAssignmentPolicyService? policies = null, ILogger<ScormService>? logger = null, Func<DateTime>? now = null)
    {
        _repo = repo;
        _store = store;
        _tokens = tokens;
        _options = options.Value;
        _assignments = assignments;
        _submissions = submissions;
        _audit = audit;
        _modules = modules;
        _gradebook = gradebook;
        _policies = policies;
        _logger = logger;
        _now = now ?? (() => DateTime.UtcNow);
    }

    public long MaxUploadBytes => (long)_options.MaxPackageMb * 1024 * 1024;

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Checks and extracts a package (a seekable zip stream) and adds it to the course.</summary>
    public async Task<ScormResult<ScormPackageResponse>> ImportAsync(Guid offeringId, Stream zip, string? fileName, ScormImportRequest r, Guid actorUserId,
        CancellationToken ct = default)
    {
        if (zip.CanSeek && zip.Length > MaxUploadBytes) return ScormResult<ScormPackageResponse>.Fail(400, $"The package is larger than {_options.MaxPackageMb:N0} MB.");
        if (r.MaxAttempts is < 0 or > 100) return ScormResult<ScormPackageResponse>.Fail(400, "Allowed attempts must be between 0 (unlimited) and 100.");
        if (r.Graded && r.MaxMarks is not (> 0 and <= 1000)) return ScormResult<ScormPackageResponse>.Fail(400, "Enter the marks the package is worth (1–1000).");

        ScormPackageContents contents;
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException) { return ScormResult<ScormPackageResponse>.Fail(400, "The file is not a zip package."); }

        using (archive)
        {
            try { contents = ScormPackageReader.Read(archive, _options); }
            catch (ScormPackageException ex) { return ScormResult<ScormPackageResponse>.Fail(400, ex.Message); }
            catch (InvalidDataException) { return ScormResult<ScormPackageResponse>.Fail(400, "The package is damaged."); }

            var fromFile = Path.GetFileNameWithoutExtension(fileName ?? "");
            var title = !string.IsNullOrWhiteSpace(r.Title) ? r.Title.Trim()
                : contents.Manifest.Title ?? (fromFile.Length > 0 ? fromFile : "SCORM package");
            var package = new ScormPackage(offeringId, title, contents.Manifest.Version, contents.Manifest.Identifier, contents.Manifest.LaunchPath,
                contents.Files.Count, contents.TotalBytes, contents.Manifest.MasteryScore, actorUserId, contents.Manifest.ScoCount);
            package.SetMaxAttempts(r.MaxAttempts);
            package.SetParts(contents.Manifest.Parts ?? []);
            package.SetSequencing(contents.Manifest.Sequencing);

            try
            {
                foreach (var file in contents.Files)
                {
                    await using var source = file.Entry.Open();
                    await using var limited = new LimitedReadStream(source, file.Entry.Length);
                    await _store.SaveAsync(package.Id, file.Path, limited, ct);
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                await _store.DeleteAsync(package.Id, CancellationToken.None);
                _logger?.LogWarning(ex, "SCORM package extraction failed.");
                return ScormResult<ScormPackageResponse>.Fail(400, "The package could not be extracted (damaged or inconsistent zip).");
            }

            if (r.Graded) await AttachAssignmentAsync(package, r.MaxMarks!.Value, ct);
            package.SetPublished(r.Publish);
            _repo.Add(package);
            await _repo.SaveChangesAsync(ct);
            await _audit.LogAsync(new AuditLog("ScormPackageImported", "ScormPackage", package.Id.ToString(), actorUserId: actorUserId,
                newValuesJson: $"{{\"offering\":\"{offeringId}\",\"version\":\"{package.Version}\",\"files\":{package.FileCount}}}"), ct);
            return ScormResult<ScormPackageResponse>.Success(ToResponse(package, null), 201);
        }
    }

    public async Task<IReadOnlyList<ScormPackageResponse>> ListAsync(Guid offeringId, Guid? studentProfileId, CancellationToken ct = default)
    {
        var packages = (await _repo.GetPackagesAsync(offeringId, ct)).Where(p => !p.IsRemoved && (studentProfileId is null || p.IsPublished)).ToList();
        if (studentProfileId is not { } student) return packages.Select(p => ToResponse(p, null)).ToList();
        var attempts = await _repo.GetAttemptsForStudentAsync(packages.Select(p => p.Id).ToList(), student, ct);
        return packages.Select(p => ToResponse(p, Summary(p, attempts.Where(a => a.PackageId == p.Id).ToList()))).ToList();
    }

    public async Task<ScormPackageResponse?> GetAsync(Guid packageId, Guid? studentProfileId, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved || (studentProfileId is not null && !package.IsPublished)) return null;
        if (studentProfileId is not { } student) return ToResponse(package, null);
        return ToResponse(package, Summary(package, (await _repo.GetAttemptsAsync(packageId, student, ct)).ToList()));
    }

    public async Task<ScormResult<ScormPackageResponse>> UpdateAsync(Guid packageId, ScormUpdateRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved) return ScormResult<ScormPackageResponse>.Fail(404, "Package not found.");
        try
        {
            package.Rename(r.Title);
            package.SetMaxAttempts(r.MaxAttempts);
            package.SetPublished(r.IsPublished);
            if (r.Graded)
            {
                if (r.MaxMarks is not (> 0 and <= 1000)) return ScormResult<ScormPackageResponse>.Fail(400, "Enter the marks the package is worth (1–1000).");
                if (package.AssignmentId is null) await AttachAssignmentAsync(package, r.MaxMarks.Value, ct);
                else package.SetMaxMarks(r.MaxMarks.Value);
            }
            else if (package.AssignmentId is not null)
                return ScormResult<ScormPackageResponse>.Fail(400, "A graded package stays graded; remove its assignment from the gradebook instead.");
        }
        catch (ArgumentException ex) { return ScormResult<ScormPackageResponse>.Fail(400, ex.Message); }

        if (package.AssignmentId is { } assignmentId && await _assignments.GetByIdAsync(assignmentId, ct) is { ScormPackageId: not null } assignment)
        {
            assignment.SyncExternal(package.Title, package.MaxMarks);
            _assignments.Update(assignment);
        }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ScormPackageUpdated", "ScormPackage", package.Id.ToString(), actorUserId: actorUserId), ct);
        return ScormResult<ScormPackageResponse>.Success(ToResponse(package, null));
    }

    /// <summary>Removes the package: files deleted, attempts kept; its assignment goes too unless students already have grades there.</summary>
    public async Task<ScormResult<bool>> RemoveAsync(Guid packageId, Guid actorUserId, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved) return ScormResult<bool>.Fail(404, "Package not found.");
        package.Remove();
        if (package.AssignmentId is { } assignmentId && await _assignments.GetByIdAsync(assignmentId, ct) is { } assignment
            && !(await _submissions.GetSubmissionsAsync(assignmentId, false, ct)).Any(s => s.MarksAwarded.HasValue))
        {
            assignment.SoftDelete();
            _assignments.Update(assignment);
        }
        await _repo.SaveChangesAsync(ct);
        await _store.DeleteAsync(package.Id, ct);
        await _audit.LogAsync(new AuditLog("ScormPackageRemoved", "ScormPackage", package.Id.ToString(), actorUserId: actorUserId), ct);
        return ScormResult<bool>.Success(true);
    }

    public async Task<ScormReportResponse?> ReportAsync(Guid packageId, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved) return null;
        var attempts = await _repo.GetAttemptsAsync(packageId, null, ct);
        var learners = await _repo.GetEnrolledLearnersAsync(package.OfferingId, ct);
        var rows = learners.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase).Select(l =>
        {
            var mine = attempts.Where(a => a.StudentProfileId == l.StudentProfileId).OrderBy(a => a.Number).ToList();
            var last = mine.LastOrDefault();
            return new ScormReportRow(l.StudentProfileId, l.Name, l.RegistrationNumber, mine.Count, last?.CompletionStatus, last?.SuccessStatus,
                Best(mine), last?.ScorePercent, mine.Sum(a => a.TotalSeconds), last?.LastCommitAt ?? last?.StartedAt);
        }).ToList();
        return new ScormReportResponse(ToResponse(package, null), rows);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Learner
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts the first attempt, a new one when asked (and allowed), resumes an unfinished one, or opens the last finished
    /// attempt in review mode; returns the play link.
    /// </summary>
    public async Task<ScormResult<ScormLaunchResponse>> LaunchAsync(Guid packageId, Guid studentProfileId, bool newAttempt, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved || !package.IsPublished) return ScormResult<ScormLaunchResponse>.Fail(404, "This package is not available.");
        if (!await _repo.IsEnrolledAsync(package.OfferingId, studentProfileId, ct)) return ScormResult<ScormLaunchResponse>.Fail(403, "You are not enrolled in this course.");
        if (_modules is not null && await _modules.LockReasonAsync(studentProfileId, ModuleItemType.ScormPackage, packageId, ct) is { } locked)
            return ScormResult<ScormLaunchResponse>.Fail(403, locked);

        var attempts = (await _repo.GetAttemptsAsync(packageId, studentProfileId, ct)).OrderBy(a => a.Number).ToList();
        var latest = attempts.LastOrDefault();
        var review = false;
        ScormAttempt attempt;
        if (latest is null || (newAttempt && package.AllowsAnotherAttempt(attempts.Count)))
        {
            attempt = new ScormAttempt(packageId, studentProfileId, attempts.Count + 1, _now());
            _repo.Add(attempt);
            await _repo.SaveChangesAsync(ct);
            attempts.Add(attempt);
        }
        else if (newAttempt && latest.IsFinished)
            return ScormResult<ScormLaunchResponse>.Fail(409, "You have used all your attempts at this package.");
        else
        {
            attempt = latest;
            review = latest.IsFinished; // look back at the finished attempt
        }

        var token = _tokens.Issue(new ScormPlayClaims(attempt.Id, packageId, studentProfileId, review), TimeSpan.FromHours(Math.Clamp(_options.PlayTokenHours, 1, 48)));
        return ScormResult<ScormLaunchResponse>.Success(new ScormLaunchResponse(token, packageId, package.Title, VersionName(package.Version), package.LaunchPath,
            attempt.Number, review ? "review" : "normal", attempts.Count, package.MaxAttempts,
            PartStatuses(package, attempt, package.IsSequenced && !review ? Sequencer(package, attempt) : null), package.IsSequenced && !review));
    }

    /// <summary>A play link for staff to try a package: nothing is recorded (review mode, no attempt).</summary>
    public async Task<ScormResult<ScormLaunchResponse>> PreviewAsync(Guid packageId, CancellationToken ct = default)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved) return ScormResult<ScormLaunchResponse>.Fail(404, "Package not found.");
        var token = _tokens.Issue(new ScormPlayClaims(Guid.Empty, packageId, Guid.Empty, true), TimeSpan.FromHours(2));
        return ScormResult<ScormLaunchResponse>.Success(new ScormLaunchResponse(token, packageId, package.Title, VersionName(package.Version), package.LaunchPath,
            0, "preview", 0, package.MaxAttempts, PartStatuses(package, null)));
    }

    /// <summary>
    /// The player shell's start data for a play link. Sequenced packages (SCORM 2004 sequencing) open the part the package's
    /// rules deliver — the one being worked on, or the first the flow reaches — and may have ended. Other multi-part
    /// packages open the requested part (else the first one not done yet) unless its SCORM 1.2 prerequisites are not met.
    /// Each part starts from its own saved data; an asset part counts as done once opened.
    /// </summary>
    public Task<ScormResult<ScormPlayResponse>> PlayAsync(string token, CancellationToken ct = default, string? partId = null)
        => WithAttemptAsync(token, "This link has expired. Open the package again from the course.", t => PlayAsync(t, partId, ct), ct);

    private async Task<ScormResult<ScormPlayResponse>> PlayAsync(OpenToken t, string? partId, CancellationToken ct)
    {
        var learner = t.Claims.AttemptId == Guid.Empty ? new ScormLearnerInfo(Guid.Empty, Guid.Empty, "Preview", "preview")
            : await _repo.GetLearnerAsync(t.Attempt.StudentProfileId, ct);
        var learnerId = learner?.RegistrationNumber ?? t.Attempt.StudentProfileId.ToString("N");
        var parts = t.Package.Parts;
        var mode = t.Claims.Review ? "review" : "normal";

        if (IsSequenced(t))
        {
            var seq = Sequencer(t);
            // An ended package stays at its end until the learner starts it again (Next or a part in the contents).
            var delivery = seq.State.Ended
                ? new SeqDelivery(null, null, false, true, new SeqProblem("You have reached the end of this package."))
                : seq.Start();
            ScormPart? delivered = delivery.PartId is { } id ? parts.FirstOrDefault(p => p.Id == id) : null;
            if (delivered is not null)
            {
                if (delivery.NewAttempt) t.Attempt.ResetPart(delivered.Id);
                if (!delivered.IsSco) seq.Viewed(delivered.Id);
            }
            await SaveSequencingAsync(t, seq, ct);
            var view = seq.View();
            var statuses = PartStatuses(t.Package, t.Attempt, seq);
            if (delivered is null)
                return ScormResult<ScormPlayResponse>.Success(new ScormPlayResponse(t.Package.Id, t.Package.Title, VersionName(t.Package.Version), "", mode,
                    t.Attempt.Number, "{}", null, null, true, statuses, true, delivery.Ended,
                    delivery.Message?.Text ?? "Choose a part from the contents.", view.CanContinue, view.CanPrevious, view.HideLmsUi, 0, delivery.Message?.Arg));
            var saved = t.Attempt.PartStates().GetValueOrDefault(delivered.Id);
            var startCmi = ScormCmi.Initial(t.Package.Version, saved?.CmiJson, saved?.CommitCount ?? 0, saved?.Exit, saved?.TotalSeconds ?? 0, learnerId,
                learner?.Name ?? "Learner", delivered.MasteryScore ?? t.Package.MasteryScore, false);
            return ScormResult<ScormPlayResponse>.Success(new ScormPlayResponse(t.Package.Id, t.Package.Title, VersionName(t.Package.Version), delivered.LaunchPath,
                mode, t.Attempt.Number, startCmi, delivered.Id, delivered.Title, delivered.IsSco, statuses, true, false, null, view.CanContinue,
                view.CanPrevious, view.HideLmsUi, PartAttempt(seq, delivered.Id)));
        }

        var requested = partId is null ? null : parts.FirstOrDefault(p => p.Id == partId);
        var part = requested is not null && (t.Claims.Review || PrerequisitesMet(t.Package, t.Attempt, requested)) ? requested : FirstOpenPart(t.Package, t.Attempt);
        string cmi;
        if (t.Package.IsMultiPart)
        {
            if (!part.IsSco && !t.Claims.Review && t.Claims.AttemptId != Guid.Empty)
            {
                var wasFinished = t.Attempt.IsFinished;
                t.Attempt.MarkPartViewed(parts, part.Id, _now());
                await _repo.SaveChangesAsync(ct);
                if (t.Package.AssignmentId is not null && t.Attempt.IsFinished && !wasFinished) await TryPostBestScoreAsync(t.Package, t.Attempt.StudentProfileId, ct);
            }
            var state = t.Attempt.PartStates().GetValueOrDefault(part.Id);
            cmi = ScormCmi.Initial(t.Package.Version, state?.CmiJson, state?.CommitCount ?? 0, state?.Exit, state?.TotalSeconds ?? 0, learnerId,
                learner?.Name ?? "Learner", part.MasteryScore ?? t.Package.MasteryScore, t.Claims.Review);
        }
        else cmi = ScormCmi.Initial(t.Package.Version, t.Attempt, learnerId, learner?.Name ?? "Learner", t.Package.MasteryScore, t.Claims.Review);
        return ScormResult<ScormPlayResponse>.Success(new ScormPlayResponse(t.Package.Id, t.Package.Title, VersionName(t.Package.Version), part.LaunchPath,
            mode, t.Attempt.Number, cmi, part.Id, part.Title, part.IsSco, PartStatuses(t.Package, t.Attempt)));
    }

    /// <summary>The parts and their status for a play link (the player refreshes its contents list after each commit).</summary>
    public async Task<ScormResult<IReadOnlyList<ScormPartStatus>>> PartsAsync(string token, CancellationToken ct = default)
        => await OpenTokenAsync(token, ct) is { } t
            ? ScormResult<IReadOnlyList<ScormPartStatus>>.Success(PartStatuses(t.Package, t.Attempt, IsSequenced(t) ? Sequencer(t) : null))
            : ScormResult<IReadOnlyList<ScormPartStatus>>.Fail(404, "This link has expired.");

    private static readonly HashSet<string> NavigationRequests = new(StringComparer.Ordinal)
    {
        "start", "resumeAll", "continue", "previous", "choice", "exit", "exitAll", "suspendAll", "abandon", "abandonAll"
    };

    /// <summary>
    /// A navigation request for a sequenced package (from the player page's Next / Previous / contents list, or from a SCO's
    /// adl.nav.request): the package's rules decide the part to open next. Other packages are navigated by the page itself.
    /// </summary>
    public Task<ScormResult<ScormNavigateResponse>> NavigateAsync(string token, ScormNavigateRequest request, CancellationToken ct = default)
        => WithAttemptAsync(token, "This link has expired.", t => NavigateAsync(t, request, ct), ct);

    private async Task<ScormResult<ScormNavigateResponse>> NavigateAsync(OpenToken t, ScormNavigateRequest request, CancellationToken ct)
    {
        if (!NavigationRequests.Contains(request.Request ?? "")) return ScormResult<ScormNavigateResponse>.Fail(400, "Unknown navigation request.");
        if (!IsSequenced(t))
            return ScormResult<ScormNavigateResponse>.Success(new ScormNavigateResponse(request.Request == "choice" ? request.Target : null, false, null,
                PartStatuses(t.Package, t.Attempt), true, true));

        var seq = Sequencer(t);
        var delivery = seq.Navigate(request.Request!, request.Target);
        if (delivery.PartId is { } partId)
        {
            if (delivery.NewAttempt) t.Attempt.ResetPart(partId);
            if (t.Package.Parts.FirstOrDefault(p => p.Id == partId) is { IsSco: false }) seq.Viewed(partId);
        }
        await SaveSequencingAsync(t, seq, ct);
        var view = seq.View();
        return ScormResult<ScormNavigateResponse>.Success(new ScormNavigateResponse(delivery.PartId, delivery.Ended, delivery.Message?.Text,
            PartStatuses(t.Package, t.Attempt, seq), view.CanContinue, view.CanPrevious, delivery.Message?.Arg));
    }

    /// <summary>
    /// Stores a commit from the runtime (of one part for multi-part packages). Review sessions are accepted but change nothing.
    /// Sequenced packages name the part's attempt: a late commit from an earlier attempt of the part (a frame being replaced) is ignored.
    /// </summary>
    public Task<ScormResult<ScormCommitResponse>> CommitAsync(string token, string body, bool finalCommit, CancellationToken ct = default, string? partId = null,
        int? partAttempt = null)
        => WithAttemptAsync(token, "This link has expired.", t => CommitAsync(t, body, finalCommit, partId, partAttempt, ct), ct);

    private async Task<ScormResult<ScormCommitResponse>> CommitAsync(OpenToken t, string body, bool finalCommit, string? partId, int? partAttempt,
        CancellationToken ct)
    {
        if (body.Length > ScormAttempt.MaxCmiJsonLength * 2) return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(false, 101));
        if (ScormCmi.CmiOf(body) is not { } cmi) return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(false, 101));
        if (t.Claims.Review) return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(true, 0));
        var sequenced = IsSequenced(t);
        var part = t.Package.IsMultiPart ? t.Package.Parts.FirstOrDefault(p => p.Id == partId) : sequenced ? t.Package.Parts[0] : null;
        if (t.Package.IsMultiPart && part is null) return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(false, 101));

        var wasFinished = t.Attempt.IsFinished;
        var previousBest = t.Attempt.ScorePercent;
        var values = ScormCmi.Read(t.Package.Version, cmi, part?.MasteryScore ?? t.Package.MasteryScore);
        var stored = new JsonObject { ["cmi"] = cmi.DeepClone() }.ToJsonString();
        if (stored.Length > ScormAttempt.MaxCmiJsonLength && cmi["interactions"] is not null)
        {
            // Very long interaction logs are not needed to resume; keep the rest.
            var trimmed = (JsonObject)cmi.DeepClone();
            trimmed.Remove("interactions");
            stored = new JsonObject { ["cmi"] = trimmed }.ToJsonString();
        }
        if (sequenced && part is not null)
        {
            var seq = Sequencer(t);
            if (partAttempt is { } n && n != PartAttempt(seq, part.Id)) return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(true, 0));
            t.Attempt.StorePart(t.Package.Parts, part.Id, values, stored, finalCommit, _now());
            seq.Report(part.Id, new SeqReport(values.CompletionStatus, values.SuccessStatus, values.ScoreScaled, ScormCmi.Objectives(cmi), values.Exit), finalCommit);
            await SaveSequencingAsync(t, seq, ct, postGrade: false);
        }
        else
        {
            if (part is not null) t.Attempt.ApplyPart(t.Package.Parts, part.Id, values, stored, finalCommit, _now());
            else t.Attempt.Apply(values, stored, finalCommit, _now());
            await _repo.SaveChangesAsync(ct);
        }

        if (t.Package.AssignmentId is not null && t.Attempt.IsFinished && (!wasFinished || previousBest != t.Attempt.ScorePercent))
            await TryPostBestScoreAsync(t.Package, t.Attempt.StudentProfileId, ct);
        return ScormResult<ScormCommitResponse>.Success(new ScormCommitResponse(true, 0));
    }

    /// <summary>Sequencing applies to a learner's own attempt; staff preview and review mode open any part freely.</summary>
    private static bool IsSequenced(OpenToken t) => t.Package.IsSequenced && !t.Claims.Review && t.Claims.AttemptId != Guid.Empty;

    private static ScormSequencer Sequencer(OpenToken t) => Sequencer(t.Package, t.Attempt);

    /// <summary>
    /// The package's sequencing for an attempt; an attempt with progress but no sequencing state yet (made before the
    /// upgrade) brings its parts' results in.
    /// </summary>
    private static ScormSequencer Sequencer(ScormPackage package, ScormAttempt attempt)
    {
        var seq = new ScormSequencer(package.Sequencing!, attempt.SequencingState());
        if (attempt.SequencingStateJson is null)
            foreach (var (partId, state) in attempt.PartStates())
                seq.Adopt(partId, state.CompletionStatus, state.SuccessStatus, state.ScorePercent is { } p ? p / 100 : null);
        return seq;
    }

    /// <summary>The attempt number of a part's activity (stale commits from an earlier attempt carry an older number).</summary>
    private static int PartAttempt(ScormSequencer seq, string partId)
        => seq.ActivityOfPart(partId) is { } a && seq.State.Activities.TryGetValue(a.Id, out var st) ? st.Attempts : 0;

    /// <summary>Saves the sequencing state and the package's rolled-up result; posts the best score when the attempt is finished.</summary>
    private async Task SaveSequencingAsync(OpenToken t, ScormSequencer seq, CancellationToken ct, bool postGrade = true)
    {
        var wasFinished = t.Attempt.IsFinished;
        var previousBest = t.Attempt.ScorePercent;
        var (completion, success, measure) = seq.Result();
        var states = t.Attempt.PartStates().Values.Where(s => s.ScorePercent is not null).Select(s => s.ScorePercent!.Value).ToList();
        decimal? percent = measure is { } m ? m * 100 : states.Count > 0 ? states.Average() : null;
        t.Attempt.SetSequencingState(seq.State);
        t.Attempt.SetSequencedResult(completion, success, percent, seq.State.Ended, _now());
        await _repo.SaveChangesAsync(ct);
        if (postGrade && t.Package.AssignmentId is not null && t.Attempt.IsFinished && (!wasFinished || previousBest != t.Attempt.ScorePercent))
            await TryPostBestScoreAsync(t.Package, t.Attempt.StudentProfileId, ct);
    }

    /// <summary>SCORM 1.2 prerequisites of a part, from the other parts' lesson status in this attempt.</summary>
    private static bool PrerequisitesMet(ScormPackage package, ScormAttempt attempt, ScormPart part)
    {
        if (part.Prerequisites is null) return true;
        var states = attempt.PartStates();
        var ids = package.Parts.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        return ScormPrerequisites.Met(part.Prerequisites, id => !ids.Contains(id) ? null
            : states.GetValueOrDefault(id) is { } s ? (s.SuccessStatus is "passed" or "failed" ? s.SuccessStatus : s.CompletionStatus) : "not attempted");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Packages read under older rules
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads again the manifest of packages uploaded before the current structure rules (parts, SCORM 2004 sequencing, SCORM
    /// 1.2 prerequisites), so they work as if uploaded today. Attempts made when only the first part launched keep their data
    /// on that part. Returns how many packages were upgraded.
    /// </summary>
    public async Task<int> UpgradeStructuresAsync(CancellationToken ct = default)
    {
        var upgraded = 0;
        foreach (var package in await _repo.GetPackagesBelowStructureVersionAsync(ScormPackage.CurrentStructureVersion, ct))
        {
            try
            {
                await using var stream = await _store.OpenAsync(package.Id, "imsmanifest.xml", ct);
                if (stream is null)
                {
                    package.SetSequencing(null); // nothing to read: keep its parts, stop asking
                    continue;
                }
                var manifest = ScormPackageReader.ParseManifest(ScormPackageReader.LoadManifest(stream));
                var oldLaunch = package.LaunchPath;
                var wasMultiPart = package.IsMultiPart;
                package.UpgradeStructure(manifest.LaunchPath, manifest.ScoCount, manifest.Parts ?? [], manifest.Sequencing);
                if (package.IsMultiPart && !wasMultiPart)
                {
                    var first = package.Parts.FirstOrDefault(p => p.LaunchPath == oldLaunch) ?? package.Parts[0];
                    foreach (var attempt in await _repo.GetAttemptsAsync(package.Id, null, ct)) attempt.AdoptSinglePartData(first.Id);
                }
                upgraded++;
            }
            catch (Exception ex) when (ex is ScormPackageException or IOException or InvalidOperationException)
            {
                _logger?.LogWarning(ex, "SCORM package {PackageId} could not be read again.", package.Id);
                package.SetSequencing(null);
            }
        }
        await _repo.SaveChangesAsync(ct);
        return upgraded;
    }

    private async Task TryPostBestScoreAsync(ScormPackage package, Guid studentProfileId, CancellationToken ct)
    {
        try { await PostBestScoreAsync(package, studentProfileId, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger?.LogWarning(ex, "SCORM score could not be posted to the gradebook."); }
    }

    /// <summary>The first part not done yet whose prerequisites are met (the first part when all are done).</summary>
    private static ScormPart FirstOpenPart(ScormPackage package, ScormAttempt attempt)
    {
        var parts = package.Parts;
        if (!package.IsMultiPart) return parts[0];
        var states = attempt.PartStates();
        return parts.FirstOrDefault(p => states.GetValueOrDefault(p.Id)?.IsDone != true && PrerequisitesMet(package, attempt, p))
               ?? parts.FirstOrDefault(p => PrerequisitesMet(package, attempt, p)) ?? parts[0];
    }

    /// <summary>
    /// Each part's status in an attempt (a one-part package reports the attempt itself). With sequencing: the contents as the
    /// package shows them (visible groups as headings, hidden parts left out) and whether each part may be chosen now; with
    /// SCORM 1.2 prerequisites, whether they are met.
    /// </summary>
    private static IReadOnlyList<ScormPartStatus> PartStatuses(ScormPackage package, ScormAttempt? attempt, ScormSequencer? seq = null)
    {
        static string StatusOf(string completion, string success) => success is "passed" or "failed" ? success : completion;
        var states = attempt?.PartStates() ?? new Dictionary<string, ScormPartState>();
        ScormPartStatus Status(ScormPart p) => states.GetValueOrDefault(p.Id) is { } s
            ? new ScormPartStatus(p.Id, p.Title, p.IsSco, StatusOf(s.CompletionStatus, s.SuccessStatus), s.ScorePercent)
            : new ScormPartStatus(p.Id, p.Title, p.IsSco, "not attempted", null);
        if (seq is not null)
        {
            var parts = package.Parts.ToDictionary(p => p.Id, StringComparer.Ordinal);
            return seq.View().Items.Select(i => i.IsLeaf && i.PartId is { } partId && parts.TryGetValue(partId, out var p)
                ? Status(p) with { Available = i.CanChoose, Reason = i.Reason?.Text, ReasonArg = i.Reason?.Arg, Depth = i.Depth, IsCurrent = i.IsCurrent }
                : new ScormPartStatus(i.ActivityId, i.Title, false, "", null, false, null, i.Depth, IsHeading: true)).ToList();
        }
        if (!package.IsMultiPart)
        {
            var only = package.Parts[0];
            return [new ScormPartStatus(only.Id, package.Title, true, attempt is null ? "not attempted" : StatusOf(attempt.CompletionStatus, attempt.SuccessStatus), attempt?.ScorePercent)];
        }
        return package.Parts.Select(p => attempt is not null && !PrerequisitesMet(package, attempt, p)
            ? Status(p) with { Available = false, Reason = "Finish the parts it depends on first." }
            : Status(p)).ToList();
    }

    /// <summary>A file of the package for a play link (null when the link or file is unknown).</summary>
    public async Task<(Stream Content, string Path)?> OpenContentAsync(string token, string path, CancellationToken ct = default)
    {
        var claims = _tokens.Read(token);
        if (claims is null) return null;
        string normalized;
        try { normalized = ScormPackageReader.NormalizeEntryPath(path ?? ""); } // already decoded by routing
        catch (ScormPackageException) { return null; }
        if (normalized.EndsWith('/')) return null;
        var stream = await _store.OpenAsync(claims.PackageId, normalized, ct);
        return stream is null ? null : (stream, normalized);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Pieces
    // ═════════════════════════════════════════════════════════════════════════

    private sealed record OpenToken(ScormPlayClaims Claims, ScormPackage Package, ScormAttempt Attempt);

    /// <summary>
    /// Opens a play link and runs a step on its attempt. When another request saved the attempt in between (the last commit
    /// of the part being left arrives together with the opening of the next part), the attempt is read again and the step
    /// repeated with what that request saved.
    /// </summary>
    private async Task<ScormResult<T>> WithAttemptAsync<T>(string token, string expired, Func<OpenToken, Task<ScormResult<T>>> step, CancellationToken ct)
    {
        for (var tries = 1; ; tries++)
        {
            if (await OpenTokenAsync(token, ct) is not { } t) return ScormResult<T>.Fail(404, expired);
            try { return await step(t); }
            catch (Exception ex) when (tries < 5 && t.Claims.AttemptId != Guid.Empty
                                       && string.Equals(ex.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
            {
                await _repo.ReloadAsync(t.Attempt, ct);
            }
        }
    }

    private async Task<OpenToken?> OpenTokenAsync(string token, CancellationToken ct)
    {
        if (_tokens.Read(token) is not { } claims) return null;
        var package = await _repo.GetPackageAsync(claims.PackageId, ct);
        if (claims.AttemptId == Guid.Empty)
            // Staff preview: a fresh attempt that is never saved.
            return claims.Review && package is { IsRemoved: false } ? new OpenToken(claims, package, new ScormAttempt(package.Id, Guid.Empty, 0, _now())) : null;
        var attempt = await _repo.GetAttemptAsync(claims.AttemptId, ct);
        if (package is null || package.IsRemoved || attempt is null || attempt.PackageId != package.Id || attempt.StudentProfileId != claims.StudentProfileId) return null;
        return new OpenToken(claims, package, attempt);
    }

    private async Task AttachAssignmentAsync(ScormPackage package, decimal maxMarks, CancellationToken ct)
    {
        var assignment = Assignment.ForScormPackage(package.OfferingId, package.Title, maxMarks, _now().AddYears(1), package.Id);
        await _assignments.AddAsync(assignment, ct);
        package.AttachAssignment(assignment.Id, maxMarks);
    }

    /// <summary>The best score of the learner's finished attempts becomes their graded submission (as for LTI tool scores).</summary>
    private async Task PostBestScoreAsync(ScormPackage package, Guid studentProfileId, CancellationToken ct)
    {
        var attempts = await _repo.GetAttemptsAsync(package.Id, studentProfileId, ct);
        if (Best(attempts) is not { } best) return;
        var assignment = await _submissions.GetAssignmentAsync(package.AssignmentId!.Value, ct);
        if (assignment is null) return;
        var submission = await _submissions.GetSubmissionAsync(assignment.Id, studentProfileId, false, ct);
        if (submission is null)
        {
            submission = AssignmentSubmission.Start(assignment.Id, studentProfileId);
            _submissions.Add(submission);
            _submissions.Add(submission.AddAttempt($"Completed in the SCORM package \"{package.Title}\".", null, _now(), false));
        }
        if (submission.Status == SubmissionStatus.Rejected) { await _submissions.SaveChangesAsync(ct); return; }
        var marks = Math.Round(best / 100 * assignment.MaxMarks, 2, MidpointRounding.AwayFromZero);
        if (submission.MarksAwarded == marks) { await _submissions.SaveChangesAsync(ct); return; }
        var penalty = _policies is null ? 0 : await _policies.LatePenaltyAsync(assignment, submission, marks, ct);
        submission.Grade(marks, $"Best score in the SCORM package: {best:0.##}%.", Guid.Empty, postNow: assignment.PostingPolicy == GradePostingPolicy.Automatic, latePenalty: penalty);
        await _submissions.SaveChangesAsync(ct);
        if (submission.PostedAt.HasValue && _gradebook is not null) await _gradebook.RefreshAsync(package.OfferingId, studentProfileId, ct);
    }

    private static decimal? Best(IEnumerable<ScormAttempt> attempts)
        => attempts.Where(a => a.IsFinished && a.ScorePercent is not null).Select(a => a.ScorePercent).Max();

    private static ScormLearnerSummary Summary(ScormPackage package, IReadOnlyList<ScormAttempt> attempts)
    {
        var last = attempts.OrderBy(a => a.Number).LastOrDefault();
        var status = last is null ? null : last.SuccessStatus is "passed" or "failed" ? last.SuccessStatus : last.CompletionStatus;
        return new ScormLearnerSummary(attempts.Count, status, Best(attempts), last is not null && package.AllowsAnotherAttempt(attempts.Count),
            last?.LastCommitAt ?? last?.StartedAt);
    }

    private static ScormPackageResponse ToResponse(ScormPackage p, ScormLearnerSummary? mine)
        => new(p.Id, p.OfferingId, p.Title, VersionName(p.Version), p.LaunchPath, p.FileCount, p.SizeBytes, p.MasteryScore, p.MaxAttempts, p.IsPublished,
            p.AssignmentId is not null, p.MaxMarks, p.AssignmentId, p.CreatedAt, p.ScoCount, mine, p.Parts.Count, p.IsSequenced);

    public static string VersionName(ScormVersion v) => v == ScormVersion.Scorm2004 ? "SCORM 2004" : "SCORM 1.2";

    /// <summary>Stops reading a zip entry that is longer than the size its header declared (zip-bomb guard).</summary>
    private sealed class LimitedReadStream(Stream inner, long limit) : Stream
    {
        private long _read;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => limit;
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => Count(await inner.ReadAsync(buffer, ct));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Count(int n)
        {
            _read += n;
            if (_read > limit) throw new InvalidDataException("A file in the package is larger than its zip header says.");
            return n;
        }
    }
}
