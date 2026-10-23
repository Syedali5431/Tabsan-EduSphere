using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

// Plan M3 — submission history, files, comments, annotations and provisional (moderated) grades.

/// <summary>Whether a submitted file has a PDF rendition for the grading viewer.</summary>
public enum FileConversionStatus
{
    /// <summary>PDFs and images are shown as they are; other types are download-only.</summary>
    NotRequired = 0,

    /// <summary>An office document waiting for the conversion worker.</summary>
    Pending = 1,

    /// <summary>Claimed by a conversion worker (a stale claim is retried).</summary>
    Converting = 2,

    /// <summary>The PDF rendition is stored next to the original.</summary>
    Ready = 3,

    /// <summary>Conversion failed after the allowed retries; the original can still be downloaded.</summary>
    Failed = 4
}

/// <summary>One submission attempt. Every resubmission adds an attempt; nothing is overwritten.</summary>
public class SubmissionAttempt : BaseEntity
{
    public Guid SubmissionId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string? TextContent { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    /// <summary>True when the attempt arrived after the due date (only possible where late work is accepted).</summary>
    public bool IsLate { get; private set; }

    private readonly List<SubmissionFile> _files = new();
    public IReadOnlyCollection<SubmissionFile> Files => _files;

    private SubmissionAttempt() { }

    internal SubmissionAttempt(Guid submissionId, int attemptNumber, string? textContent, DateTime submittedAt, bool isLate)
    {
        SubmissionId = submissionId;
        AttemptNumber = attemptNumber;
        TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent.Trim();
        SubmittedAt = submittedAt;
        IsLate = isLate;
    }

    /// <summary>Adds a file stored in media storage.</summary>
    public SubmissionFile AddFile(string fileName, string contentType, long sizeBytes, string storageKey, string? sha256)
    {
        var file = SubmissionFile.Stored(Id, fileName, contentType, sizeBytes, storageKey, sha256);
        _files.Add(file);
        return file;
    }

    /// <summary>Adds a pre-M3 file that lives at a URL (the portal's old upload folder).</summary>
    public SubmissionFile AddLegacyFile(string url)
    {
        var file = SubmissionFile.Legacy(Id, url);
        _files.Add(file);
        return file;
    }
}

/// <summary>A file in a submission attempt, with its optional PDF rendition.</summary>
public class SubmissionFile : BaseEntity
{
    /// <summary>Office formats the conversion worker turns into PDF.</summary>
    public static readonly IReadOnlySet<string> ConvertibleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".odt", ".rtf", ".ppt", ".pptx", ".odp", ".xls", ".xlsx", ".ods", ".txt"
    };

    /// <summary>Image formats annotated directly on an image canvas.</summary>
    public static readonly IReadOnlySet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

    public Guid AttemptId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long SizeBytes { get; private set; }

    /// <summary>Media storage key of the original (null for legacy files).</summary>
    public string? StorageKey { get; private set; }

    /// <summary>Pre-M3 URL (portal upload folder). Null for files stored by the API.</summary>
    public string? LegacyUrl { get; private set; }

    public string? Sha256 { get; private set; }

    public FileConversionStatus ConversionStatus { get; private set; }
    public string? PdfStorageKey { get; private set; }
    public string? ConversionError { get; private set; }
    public int ConversionAttempts { get; private set; }
    public DateTime? ConversionStartedAt { get; private set; }
    public DateTime? ConvertedAt { get; private set; }

    private SubmissionFile() { }

    internal static SubmissionFile Stored(Guid attemptId, string fileName, string contentType, long sizeBytes, string storageKey, string? sha256)
    {
        var name = CleanFileName(fileName);
        return new SubmissionFile
        {
            AttemptId = attemptId,
            FileName = name,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Sha256 = sha256,
            ConversionStatus = ConvertibleExtensions.Contains(Path.GetExtension(name))
                ? FileConversionStatus.Pending
                : FileConversionStatus.NotRequired
        };
    }

    internal static SubmissionFile Legacy(Guid attemptId, string url)
    {
        var trimmed = url.Trim();
        var name = CleanFileName(trimmed.Split('?', '#')[0].Split('/', '\\').LastOrDefault() ?? "submission");
        return new SubmissionFile
        {
            AttemptId = attemptId,
            FileName = name,
            LegacyUrl = trimmed,
            ConversionStatus = FileConversionStatus.NotRequired
        };
    }

    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();
    public bool IsPdf => Extension == ".pdf";
    public bool IsImage => ImageExtensions.Contains(Extension);

    /// <summary>What the viewer shows: "pdf" (original or rendition), "image", or "download".</summary>
    public string ViewerKind => StorageKey is null
        ? "download"
        : IsPdf || ConversionStatus == FileConversionStatus.Ready ? "pdf"
        : IsImage ? "image"
        : "download";

    /// <summary>Claims the file for conversion. Returns false when it is not waiting (or another worker holds a fresh claim).</summary>
    public bool TryClaimForConversion(DateTime now, TimeSpan staleAfter)
    {
        var claimable = ConversionStatus == FileConversionStatus.Pending
            || (ConversionStatus == FileConversionStatus.Converting && ConversionStartedAt < now - staleAfter);
        if (!claimable || StorageKey is null) return false;
        ConversionStatus = FileConversionStatus.Converting;
        ConversionStartedAt = now;
        ConversionAttempts++;
        Touch();
        return true;
    }

