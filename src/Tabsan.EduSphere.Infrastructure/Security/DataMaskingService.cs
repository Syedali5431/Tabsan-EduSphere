using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Security;

/// <summary>Phase 5: Data masking for PII protection in UI displays.</summary>
public class DataMaskingService : IDataMaskingService
{
    public string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email ?? string.Empty;
        var at = email.IndexOf('@');
        if (at <= 0) return new string('*', email.Length);
        return email[0] + "***" + email[at..];
    }

    public string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone ?? string.Empty;
        if (phone.Length <= 4) return new string('*', phone.Length);
        return "***" + phone[^4..];
    }

    public string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name ?? string.Empty;
        var parts = name.Split(' ');
        if (parts.Length == 0) return name;
        var last = parts[^1];
        if (last.Length <= 1) return name;
        parts[^1] = last[0] + new string('*', last.Length - 1);
        return string.Join(' ', parts);
    }
}
