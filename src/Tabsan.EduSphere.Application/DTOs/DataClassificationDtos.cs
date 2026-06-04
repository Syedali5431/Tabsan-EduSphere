namespace Tabsan.EduSphere.Application.DTOs.DataClassification;

/// <summary>Phase 5: Request to classify an entity.</summary>
public sealed class CreateClassificationRequest
{
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string ClassificationLevel { get; set; } = "Internal";
    public string? Justification { get; set; }
}

/// <summary>Phase 5: Flat response DTO for a classification entry.</summary>
public sealed class DataClassificationItemDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string ClassificationLevel { get; set; } = string.Empty;
    public Guid ClassifiedBy { get; set; }
    public DateTime ClassifiedAt { get; set; }
    public string? Justification { get; set; }
}

/// <summary>Phase 5: Request for encryption/decryption operations.</summary>
public sealed class CryptoRequest
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>Phase 5: Response for encryption/decryption operations.</summary>
public sealed class CryptoResponse
{
    public string Result { get; set; } = string.Empty;
}
