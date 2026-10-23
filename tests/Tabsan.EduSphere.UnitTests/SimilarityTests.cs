using System.Net;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Similarity;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Similarity;
using Tabsan.EduSphere.Infrastructure.Similarity;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M9 — fingerprinting, scoring and passages, text extraction, the Copyleaks client and webhook, domain rules.</summary>
public class SimilarityTests
{
    private const string Essay =
        "Sunlight reaches the atmosphere as a mixture of every colour. Molecules of nitrogen and oxygen scatter short wavelengths far more " +
        "strongly than long ones, a process known as Rayleigh scattering. Blue light is therefore sent in every direction across the sky, " +
        "while red and orange light travel on almost undisturbed. At sunset the light crosses much more air, so most of the blue is scattered " +
        "away before it reaches us and the sky glows red.";

    private const string Unrelated =
        "Photosynthesis lets plants turn carbon dioxide and water into glucose using the energy of light captured by chlorophyll in their " +
        "leaves, releasing oxygen that animals breathe.";

    private static (IReadOnlyList<TextWord> Words, IReadOnlyList<Fingerprint> Prints) Doc(string text)
    {
        var words = Fingerprinter.Words(text);
        return (words, Fingerprinter.Fingerprints(words));
    }

    /// <summary>The scorer's input: pairs of (position in checked work, position in source) for shared hashes.</summary>
    private static List<(int Position, int SourcePosition)> Pairs(IReadOnlyList<Fingerprint> mine, IReadOnlyList<Fingerprint> source)
    {
        var bySource = source.GroupBy(f => f.Hash).ToDictionary(g => g.Key, g => g.Select(f => f.Position).ToList());
        return mine.Where(f => bySource.ContainsKey(f.Hash)).SelectMany(f => bySource[f.Hash].Select(sp => (f.Position, sp))).ToList();
    }

    private static decimal Score((IReadOnlyList<TextWord> Words, IReadOnlyList<Fingerprint> Prints) mine, (IReadOnlyList<TextWord> Words, IReadOnlyList<Fingerprint> Prints) source)
        => SimilarityScorer.Score(mine.Words.Count, mine.Prints.Select(f => f.Position).Distinct().OrderBy(p => p).ToList(),
            Pairs(mine.Prints, source.Prints).Select(p => p.Position).ToHashSet());

    [Fact]
    public void Words_AreLowercased_WithOriginalPositions()
    {
        var words = Fingerprinter.Words("Don’t PANIC, it's 42!");
        Assert.Equal(["don't", "panic", "it's", "42"], words.Select(w => w.Word));
        Assert.Equal(6, words[1].Start);
        Assert.Equal(5, words[1].Length);
    }

    [Fact]
    public void Fingerprints_AreDeterministic_AndIgnoreCaseAndPunctuation()
    {
        var a = Doc(Essay).Prints;
        var b = Doc(Essay.ToUpperInvariant().Replace(",", " ; ").Replace(".", " !")).Prints;
        Assert.NotEmpty(a);
        Assert.Equal(a.Select(f => f.Hash), b.Select(f => f.Hash));
        Assert.Empty(Fingerprinter.Fingerprints(Fingerprinter.Words("too short")));
        // Winnowing keeps a fraction of the runs, but never leaves a window of Window runs without a fingerprint.
        var runs = Fingerprinter.Words(Essay).Count - Fingerprinter.ShingleSize + 1;
        Assert.True(a.Count < runs);
        Assert.All(a.Zip(a.Skip(1)), p => Assert.True(p.Second.Position - p.First.Position <= Fingerprinter.Window));
    }

    [Fact]
    public void Score_IsFullForCopies_ZeroForUnrelated_PartialForPartialCopies()
    {
        var essay = Doc(Essay);
        Assert.Equal(100m, Score(essay, Doc(Essay)));
        Assert.Equal(0m, Score(essay, Doc(Unrelated)));

        // Half own words, half the copied essay: about half is matched.
        var mixed = Doc(Unrelated + " " + Unrelated + " " + Essay);
        var score = Score(mixed, essay);
        Assert.InRange(score, 55m, 75m);

        // Any shared passage of ShingleSize + Window - 1 words is found, wherever it sits.
        var passage = string.Join(' ', Essay.Split(' ').Skip(20).Take(Fingerprinter.ShingleSize + Fingerprinter.Window - 1));
        var embedded = Doc(Unrelated + " " + passage + " " + Unrelated);
        Assert.NotEmpty(Pairs(embedded.Prints, essay.Prints));
    }

