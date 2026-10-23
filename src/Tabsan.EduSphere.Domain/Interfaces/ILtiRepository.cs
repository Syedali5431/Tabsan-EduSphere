using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Course offering facts an LTI launch needs (context claim, deployment scope).</summary>
public sealed record LtiOfferingContext(Guid OfferingId, string CourseCode, string CourseTitle, string? SemesterName, Guid? DepartmentId, Guid? TenantId, Guid? CampusId);

/// <summary>A person as the platform describes them to a tool.</summary>
public sealed record LtiPerson(Guid UserId, string? FullName, string? Email, string Role, Guid? StudentProfileId, bool IsActive);

/// <summary>Plan M5 data access for tools, deployments, links, line items, scores, launches, nonces and access tokens.</summary>
public interface ILtiRepository
{
    Task<IReadOnlyList<LtiTool>> GetToolsAsync(CancellationToken ct = default);
    Task<LtiTool?> GetToolAsync(Guid toolId, CancellationToken ct = default);
    Task<LtiTool?> GetToolByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<LtiToolDeployment?> GetDeploymentAsync(Guid deploymentRowId, CancellationToken ct = default);

    /// <summary>The most specific active deployment of the tool that covers the offering (course, then department, then installation).</summary>
    Task<LtiToolDeployment?> FindDeploymentForOfferingAsync(Guid toolId, Guid offeringId, CancellationToken ct = default);

    /// <summary>Active tools with a deployment covering the offering.</summary>
    Task<IReadOnlyList<LtiTool>> GetToolsForOfferingAsync(Guid offeringId, CancellationToken ct = default);

    Task<IReadOnlyList<LtiResourceLink>> GetLinksForOfferingAsync(Guid offeringId, CancellationToken ct = default);
    Task<LtiResourceLink?> GetLinkAsync(Guid linkId, CancellationToken ct = default);

    Task<IReadOnlyList<LtiLineItem>> GetLineItemsAsync(Guid toolId, Guid offeringId, CancellationToken ct = default);
    Task<LtiLineItem?> GetLineItemAsync(Guid lineItemId, CancellationToken ct = default);
    Task<IReadOnlyList<LtiScore>> GetScoresAsync(Guid lineItemId, CancellationToken ct = default);

    Task<LtiLaunch?> GetLaunchAsync(Guid launchId, CancellationToken ct = default);

    /// <summary>Records a nonce/jti; false when it was already used (replay).</summary>
    Task<bool> TryUseNonceAsync(Guid toolId, string value, DateTime expiresAt, CancellationToken ct = default);

    Task<LtiAccessToken?> GetAccessTokenAsync(string tokenHash, CancellationToken ct = default);

    Task<LtiOfferingContext?> GetOfferingContextAsync(Guid offeringId, CancellationToken ct = default);
    Task<LtiPerson?> GetPersonAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Enrolled students and the offering's teachers.</summary>
    Task<IReadOnlyList<(LtiPerson Person, bool IsInstructor)>> GetOfferingMembersAsync(Guid offeringId, CancellationToken ct = default);

    /// <summary>Removes expired launches, nonces and access tokens.</summary>
    Task PurgeExpiredAsync(DateTime now, CancellationToken ct = default);

    void Add<T>(T entity) where T : BaseEntity;
    void Remove<T>(T entity) where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
