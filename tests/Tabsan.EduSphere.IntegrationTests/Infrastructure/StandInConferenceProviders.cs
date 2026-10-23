using System.Collections.Concurrent;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Domain.LiveClasses;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M10: test-only conferencing providers that never call BigBlueButton or Zoom. They record what the API asked for
/// and return participant reports that tests set per meeting.
/// </summary>
public abstract class StandInConferenceProvider : IConferenceProvider
{
    public abstract ConferenceProviderType Type { get; }
    public ConcurrentQueue<ConferenceMeetingRequest> Created { get; } = new();
    public ConcurrentQueue<ConferenceJoinRequest> Joins { get; } = new();
    public ConcurrentQueue<string> Cancelled { get; } = new();
    public ConcurrentDictionary<string, IReadOnlyList<ConferenceParticipant>> Participants { get; } = new();
    public ConcurrentQueue<ConferenceConfig> Configs { get; } = new();

    public Task<ConferenceMeeting> CreateAsync(ConferenceConfig config, ConferenceMeetingRequest request, CancellationToken ct = default)
    {
        Configs.Enqueue(config);
        Created.Enqueue(request);
        var id = request.SessionId.ToString("N");
        return Task.FromResult(Type == ConferenceProviderType.BigBlueButton
            ? new ConferenceMeeting(id, null, null)
            : new ConferenceMeeting(id, $"https://meet.example.test/j/{id}", $"https://meet.example.test/s/{id}?host=1"));
    }

    public Task<string?> PersonalJoinUrlAsync(ConferenceConfig config, ConferenceJoinRequest request, CancellationToken ct = default)
    {
        Joins.Enqueue(request);
        return Task.FromResult<string?>(Type == ConferenceProviderType.BigBlueButton
            ? $"https://bbb.example.test/join?meetingID={request.MeetingId}&role={(request.Moderator ? "MODERATOR" : "VIEWER")}"
            : null);
    }

    public Task<IReadOnlyList<ConferenceParticipant>?> ParticipantsAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
        => Task.FromResult(Participants.TryGetValue(externalId, out var list) ? list : Type == ConferenceProviderType.BigBlueButton ? null : (IReadOnlyList<ConferenceParticipant>?)[]);

    public Task<string?> RecordingUrlAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
        => Task.FromResult<string?>($"https://meet.example.test/rec/{externalId}");

    public Task CancelAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        Cancelled.Enqueue(externalId);
        return Task.CompletedTask;
    }

    public Task TestAsync(ConferenceConfig config, CancellationToken ct = default)
        => config.Secret == "wrong" ? throw new InvalidOperationException("the shared secret is wrong") : Task.CompletedTask;
}

public sealed class StandInBigBlueButton : StandInConferenceProvider
{
    public override ConferenceProviderType Type => ConferenceProviderType.BigBlueButton;
}

public sealed class StandInZoom : StandInConferenceProvider
{
    public override ConferenceProviderType Type => ConferenceProviderType.Zoom;
}
