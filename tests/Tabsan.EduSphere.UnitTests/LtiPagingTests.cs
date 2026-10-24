using Tabsan.EduSphere.Application.Lti;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M5 — paging of AGS/NRPS containers (found with saLTIre): pages after filtering, rel="next" until the last page.</summary>
public class LtiPagingTests
{
    private const string Url = "https://platform.test/lti/nrps/course/memberships";

    [Fact]
    public void Page_ReturnsTheRequestedSlice_AndTheNextPageUrl_WithTheFilters()
    {
        var all = Enumerable.Range(1, 7).ToList();
        var (first, next) = LtiAgsNrpsService.Page(all, 3, null, 100, 500, Url, ("role", "Learner"), ("tag", null));
        Assert.Equal([1, 2, 3], first);
        Assert.Equal(Url + "?role=Learner&limit=3&page=2", next);

        var (third, last) = LtiAgsNrpsService.Page(all, 3, 3, 100, 500, Url, ("role", "Learner"));
        Assert.Equal([7], third);
        Assert.Null(last);

        var (exact, none) = LtiAgsNrpsService.Page(all.Take(6), 3, 2, 100, 500, Url);
        Assert.Equal([4, 5, 6], exact);
        Assert.Null(none);
    }

    [Fact]
    public void Page_ClampsTheLimit_AndTreatsBadPagesAsTheFirst()
    {
        var all = Enumerable.Range(1, 20).ToList();
        Assert.Equal(5, LtiAgsNrpsService.Page(all, 99, 1, 10, 5, Url).Items.Count);
        Assert.Single(LtiAgsNrpsService.Page(all, 0, 1, 10, 5, Url).Items);
        Assert.Equal(1, LtiAgsNrpsService.Page(all, 2, -4, 10, 5, Url).Items[0]);
        Assert.Contains("limit=10&page=2", LtiAgsNrpsService.Page(all, null, null, 10, 50, Url).NextUrl);
        Assert.Contains("resource_link_id=a%20b", LtiAgsNrpsService.Page(all, 1, 1, 10, 50, Url, ("resource_link_id", "a b")).NextUrl);
    }
}
