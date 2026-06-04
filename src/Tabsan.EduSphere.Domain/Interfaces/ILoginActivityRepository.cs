using Tabsan.EduSphere.Domain.Activity;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Phase 3: Repository contract for login activity logging.
/// Login activity rows are append-only — never updated or deleted.
/// </summary>
public interface ILoginActivityRepository
{
    /// <summary>Appends a new login activity record.</summary>
    Task AddAsync(LoginActivityLog entry, CancellationToken ct = default);

    /// <summary>Commits pending login activity writes.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
