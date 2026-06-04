using Tabsan.EduSphere.Application.DTOs.LoginActivity;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 3: Service for querying login activity logs and generating dashboard summaries.
/// ISO 27001 A.12.4.1 — structured event logging for security monitoring.
/// </summary>
public interface ILoginActivityService
{
    /// <summary>Returns paged login activity with optional filters.</summary>
    Task<PagedLoginActivityResult> GetActivityAsync(LoginActivityFilterDto filter, CancellationToken ct = default);

    /// <summary>Returns aggregated summary with daily breakdown for the dashboard.</summary>
    Task<LoginActivitySummaryDto> GetSummaryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
}
