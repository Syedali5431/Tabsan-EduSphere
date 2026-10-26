namespace Tabsan.EduSphere.Web.Services;

/// <summary>
/// The institution types the portal shows, from the API's capability matrix: the institution policy narrowed to the types
/// that have data. In a School-only installation only School is shown; College and University menus, options, cards and
/// reports reappear by themselves once their departments exist. Values are the API's InstitutionType numbers
/// (0 University, 1 School, 2 College).
/// </summary>
public sealed record VisibleInstitutionTypes(bool School, bool College, bool University)
{
    /// <summary>Everything shown (when the matrix cannot be read; the API still enforces its own rules).</summary>
    public static readonly VisibleInstitutionTypes All = new(true, true, true);

    /// <summary>True when the given InstitutionType number is shown.</summary>
    public bool Shows(int institutionType) => institutionType switch
    {
        0 => University,
        1 => School,
        2 => College,
        _ => true
    };

    /// <summary>The shown types in the portal's usual order (University, School, College).</summary>
    public IReadOnlyList<int> Types => new[] { 0, 1, 2 }.Where(Shows).ToList();

    /// <summary>The requested type when it is shown; otherwise <paramref name="fallback"/> when shown, else the first shown type.</summary>
    public int Resolve(int? requested, int fallback = 0)
        => requested is { } r && Shows(r) ? r : Shows(fallback) ? fallback : Types.FirstOrDefault(fallback);
}

public static class InstitutionVisibilityExtensions
{
    private const string ItemKey = "Tabsan.VisibleInstitutionTypes";

    /// <summary>The institution types to show, read once per request.</summary>
    public static async Task<VisibleInstitutionTypes> GetVisibleInstitutionTypesAsync(this IEduApiClient api, HttpContext http, CancellationToken ct)
    {
        if (http.Items[ItemKey] is VisibleInstitutionTypes cached) return cached;
        VisibleInstitutionTypes result;
        try
        {
            var matrix = api.IsConnected() ? await api.GetPortalCapabilityMatrixAsync(ct) : null;
            result = matrix is null ? VisibleInstitutionTypes.All : new VisibleInstitutionTypes(matrix.IncludeSchool, matrix.IncludeCollege, matrix.IncludeUniversity);
            if (!result.School && !result.College && !result.University) result = VisibleInstitutionTypes.All;
        }
        catch
        {
            result = VisibleInstitutionTypes.All; // fail open, like the portal's other capability checks
        }
        http.Items[ItemKey] = result;
        return result;
    }
}