    [Fact]
    public void Passages_PointAtTheCopiedTextInBothDocuments()
    {
        var source = Doc(Essay);
        var text = "My own introduction comes first. " + Essay[..Essay.IndexOf(" At sunset", StringComparison.Ordinal)] + " And my own ending is here.";
        var mine = Doc(text);
        var passages = SimilarityScorer.Passages(mine.Words, source.Words, Pairs(mine.Prints, source.Prints));
        var p = Assert.Single(passages);
        var copied = text.Substring(p.Start, p.Length);
        Assert.StartsWith("Sunlight reaches", copied);
        Assert.EndsWith("almost undisturbed", copied);
        Assert.StartsWith("Sunlight reaches", Essay.Substring(p.SourceStart, p.SourceLength));
    }

    [Fact]
    public void CopyleaksWebhook_ParsesCompletedErrorAndInformational()
    {
        var completed = CopyleaksWebhook.Parse("completed", """
            {"scannedDocument":{"scanId":"x","totalWords":200},
             "results":{"score":{"aggregatedScore":37.5},
               "internet":[{"title":"Why is the sky blue","url":"https://example.org/sky","matchedWords":60,"introduction":"Blue light is scattered"},
                           {"title":"Bad","url":"javascript:alert(1)","matchedWords":10}],
               "database":[{"title":"","matchedWords":20}]}}
            """);
        Assert.True(completed.IsCompleted);
        Assert.Equal(37.5m, completed.Score);
        Assert.Equal(3, completed.Sources.Count);
        Assert.Equal(("Why is the sky blue", "https://example.org/sky", 30m), (completed.Sources[0].Label, completed.Sources[0].Url, completed.Sources[0].Percent));
        Assert.Null(completed.Sources.Single(s => s.Label == "Bad").Url); // only http(s) links are kept
        Assert.Equal("Copyleaks database", completed.Sources.Single(s => s.Kind == "database").Label);

        var error = CopyleaksWebhook.Parse("error", """{"error":{"code":2,"message":"Not enough credits"}}""");
        Assert.True(error.IsError);
        Assert.Equal("Not enough credits", error.Error);
        Assert.False(CopyleaksWebhook.Parse("creditsChecked", "{}").IsCompleted);
    }

