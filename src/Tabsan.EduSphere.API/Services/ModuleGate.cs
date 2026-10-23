using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M7.3 — keeps assignments, quizzes and external tools closed while their module (or an earlier item in a
/// sequential module) is locked for the student. Items that are not placed in any published module are not affected.
/// </summary>
internal static class ModuleGate
{
    /// <summary>The reason the student must wait, or null when the work is open.</summary>
    public static async Task<string?> LockReasonAsync(HttpContext http, Guid? studentProfileId, ModuleItemType type, Guid targetId, CancellationToken ct)
    {
        if (studentProfileId is not { } studentId || studentId == Guid.Empty) return null;
        var service = http.RequestServices.GetService<ModuleItemService>();
        return service is null ? null : await service.LockReasonAsync(studentId, type, targetId, ct);
    }
}