    public void MarkConverted(string pdfStorageKey, DateTime now)
    {
        PdfStorageKey = pdfStorageKey;
        ConversionStatus = FileConversionStatus.Ready;
        ConversionError = null;
        ConvertedAt = now;
        Touch();
    }

    /// <summary>Records a failure; the file goes back to Pending until <paramref name="maxAttempts"/> is reached.</summary>
    public void MarkConversionFailed(string error, int maxAttempts)
    {
        ConversionError = error.Length > 1000 ? error[..1000] : error;
        ConversionStatus = ConversionAttempts >= maxAttempts ? FileConversionStatus.Failed : FileConversionStatus.Pending;
        Touch();
    }

    /// <summary>Queues a failed (or finished) conversion again — e.g. after the conversion service was fixed.</summary>
    public bool RequeueConversion()
    {
        if (StorageKey is null || !ConvertibleExtensions.Contains(Extension)) return false;
        ConversionStatus = FileConversionStatus.Pending;
        ConversionAttempts = 0;
        ConversionError = null;
        Touch();
        return true;
    }

    private static string CleanFileName(string fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(name)) name = "submission";
        return name.Length > 255 ? name[^255..] : name;
    }
}

/// <summary>A comment on a submission (text and/or an audio/video recording). Grader-only comments are hidden from the student.</summary>
public class SubmissionComment : BaseEntity
{
    public Guid SubmissionId { get; private set; }

    /// <summary>The attempt the comment refers to (null = the submission as a whole).</summary>
    public int? AttemptNumber { get; private set; }

    public Guid AuthorUserId { get; private set; }

    /// <summary>True when the author is staff (decides visibility before grades are posted).</summary>
    public bool AuthorIsStaff { get; private set; }

    public string? Body { get; private set; }
    public string? MediaStorageKey { get; private set; }
    public string? MediaContentType { get; private set; }
    public string? MediaFileName { get; private set; }

    /// <summary>Visible to graders only (never to the student).</summary>
    public bool IsPrivateToGraders { get; private set; }

    public bool IsDeleted { get; private set; }

    private SubmissionComment() { }

    public SubmissionComment(Guid submissionId, int? attemptNumber, Guid authorUserId, bool authorIsStaff, string? body, bool isPrivateToGraders)
    {
        var text = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        if (text is { Length: > 4000 }) throw new ArgumentException("Comments are limited to 4000 characters.", nameof(body));
        SubmissionId = submissionId;
        AttemptNumber = attemptNumber;
        AuthorUserId = authorUserId;
        AuthorIsStaff = authorIsStaff;
        Body = text;
        IsPrivateToGraders = authorIsStaff && isPrivateToGraders;
    }

    public void AttachMedia(string storageKey, string contentType, string fileName)
    {
        MediaStorageKey = storageKey;
        MediaContentType = contentType;
        MediaFileName = Path.GetFileName(fileName);
    }

    public void Delete()
    {
        IsDeleted = true;
        Touch();
    }
}

/// <summary>Annotation shapes drawn in the grading viewer.</summary>
public enum AnnotationType
{
    Highlight = 1,
    Strikeout = 2,
    Text = 3,
    Pin = 4,
    Freehand = 5,
    Box = 6
}

/// <summary>
/// An annotation on one page of a submitted file. Geometry is stored in page-relative
/// coordinates (0–1 of the page width/height) so it is independent of zoom.
/// </summary>
public class SubmissionAnnotation : BaseEntity
{
    public Guid SubmissionFileId { get; private set; }
    public int PageNumber { get; private set; }
    public AnnotationType Type { get; private set; }
    public string GeometryJson { get; private set; } = "{}";
    public string? Comment { get; private set; }
    public string Color { get; private set; } = "#f5c400";
    public Guid AuthorUserId { get; private set; }

    private SubmissionAnnotation() { }

    public SubmissionAnnotation(Guid submissionFileId, int pageNumber, AnnotationType type, string geometryJson, string? comment, string? color, Guid authorUserId)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        SubmissionFileId = submissionFileId;
        PageNumber = pageNumber;
        Type = type;
        AuthorUserId = authorUserId;
        Update(geometryJson, comment, color);
    }

    public void Update(string geometryJson, string? comment, string? color)
    {
        GeometryJson = geometryJson;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (Comment is { Length: > 2000 }) throw new ArgumentException("Annotation comments are limited to 2000 characters.", nameof(comment));
        if (!string.IsNullOrWhiteSpace(color)) Color = color.Trim();
        Touch();
    }
}

/// <summary>Plan M3.5 moderated grading: one grader's mark before the moderator decides the final grade.</summary>
public class ProvisionalGrade : BaseEntity
{
    public Guid SubmissionId { get; private set; }
    public Guid GraderUserId { get; private set; }
    public decimal Marks { get; private set; }
    public string? Feedback { get; private set; }
    public bool IsSelected { get; private set; }

    private ProvisionalGrade() { }

    public ProvisionalGrade(Guid submissionId, Guid graderUserId, decimal marks, string? feedback)
    {
        SubmissionId = submissionId;
        GraderUserId = graderUserId;
        Update(marks, feedback);
    }

    public void Update(decimal marks, string? feedback)
    {
        Marks = marks;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        Touch();
    }

    public void Select() { IsSelected = true; Touch(); }
    public void Unselect() { IsSelected = false; Touch(); }
}