    [Fact]
    public async Task Extractor_ReadsTextHtmlDocxAndSimplePdf_AndIgnoresOthers()
    {
        var x = new SubmissionTextExtractor();
        Assert.Equal("plain text", await x.ExtractAsync("a.txt", "text/plain", Encoding.UTF8.GetBytes("plain text")));
        var html = await x.ExtractAsync("a.html", "text/html", Encoding.UTF8.GetBytes("<html><style>p{}</style><p>Fish &amp; chips</p><script>alert(1)</script></html>"));
        Assert.Equal("Fish & chips", html!.Trim());

        using (var stream = new MemoryStream())
        {
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body(new Paragraph(new Run(new Text("First paragraph."))), new Paragraph(new Run(new Text("Second one.")))));
            }
            Assert.Equal("First paragraph.\nSecond one.", await x.ExtractAsync("essay.docx", "application/octet-stream", stream.ToArray()));
        }

        var pdf = await x.ExtractAsync("essay.pdf", "application/pdf", SimplePdf("Why the sky is blue", "Rayleigh scattering"));
        Assert.Contains("Why the sky is blue", pdf);
        Assert.Contains("Rayleigh scattering", pdf);

        Assert.Null(await x.ExtractAsync("photo.png", "image/png", [1, 2, 3]));
        Assert.Null(await x.ExtractAsync("broken.docx", "application/octet-stream", [1, 2, 3])); // damaged → no text, no exception
    }

    /// <summary>A minimal PDF drawing text with the standard Helvetica font.</summary>
    private static byte[] SimplePdf(params string[] lines)
    {
        var content = new StringBuilder();
        var y = 780;
        foreach (var l in lines) { content.Append($"BT /F1 12 Tf 60 {y} Td ({l}) Tj ET\n"); y -= 20; }
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {content.Length} >>\nstream\n{content}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++) { offsets.Add(sb.Length); sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = sb.Length;
        sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) sb.Append($"{o:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    [Fact]
    public async Task CopyleaksClient_LogsInOnce_AndSubmitsTheTextWithTheWebhook()
    {
        var handler = new FakeHandler();
        var client = new CopyleaksClient(new HttpClient(handler), Options.Create(new SimilarityOptions { CopyleaksIdentityUrl = "https://id.test", CopyleaksApiUrl = "https://api.test" }));
        var email = $"{Guid.NewGuid():N}@uni.edu";
        Assert.Equal("tok-1", await client.LoginAsync(email, "key"));
        Assert.Equal("tok-1", await client.LoginAsync(email, "key")); // cached
        Assert.Equal(1, handler.Requests.Count(r => r.Url == "https://id.test/v3/account/login/api"));

        await client.SubmitTextAsync("tok-1", "scan123", "Hello world", "https://api.uni.edu/hook/{STATUS}?check=1&token=t", sandbox: true);
        var submit = handler.Requests.Single(r => r.Method == "PUT");
        Assert.Equal("https://api.test/v3/scans/submit/file/scan123", submit.Url);
        Assert.Equal("Bearer tok-1", submit.Authorization);
        using var body = JsonDocument.Parse(submit.Body);
        Assert.Equal("Hello world", Encoding.UTF8.GetString(Convert.FromBase64String(body.RootElement.GetProperty("base64").GetString()!)));
        Assert.Equal("https://api.uni.edu/hook/{STATUS}?check=1&token=t", body.RootElement.GetProperty("properties").GetProperty("webhooks").GetProperty("status").GetString());
        Assert.True(body.RootElement.GetProperty("properties").GetProperty("sandbox").GetBoolean());

        handler.FailSubmit = true;
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SubmitTextAsync("tok-1", "scan124", "x", "https://h", false));
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<(string Method, string Url, string? Authorization, string Body)> Requests { get; } = [];
        public bool FailSubmit { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.Method.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
            if (request.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($$"""{"access_token":"tok-1",".expires":"{{DateTime.UtcNow.AddDays(2):O}}"}""") };
            return new HttpResponseMessage(FailSubmit ? HttpStatusCode.PaymentRequired : HttpStatusCode.Created) { Content = new StringContent("{}") };
        }
    }

    [Fact]
    public void Check_RetriesWithBackOff_ThenFails_AndClampsScores()
    {
        var now = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
        var check = new SimilarityCheck(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, SimilarityProviderType.Copyleaks, null);
        check.RecordFailure("timeout", now);
        Assert.Equal((SimilarityCheckStatus.Queued, now.AddMinutes(2)), (check.Status, check.NextAttemptAt));
        for (var i = 0; i < SimilarityCheck.MaxTries - 1; i++) check.RecordFailure("timeout", now);
        Assert.Equal(SimilarityCheckStatus.Failed, check.Status);
        Assert.True(check.IsFinished);

        var done = new SimilarityCheck(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, SimilarityProviderType.Internal, null);
        done.Complete(123.456m, "[]", null, now);
        Assert.Equal(100m, done.Score);
    }

    [Fact]
    public void Settings_AndAssignmentOptions_AreValidated()
    {
        var s = new SimilaritySetting(null);
        Assert.True(s.IsComplete); // built-in needs nothing
        s.Configure(SimilarityProviderType.Copyleaks, true, SimilarityScope.Institution, "checker@uni.edu", false);
        Assert.False(s.IsComplete);
        s.SetEncryptedApiKey("cipher");
        Assert.True(s.IsComplete);
        Assert.Throws<ArgumentException>(() => s.Configure(SimilarityProviderType.Copyleaks, true, SimilarityScope.Course, "not-an-email", false));

        var a = new Assignment(Guid.NewGuid(), "Essay", null, DateTime.UtcNow.AddDays(3), 20);
        a.ConfigureSimilarity(false, true);
        Assert.False(a.SimilarityVisibleToStudents); // only shown when checking is on
        a.ConfigureSimilarity(true, true);
        Assert.True(a.SimilarityCheckEnabled && a.SimilarityVisibleToStudents);
    }
}
