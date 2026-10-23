using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Web;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>
/// Plan M8 — late policy &amp; groups page, assignment options ("Assign to" dates, group work, peer review), the student's
/// groups, peer review page, submission-page extras, group discussions and the workspace hooks, against a fake API.
/// </summary>
public class AssignmentEnhancementsWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private const string AssignmentId = "c8000000-0000-0000-0000-000000000001";
    private const string SetId = "e8000000-0000-0000-0000-000000000001";
    private const string GroupA = "f8000000-0000-0000-0000-00000000000a";
    private const string GroupB = "f8000000-0000-0000-0000-00000000000b";
    private const string Ann = "d8000000-0000-0000-0000-000000000001";
    private const string Ben = "d8000000-0000-0000-0000-000000000002";
    private const string Cara = "d8000000-0000-0000-0000-000000000003";
    private const string OverrideId = "a8000000-0000-0000-0000-000000000001";
    private const string ReviewId = "b8000000-0000-0000-0000-000000000001";
    private const string CriterionId = "98000000-0000-0000-0000-000000000001";
    private const string LevelStrong = "98000000-0000-0000-0000-0000000000a2";
    private const string SubmissionId = "78000000-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public AssignmentEnhancementsWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task PoliciesPage_ShowsPolicyAndGroups_AndFormsReachTheApi()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/AssignmentPolicies?offeringId={OfferingId}");
        var text = WebUtility.HtmlDecode(html);
        Assert.Contains("Late work loses 10% of the points per day late (never below 40%).", text);
        Assert.Contains("data-group-set=\"Project teams\"", html);
        Assert.Contains("data-group=\"Team A\"", html);
        Assert.Contains("Ann Lee", html);
        Assert.Contains("Not in a group: Cara Diaz", text);
        Assert.Contains("name=\"lateDeductionEnabled\" value=\"false\"", html); // unchecked boxes still post false

        var token = Token(html);
        var save = await client.PostAsync("/Portal/SaveLatePolicy", Form(token, ("offeringId", OfferingId), ("lateDeductionEnabled", "true"), ("lateDeductionEnabled", "false"),
            ("deductionPercent", "5"), ("interval", "Hour"), ("lowestGradePercent", "50"), ("missingEnabled", "false"), ("missingGradePercent", "0")));
        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        using (var doc = JsonDocument.Parse(api.Body("PUT", $"/api/v1/assignment/offerings/{OfferingId}/late-policy")))
        {
            Assert.True(doc.RootElement.GetProperty("lateDeductionEnabled").GetBoolean());
            Assert.False(doc.RootElement.GetProperty("missingEnabled").GetBoolean());
            Assert.Equal("Hour", doc.RootElement.GetProperty("interval").GetString());
            Assert.Equal(5m, doc.RootElement.GetProperty("deductionPercent").GetDecimal());
        }

        await client.PostAsync("/Portal/CreateGroupSet", Form(token, ("offeringId", OfferingId), ("name", "Labs"), ("selfSignup", "false"), ("maxSize", "3")));
        Assert.Contains("\"selfSignup\":false", api.Body("POST", $"/api/v1/assignment/offerings/{OfferingId}/group-sets"));
        await client.PostAsync("/Portal/SplitGroupSet", Form(token, ("setId", SetId), ("offeringId", OfferingId), ("splitBy", "size"), ("splitValue", "4")));
        Assert.Contains("\"groupSize\":4", api.Body("POST", $"/api/v1/assignment/group-sets/{SetId}/random-split"));
        await client.PostAsync("/Portal/AddGroupMember", Form(token, ("groupId", GroupA), ("offeringId", OfferingId), ("studentProfileId", Cara)));
        Assert.Contains(Cara, api.Body("POST", $"/api/v1/assignment/groups/{GroupA}/members"));
        await client.PostAsync("/Portal/RemoveGroupMember", Form(token, ("groupId", GroupA), ("offeringId", OfferingId), ("studentProfileId", Ann)));
        Assert.Contains(api.Writes, w => w == $"DELETE /api/v1/assignment/groups/{GroupA}/members/{Ann}");

        // Students cannot open the teacher page.
        using var student = await SignedInClientAsync(new FakeApi("Student"));
        var refused = await student.GetAsync($"/Portal/AssignmentPolicies?offeringId={OfferingId}");
        Assert.DoesNotContain("data-late-policy", await refused.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OptionsPage_ShowsDatesGroupsAndPeerReviews_AndSendsUtcDates()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/AssignmentOptions?assignmentId={AssignmentId}");
        var text = WebUtility.HtmlDecode(html);
        Assert.Contains("data-assignment-options", html);
        Assert.Contains("value=\"2026-11-02T12:00\"", html); // late window shown in UTC
        Assert.Contains($"<option value=\"{SetId}\" selected=\"selected\">Project teams (2 groups)</option>", html);
        Assert.Contains("data-override=\"Ann Lee\"", html);
        Assert.Contains("medical", text);
        Assert.Contains($"value=\"s:{Ben}\"", html);
        Assert.Contains($"value=\"g:{GroupB}\"", html);
        Assert.Contains("data-peer-overview", html);
        Assert.Contains("1 of 2 done", text);

        var token = Token(html);
        await client.PostAsync("/Portal/SaveAssignmentOptions", Form(token, ("assignmentId", AssignmentId), ("availableFrom", ""), ("availableUntil", "2026-11-03T08:15"),
            ("groupSetId", SetId), ("gradeGroupIndividually", "false"), ("peerReviewCount", "2"), ("peerReviewDueDate", "2026-11-05T09:00"),
            ("peerReviewAnonymous", "true"), ("peerReviewAnonymous", "false")));
        var options = api.Body("PUT", $"/api/v1/assignment/{AssignmentId}/options");
        Assert.Contains("\"availableUntil\":\"2026-11-03T08:15:00Z\"", options);
        Assert.Contains("\"availableFrom\":null", options);
        Assert.Contains($"\"groupSetId\":\"{SetId}\"", options);
        Assert.Contains("\"peerReviewCount\":2", options);
        Assert.Contains("\"peerReviewAnonymous\":true", options);

        await client.PostAsync("/Portal/AddAssignmentOverride", Form(token, ("assignmentId", AssignmentId), ("target", $"g:{GroupB}"), ("dueDate", "2026-11-04T10:00"),
            ("availableFrom", ""), ("availableUntil", ""), ("note", "Field trip")));
        var added = api.Body("POST", $"/api/v1/assignment/{AssignmentId}/overrides");
        Assert.Contains($"\"groupId\":\"{GroupB}\"", added);
        Assert.Contains("\"studentProfileId\":null", added);
        Assert.Contains("\"dueDate\":\"2026-11-04T10:00:00Z\"", added);

        await client.PostAsync("/Portal/RemoveAssignmentOverride", Form(token, ("assignmentId", AssignmentId), ("overrideId", OverrideId)));
        Assert.Contains(api.Writes, w => w == $"DELETE /api/v1/assignment/overrides/{OverrideId}");
        var allocate = await client.PostAsync("/Portal/AllocatePeerReviews", Form(token, ("assignmentId", AssignmentId)));
        Assert.Equal(HttpStatusCode.Redirect, allocate.StatusCode);
        Assert.Contains(api.Writes, w => w == $"POST /api/v1/assignment/{AssignmentId}/peer-reviews/allocate");
    }

    [Fact]
    public async Task Student_GroupsPage_PeerReview_AndSubmissionExtras()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var groups = await client.GetStringAsync($"/Portal/MyGroups?offeringId={OfferingId}");
        Assert.Contains("data-my-group=\"Team A\"", groups);
        Assert.Contains("data-my-group-set=\"Study buddies\"", groups);
        Assert.Contains("aria-label=\"Join Pair 1\"", groups);
        var token = Token(groups);
        await client.PostAsync("/Portal/JoinGroup", Form(token, ("groupId", GroupB), ("offeringId", OfferingId)));
        Assert.Contains(api.Writes, w => w == $"POST /api/v1/assignment/groups/{GroupB}/join");

        var review = await client.GetStringAsync($"/Portal/PeerReview/{ReviewId}");
        Assert.Contains("data-peer-text", review);
        Assert.Contains("A classmate's essay <script>", WebUtility.HtmlDecode(review));
        Assert.DoesNotContain("essay <script>", review); // encoded
        Assert.Contains($"name=\"rubric[{CriterionId}]\"", review);
        var submit = await client.PostAsync("/Portal/SubmitPeerReview", Form(Token(review), ("id", ReviewId), ("assignmentId", AssignmentId),
            ($"rubric[{CriterionId}]", LevelStrong), ("comment", "Clear and convincing")));
        Assert.Equal(HttpStatusCode.Redirect, submit.StatusCode);
        Assert.Equal($"/Portal/AssignmentSubmission?assignmentId={AssignmentId}", submit.Headers.Location!.ToString());
        using (var doc = JsonDocument.Parse(api.Body("POST", $"/api/v1/assignment/peer-reviews/{ReviewId}")))
        {
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("score").ValueKind);
            Assert.Equal(LevelStrong, doc.RootElement.GetProperty("rubric")[0].GetProperty("levelId").GetString());
            Assert.Equal("Clear and convincing", doc.RootElement.GetProperty("comment").GetString());
        }

        var page = await client.GetStringAsync($"/Portal/AssignmentSubmission?assignmentId={AssignmentId}");
        var text = WebUtility.HtmlDecode(page);
        Assert.Contains("data-dates-overridden", page);
        Assert.Contains("Late work loses 10% of the points per day late", text);
        Assert.Contains("Group work — your group <strong>Team A</strong>", text);
        Assert.Contains("data-late-deduction", page);
        Assert.Contains("Marked 15, late deduction −4.", text);
        Assert.Contains($"href=\"/Portal/PeerReview/{ReviewId}\"", page);
        Assert.Contains("Nice structure", page);
        Assert.Contains("Peer 1", page);
    }

    [Fact]
    public async Task Discussion_OffersGroups_AndPostsGroupThreads()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/Discussion?offeringId={OfferingId}");
        Assert.Contains($"<option value=\"{GroupA}\">Group: Team A (Project teams)</option>", html);
        Assert.Contains("data-group-thread>Team A (Project teams)</span>", html);
        await client.PostAsync("/Portal/CreateDiscussionThread", Form(Token(html), ("offeringId", OfferingId), ("title", "Plan"), ("groupId", GroupA)));
        Assert.Contains($"\"groupId\":\"{GroupA}\"", api.Body("POST", "/api/v1/discussion/thread"));
        await client.PostAsync("/Portal/CreateDiscussionThread", Form(Token(html), ("offeringId", OfferingId), ("title", "Everyone"), ("groupId", "")));
        Assert.Contains("\"groupId\":null", api.Bodies("POST", "/api/v1/discussion/thread").Last());
    }

    [Fact]
    public async Task Workspace_LinksToOptions_AndRelaysWaivers()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/GradingWorkspace?assignmentId={AssignmentId}");
        Assert.Contains($"href=\"/Portal/AssignmentOptions?assignmentId={AssignmentId}\"", html);
        Assert.Contains("WorkspaceLatePenalty", html);
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/Portal/WorkspaceLatePenalty?submissionId={SubmissionId}")
        {
            Content = new StringContent("{\"waived\":true}", Encoding.UTF8, "application/json"),
            Headers = { { "RequestVerificationToken", Token(html) } }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"waived\":true}", api.Body("PUT", $"/api/v1/assignment/submissions/{SubmissionId}/late-penalty"));
    }

    // ── helpers ──

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(page), ["username"] = "tester", ["password"] = "Password#123"
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        private readonly List<(string Method, string Path, string Body)> _writes = new();

        public IEnumerable<string> Writes => _writes.Select(w => $"{w.Method} {w.Path}");
        public IEnumerable<string> Bodies(string method, string path) => _writes.Where(w => w.Method == method && w.Path == path).Select(w => w.Body);
        public string Body(string method, string path) => Bodies(method, path).Single();

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string Policy = """{"lateDeductionEnabled":true,"deductionPercent":10,"interval":"Day","lowestGradePercent":40,"missingEnabled":true,"missingGradePercent":0}""";

        private static readonly string GroupSets = $$"""
            [{"id":"{{SetId}}","name":"Project teams","selfSignup":false,"maxSize":null,
              "groups":[{"id":"{{GroupA}}","name":"Team A","memberCount":2,"members":[{"studentProfileId":"{{Ann}}","name":"Ann Lee","registrationNumber":"R-1"},{"studentProfileId":"{{Ben}}","name":"Ben Ng","registrationNumber":"R-2"}]},
                        {"id":"{{GroupB}}","name":"Team B","memberCount":0,"members":[]}],
              "unassigned":[{"studentProfileId":"{{Cara}}","name":"Cara Diaz","registrationNumber":"R-3"}]}]
            """;

        private static readonly string Settings = $$"""
            {"assignmentId":"{{AssignmentId}}","allowedAttempts":1,"allowedFileTypes":[".pdf"],"fileTypesRestricted":true,"maxFileSizeMb":10,
             "anonymousGrading":false,"namesHidden":false,"postingPolicy":"Automatic","moderatedGrading":false}
            """;

        private static readonly string Workspace = $$"""
            {"assignmentId":"{{AssignmentId}}","title":"Essay","courseOfferingId":"{{OfferingId}}","maxMarks":20,"dueDate":"2026-10-30T00:00:00Z","settings":{{Settings}},
             "submissions":[{"submissionId":"{{SubmissionId}}","studentLabel":"Ann Lee","status":"Graded","isLate":true,"attemptNumber":1,"submittedAt":"2026-10-31T00:00:00Z","marksAwarded":11,"isPosted":true,"needsGrading":false,"provisionalGradeCount":0,"isMissing":false,"latePenalty":4,"groupId":"{{GroupA}}"}],
             "gradedCount":1,"postedCount":1,"gradedNotPostedCount":0,"hasRubric":false,"conversionEnabled":false,"canModerate":false}
            """;

        private static readonly string StudentView = $$"""
            {"assignmentId":"{{AssignmentId}}","title":"Essay","description":"Write it","dueDate":"2030-10-30T00:00:00Z","maxMarks":20,"isOpen":true,"settings":{{Settings}},
             "submission":{"submissionId":"{{SubmissionId}}","assignmentId":"{{AssignmentId}}","assignmentTitle":"Essay","courseOfferingId":"{{OfferingId}}",
               "maxMarks":20,"dueDate":"2030-10-30T00:00:00Z","studentLabel":"Me","status":"Graded","currentAttemptNumber":1,"isLate":true,"submittedAt":"2026-10-31T00:00:00Z",
               "marksAwarded":11,"feedback":"Good","isPosted":true,"gradeHiddenFromStudent":false,
               "attempts":[{"attemptNumber":1,"submittedAt":"2026-10-31T00:00:00Z","isLate":true,"textContent":"Hello","files":[]}],
               "comments":[],"settings":{{Settings}},"canResubmit":false,"attemptsRemaining":0,"canModerate":false,
               "rawMarks":15,"latePenalty":4,"latePenaltyWaived":false,"isMissing":false,"groupId":"{{GroupA}}","groupName":"Team A"} }
            """;

        private static readonly string Extras = $$"""
            {"assignmentId":"{{AssignmentId}}","courseOfferingId":"{{OfferingId}}","dueDate":"2026-10-29T00:00:00Z","availableFrom":null,"closesAt":"2026-11-02T00:00:00Z",
             "datesOverridden":true,"latePolicy":{{Policy}},"groupName":"Team A","groupMembers":["Ann Lee","Ben Ng"],"needsGroup":false,
             "peerReviewCount":1,"peerReviewDueDate":"2026-11-05T00:00:00Z",
             "reviewsToDo":[{"id":"{{ReviewId}}","submissionId":"{{SubmissionId}}","revieweeLabel":"Peer submission 1","reviewerLabel":"You","status":"Assigned","score":null,"completedAt":null}],
             "reviewsReceived":[{"reviewerLabel":"Peer 1","score":16,"comment":"Nice structure","completedAt":"2026-11-01T00:00:00Z"}],
             "rawMarks":15,"latePenalty":4,"isMissing":false}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b2","key":"assignments","name":"Assignments","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000b3","key":"discussion","name":"Discussion","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/course/offerings/my") return Json($$"""[{"id":"{{OfferingId}}","courseTitle":"Programming","semesterName":"Fall 2026"}]""");
            if (path == $"/api/v1/assignment/offerings/{OfferingId}/late-policy") return Json(Policy);
            if (path == $"/api/v1/assignment/offerings/{OfferingId}/group-sets" && request.Method == HttpMethod.Get) return Json(GroupSets);
            if (path == $"/api/v1/assignment/offerings/{OfferingId}/roster")
                return Json($$"""[{"studentProfileId":"{{Ann}}","name":"Ann Lee","registrationNumber":"R-1"},{"studentProfileId":"{{Ben}}","name":"Ben Ng","registrationNumber":"R-2"}]""");
            if (path == $"/api/v1/assignment/{AssignmentId}/workspace") return Json(Workspace);
            if (path == $"/api/v1/assignment/{AssignmentId}/options" && request.Method == HttpMethod.Get)
                return Json($$"""{"assignmentId":"{{AssignmentId}}","dueDate":"2026-10-30T00:00:00Z","availableFrom":null,"availableUntil":"2026-11-02T12:00:00Z","groupSetId":"{{SetId}}","gradeGroupIndividually":false,"peerReviewCount":1,"peerReviewDueDate":"2026-11-05T00:00:00Z","peerReviewAnonymous":true,"hasSubmissions":false}""");
            if (path == $"/api/v1/assignment/{AssignmentId}/overrides" && request.Method == HttpMethod.Get)
                return Json($$"""[{"id":"{{OverrideId}}","target":"Student","studentProfileId":"{{Ann}}","groupId":null,"targetName":"Ann Lee","dueDate":"2026-11-01T00:00:00Z","availableFrom":null,"availableUntil":null,"note":"medical"}]""");
            if (path == $"/api/v1/assignment/{AssignmentId}/peer-reviews")
                return Json($$"""{"assignmentId":"{{AssignmentId}}","peerReviewCount":1,"peerReviewDueDate":null,"anonymous":true,"assigned":2,"completed":1,"reviews":[{"id":"{{ReviewId}}","submissionId":"{{SubmissionId}}","revieweeLabel":"Ann Lee","reviewerLabel":"Ben Ng","status":"Completed","score":16,"completedAt":null},{"id":"{{Guid.NewGuid()}}","submissionId":"{{Guid.NewGuid()}}","revieweeLabel":"Ben Ng","reviewerLabel":"Ann Lee","status":"Assigned","score":null,"completedAt":null}]}""");
            if (path == $"/api/v1/assignment/{AssignmentId}/peer-reviews/allocate") return Json("""{"allocated":2}""");
            if (path == $"/api/v1/assignment/offerings/{OfferingId}/my-groups")
                return Json($$"""
                    [{"groupSetId":"{{SetId}}","groupSetName":"Project teams","selfSignup":false,"maxSize":null,
                      "group":{"id":"{{GroupA}}","name":"Team A","memberCount":2,"members":[{"studentProfileId":"{{Ann}}","name":"Ann Lee"},{"studentProfileId":"{{Ben}}","name":"Ben Ng"}]},"joinable":[]},
                     {"groupSetId":"{{Guid.NewGuid()}}","groupSetName":"Study buddies","selfSignup":true,"maxSize":2,"group":null,
                      "joinable":[{"id":"{{GroupB}}","name":"Pair 1","memberCount":1,"members":[]}]}]
                    """);
            if (path == $"/api/v1/assignment/peer-reviews/{ReviewId}" && request.Method == HttpMethod.Get)
                return Json($$"""
                    {"id":"{{ReviewId}}","assignmentId":"{{AssignmentId}}","assignmentTitle":"Essay","revieweeLabel":"Peer submission","maxMarks":20,"dueDate":null,"status":"Assigned",
                     "text":"A classmate's essay <script>","files":[],
                     "rubric":[{"criterionId":"{{CriterionId}}","name":"Argument","maxPoints":6,"levels":[{"levelId":"98000000-0000-0000-0000-0000000000a1","label":"Weak","points":2},{"levelId":"{{LevelStrong}}","label":"Strong","points":6}],"chosenLevelId":null}],
                     "score":null,"comment":null}
                    """);
            if (path == $"/api/v1/assignment/{AssignmentId}/student-view") return Json(StudentView);
            if (path == $"/api/v1/assignment/{AssignmentId}/my-extras") return Json(Extras);
            if (path == $"/api/v1/discussion/{OfferingId}/threads")
                return Json($$"""[{"id":"{{Guid.NewGuid()}}","offeringId":"{{OfferingId}}","title":"Team plan","authorId":"{{Guid.NewGuid()}}","authorName":"ann","ticketNumber":"T-1","replyCount":0,"createdAt":"2026-10-19T00:00:00Z","groupId":"{{GroupA}}","replies":[]}]""");
            if (path == "/api/v1/discussion/thread") return Json($$"""{"id":"{{Guid.NewGuid()}}","offeringId":"{{OfferingId}}","title":"Plan","replies":[]}""", HttpStatusCode.Created);
            if (path.StartsWith("/api/v1/assignment/", StringComparison.Ordinal) && request.Method != HttpMethod.Get)
                return request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent) : Json("{}");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
