using Tabsan.EduSphere.Application.DTOs.Search;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Search;

/// <summary>
/// Cross-entity search service (Phase 13 — Global Search).
/// Delegates all database queries to <see cref="ISearchRepository"/> and applies
/// role-based scoping logic in the application layer.
/// No new entity or migration is required — queries existing tables.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly ISearchRepository          _search;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IUserRepository            _users;

    public SearchService(
        ISearchRepository          search,
        IAdminAssignmentRepository adminAssignments,
        IUserRepository            users)
    {
        _search           = search;
        _adminAssignments = adminAssignments;
        _users            = users;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var term  = request.Term.Trim();
        var role  = request.CallerRole;
        var limit = Math.Clamp(request.Limit, 1, 100);

        var results = new List<SearchResultItem>();

        try
        {
            switch (role)
            {
                case "SuperAdmin":
                    await AppendSuperAdminResultsAsync(term, limit, results, ct);
                    break;

                case "Admin":
                    await AppendAdminResultsAsync(request.CallerId, term, limit, results, ct);
                    break;

                case "Faculty":
                    await AppendFacultyResultsAsync(request.CallerId, term, limit, results, ct);
                    break;

                case "Student":
                    await AppendStudentResultsAsync(request.CallerId, term, limit, results, ct);
                    break;

                default:
                    break;
            }
        }
        catch (Exception)
        {
            // Swallow and return partial results
        }

        // De-duplicate by (Type, Id) and enforce limit
        var trimmed = results
            .GroupBy(r => (r.Type, r.Id))
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return new SearchResponse(term, trimmed.Count, trimmed);
    }

    // -- SuperAdmin — all data ------------------------------------------------

    private async Task AppendSuperAdminResultsAsync(
        string term, int limit, List<SearchResultItem> results, CancellationToken ct)
    {
        results.AddRange(await _search.SearchDepartmentsAsync(term, null, limit, ct));
        results.AddRange(await _search.SearchCoursesAsync(term, null, limit, ct));
        results.AddRange(await _search.SearchOfferingsAsync(term, null, null, limit, ct));
        results.AddRange(await _search.SearchStudentsAsync(term, null, limit, ct));
        results.AddRange(await _search.SearchFacultyAsync(term, null, limit, ct));
    }

    // -- Admin — assigned department(s) --------------------------------------

    private async Task AppendAdminResultsAsync(
        Guid adminId, string term, int limit, List<SearchResultItem> results, CancellationToken ct)
    {
        var deptIds = (await _adminAssignments.GetDepartmentIdsForAdminAsync(adminId, ct)).ToList();

        if (deptIds.Count == 0) return;

        results.AddRange(await _search.SearchDepartmentsAsync(term, deptIds, limit, ct));
        results.AddRange(await _search.SearchCoursesAsync(term, deptIds, limit, ct));
        results.AddRange(await _search.SearchOfferingsAsync(term, deptIds, null, limit, ct));
        results.AddRange(await _search.SearchStudentsAsync(term, deptIds, limit, ct));
    }

    // -- Faculty — own department data + own offerings ------------------------

    private async Task AppendFacultyResultsAsync(
        Guid facultyUserId, string term, int limit, List<SearchResultItem> results, CancellationToken ct)
    {
        var facultyUser = await _users.GetByIdAsync(facultyUserId, ct);
        if (facultyUser is null) return;

        IReadOnlyList<Guid>? deptIds = facultyUser.DepartmentId.HasValue
            ? new Guid[] { facultyUser.DepartmentId.Value }
            : null;

        if (deptIds is not null)
        {
            results.AddRange(await _search.SearchCoursesAsync(term, deptIds, limit, ct));
            results.AddRange(await _search.SearchStudentsAsync(term, deptIds, limit, ct));
        }

        // Faculty's own offerings
        results.AddRange(await _search.SearchOfferingsAsync(term, null, facultyUserId, limit, ct));
    }

    // -- Student — own profile + enrolled offerings ---------------------------

    private async Task AppendStudentResultsAsync(
        Guid studentUserId, string term, int limit, List<SearchResultItem> results, CancellationToken ct)
    {
        var profileId = await _search.GetStudentProfileIdByUserIdAsync(studentUserId, ct);
        if (profileId is null) return;

        results.AddRange(await _search.SearchStudentEnrolledOfferingsAsync(profileId.Value, term, limit, ct));
    }
}
