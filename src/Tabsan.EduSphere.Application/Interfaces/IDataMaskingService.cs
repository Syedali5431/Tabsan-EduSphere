namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 5: Data masking utility for protecting PII in UI displays.
/// ISO 27001 A.18.1.4 — privacy and protection of personally identifiable information.
/// </summary>
public interface IDataMaskingService
{
    /// <summary>Masks an email address: j***@domain.com</summary>
    string MaskEmail(string email);

    /// <summary>Masks a phone number: ***1234</summary>
    string MaskPhone(string phone);

    /// <summary>Masks a name: John D***</summary>
    string MaskName(string name);
}
