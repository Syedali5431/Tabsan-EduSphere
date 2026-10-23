using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>
/// Plan M13 — the SCORM run-time data model on the server: reads the values the LMS reports on from a commit (the runtime
/// posts its whole CMI object as JSON) and builds the CMI object a launch starts from (learner, entry, mode, credit, total
/// time, mastery score, and on resume everything the SCO stored).
/// </summary>
public static partial class ScormCmi
{
    /// <summary>The CMI object of a commit body ("{ cmi: {...} }", or the object itself). Null when the body is not a CMI object.</summary>
    public static JsonObject? CmiOf(string body)
    {
        try
        {
            if (JsonNode.Parse(body) is not JsonObject root) return null;
            if (root["cmi"] is JsonObject cmi) return cmi;
            return root.ContainsKey("core") || root.ContainsKey("completion_status") || root.ContainsKey("suspend_data") ? root : null;
        }
        catch (JsonException) { return null; }
    }

    public static ScormCommitValues Read(ScormVersion version, JsonObject cmi, decimal? masteryScore)
    {
        if (version == ScormVersion.Scorm12)
        {
            var core = cmi["core"] as JsonObject ?? new JsonObject();
            var score = core["score"] as JsonObject;
            var status = (Text(core["lesson_status"]) ?? "").Trim().ToLowerInvariant();
            var (completion, success) = status switch
            {
                "passed" => ("completed", "passed"),
                "failed" => ("completed", "failed"),
                "completed" => ("completed", "unknown"),
                "incomplete" or "browsed" => ("incomplete", "unknown"),
                "not attempted" or "" => ("not attempted", "unknown"),
                _ => ("unknown", "unknown")
            };
            var raw = Number(score?["raw"]);
            var min = Number(score?["min"]);
            var max = Number(score?["max"]);
            // SCORM 1.2: with a mastery score the LMS decides passed/failed from the raw score when the SCO only says "completed".
            if (success == "unknown" && completion == "completed" && masteryScore is { } mastery && raw is not null)
            {
                var percent = ScormAttempt.Percent(new ScormCommitValues(completion, success, raw, min, max, null, null, null, null, 0));
                success = percent >= mastery ? "passed" : "failed";
            }
            return new ScormCommitValues(completion, success, raw, min, max, null, Text(core["lesson_location"]), Text(cmi["suspend_data"]),
                Text(core["exit"]), Seconds12(Text(core["session_time"])));
        }
        else
        {
            var score = cmi["score"] as JsonObject;
            var completion = Normalize(Text(cmi["completion_status"]), ["completed", "incomplete", "not attempted", "unknown"]);
            var success = Normalize(Text(cmi["success_status"]), ["passed", "failed", "unknown"]);
            var scaled = Number(score?["scaled"]);
            var passing = Number(cmi["scaled_passing_score"]) ?? (masteryScore is { } m ? m / 100 : null);
            if (success == "unknown" && scaled is { } s && passing is { } p) success = s >= p ? "passed" : "failed";
            return new ScormCommitValues(completion, success, Number(score?["raw"]), Number(score?["min"]), Number(score?["max"]), scaled,
                Text(cmi["location"]), Text(cmi["suspend_data"]), Text(cmi["exit"]), Seconds2004(Text(cmi["session_time"])));
        }
    }

    /// <summary>The CMI object a launch starts from. On resume the stored object is reused with the session values reset.</summary>
    public static string Initial(ScormVersion version, ScormAttempt attempt, string learnerId, string learnerName, decimal? masteryScore, bool review)
    {
        var resumable = attempt.CmiJson is { Length: > 0 } stored && CmiOf(stored) is { } o ? Clean(o) : new JsonObject();
        var resuming = attempt.CommitCount > 0;
        var entry = !resuming ? "ab-initio" : string.Equals(attempt.Exit, "suspend", StringComparison.OrdinalIgnoreCase) ? "resume" : "";
        if (version == ScormVersion.Scorm12)
        {
            var core = resumable["core"] as JsonObject ?? new JsonObject();
            resumable["core"] = core;
            core.Remove("session_time");
            core.Remove("exit");
            core["student_id"] = learnerId;
            core["student_name"] = learnerName;
            core["credit"] = review ? "no-credit" : "credit";
            core["lesson_mode"] = review ? "review" : "normal";
            core["entry"] = entry;
            core["total_time"] = Format12(attempt.TotalSeconds);
            if (!resuming) core["lesson_status"] = "not attempted";
            if (masteryScore is { } mastery)
            {
                var data = resumable["student_data"] as JsonObject ?? new JsonObject();
                data["mastery_score"] = mastery.ToString("0.##", CultureInfo.InvariantCulture);
                resumable["student_data"] = data;
            }
        }
        else
        {
            resumable.Remove("session_time");
            resumable.Remove("exit");
            resumable["learner_id"] = learnerId;
            resumable["learner_name"] = learnerName;
            resumable["credit"] = review ? "no-credit" : "credit";
            resumable["mode"] = review ? "review" : "normal";
            resumable["entry"] = entry;
            resumable["total_time"] = Format2004(attempt.TotalSeconds);
            if (masteryScore is { } mastery) resumable["scaled_passing_score"] = (mastery / 100).ToString("0.####", CultureInfo.InvariantCulture);
        }
        return resumable.ToJsonString();
    }

    /// <summary>Drops the runtime's own bookkeeping ("_children", "_count", "_version" …) so the object loads cleanly.</summary>
    private static JsonObject Clean(JsonObject o)
    {
        foreach (var key in o.Select(p => p.Key).Where(k => k.StartsWith('_')).ToList()) o.Remove(key);
        foreach (var child in o.Select(p => p.Value))
        {
            if (child is JsonObject obj) Clean(obj);
            else if (child is JsonArray array) foreach (var item in array.OfType<JsonObject>()) Clean(item);
        }
        return o;
    }

    public static int Seconds12(string? value)
    {
        // HHHH:MM:SS(.SS)
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var m = Time12().Match(value.Trim());
        if (!m.Success) return 0;
        var seconds = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 3600L + int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) * 60L
                      + (long)decimal.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        return (int)Math.Clamp(seconds, 0, int.MaxValue);
    }

    public static int Seconds2004(string? value)
    {
        // ISO 8601 duration: P[nY][nM][nD][T[nH][nM][n(.n)S]]
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var m = Duration().Match(value.Trim());
        if (!m.Success) return 0;
        static decimal G(Match x, int i) => x.Groups[i].Success ? decimal.Parse(x.Groups[i].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = G(m, 1) * 365 * 86400 + G(m, 2) * 30 * 86400 + G(m, 3) * 86400 + G(m, 4) * 3600 + G(m, 5) * 60 + G(m, 6);
        return (int)Math.Clamp(seconds, 0, int.MaxValue);
    }

    public static string Format12(int seconds)
    {
        var s = Math.Max(0, seconds);
        return $"{s / 3600:0000}:{s % 3600 / 60:00}:{s % 60:00}.00";
    }

    public static string Format2004(int seconds)
    {
        var s = Math.Max(0, seconds);
        return $"PT{s / 3600}H{s % 3600 / 60}M{s % 60}S";
    }

    private static string Normalize(string? value, string[] allowed)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return allowed.Contains(v) ? v : "unknown";
    }

    private static string? Text(JsonNode? node) => node switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        JsonValue v => v.ToJsonString(),
        _ => null
    };

    private static decimal? Number(JsonNode? node)
    {
        if (node is not JsonValue v) return null;
        if (v.TryGetValue<decimal>(out var d)) return d;
        if (v.TryGetValue<double>(out var f) && double.IsFinite(f)) return (decimal)f;
        return v.TryGetValue<string>(out var s) && decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"^(\d{1,4}):(\d{1,2}):(\d{1,2}(?:\.\d{1,2})?)$")]
    private static partial Regex Time12();

    [GeneratedRegex(@"^P(?:(\d+(?:\.\d+)?)Y)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)D)?(?:T(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?)?$")]
    private static partial Regex Duration();
}
