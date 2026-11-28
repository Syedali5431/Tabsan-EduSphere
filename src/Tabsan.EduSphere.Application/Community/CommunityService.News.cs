using Tabsan.EduSphere.Domain.Community;

namespace Tabsan.EduSphere.Application.Community;

// News: posts for an audience, scheduled or straight away, pinned to the top, taken down when no longer needed. The
// audience is told once when a post goes live; staff see how many have read it.

public sealed partial class CommunityService
{
    public const int NewsFeedLimit = 100;

    /// <summary>Live posts for the person: pinned first, then newest.</summary>
    public async Task<IReadOnlyList<NewsSummaryDto>> NewsFeedAsync(CommunityMember m, DateTime now, CancellationToken ct = default)
    {
        var posts = (await _store.NewsAsync(m.TenantId, ct)).Where(p => p.IsLive(now) && Sees(p, m))
            .OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.PublishAt).Take(NewsFeedLimit).ToList();
        if (posts.Count == 0) return [];
        var ids = posts.Select(p => p.Id).ToList();
        var read = await _store.ReadPostsAsync(m.UserId, ids, ct);
        var names = await _store.AudienceNamesAsync(posts, ct);
        var authors = await _store.NamesAsync(posts.Select(p => p.AuthorUserId).Distinct().ToList(), ct);
        return posts.Select(p => new NewsSummaryDto(p.Id, p.Title, p.Summary ?? Short(p.Body, 220), p.ImageUrl, p.PublishAt, p.IsPinned,
            InAudience(p, m) && !read.Contains(p.Id), authors.GetValueOrDefault(p.AuthorUserId) ?? "", Describe(p, names))).ToList();
    }

    /// <summary>A post (marked read). Others' drafts, withdrawn and future posts only for those who manage them.</summary>
    public async Task<CommunityResult<NewsPostDto>> NewsPostAsync(CommunityMember m, Guid id, DateTime now, CancellationToken ct = default)
    {
        var post = await _store.NewsPostAsync(id, ct);
        var manage = post is not null && CanManage(post, m);
        if (post is null || (!manage && !(post.IsLive(now) && Sees(post, m)))) return CommunityResult<NewsPostDto>.NotFound("News post not found.");
        if (post.IsLive(now) && InAudience(post, m) && (await _store.ReadPostsAsync(m.UserId, [id], ct)).Count == 0)
        {
            _store.Add(new NewsRead(id, m.UserId, now));
            await _store.SaveChangesAsync(ct);
        }
        return CommunityResult<NewsPostDto>.Success(await NewsDtoAsync(post, m, manage, ct));
    }

    /// <summary>Posts the person manages (drafts, scheduled, live, ended, withdrawn), newest first.</summary>
    public async Task<CommunityResult<IReadOnlyList<NewsManageDto>>> ManageNewsAsync(CommunityMember m, DateTime now, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<IReadOnlyList<NewsManageDto>>.Forbidden("Only teachers and admins write for the school.");
        var posts = (await _store.NewsAsync(m.TenantId, ct)).Where(p => CanManage(p, m)).OrderByDescending(p => p.PublishAt).Take(300).ToList();
        var seen = await _store.SeenCountsAsync(posts.Select(p => p.Id).ToList(), ct);
        var names = await _store.AudienceNamesAsync(posts, ct);
        var authors = await _store.NamesAsync(posts.Select(p => p.AuthorUserId).Distinct().ToList(), ct);
        IReadOnlyList<NewsManageDto> list = posts.Select(p => new NewsManageDto(p.Id, p.Title, NewsState(p, now), p.PublishAt, p.ExpiresAt, p.IsPinned,
            p.IsLive(now), authors.GetValueOrDefault(p.AuthorUserId) ?? "", Describe(p, names), seen.GetValueOrDefault(p.Id))).ToList();
        return CommunityResult<IReadOnlyList<NewsManageDto>>.Success(list);
    }

    /// <summary>Writes a new post or changes one. <see cref="SaveNewsRequest.Publish"/> false keeps (or puts) it in drafts.</summary>
    public async Task<CommunityResult<NewsPostDto>> SaveNewsAsync(CommunityMember m, Guid? id, SaveNewsRequest r, DateTime now, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<NewsPostDto>.Forbidden("Only teachers and admins write for the school.");
        NewsPost? post = null;
        if (id is { } existing)
        {
            post = await _store.NewsPostAsync(existing, ct);
            if (post is null) return CommunityResult<NewsPostDto>.NotFound("News post not found.");
            if (!CanManage(post, m)) return CommunityResult<NewsPostDto>.Forbidden("You cannot change this post.");
        }
        try
        {
            if (post is null) post = new NewsPost(m.UserId, r.Title ?? "", r.Summary, r.Body, r.ImageUrl, now);
            else post.Edit(r.Title ?? "", r.Summary, r.Body, r.ImageUrl);
            post.Schedule(r.PublishAt ?? (id is null || post.Status != NewsStatus.Published ? now : post.PublishAt), r.ExpiresAt, r.Pinned);
            if (await ApplyAudienceAsync(post, m, r.Audience, ct) is { } error) return CommunityResult<NewsPostDto>.Invalid(error);
        }
        catch (ArgumentException ex) { return CommunityResult<NewsPostDto>.Invalid(ex.Plain()); }
        if (r.Publish) post.Publish(); else post.Unpublish();
        if (id is null) _store.Add(post);
        await _store.SaveChangesAsync(ct);
        if (post.IsLive(now) && post.NotifiedAt is null) await AnnounceAsync(post, now, ct);
        await AuditAsync(id is null ? "NewsPostCreated" : "NewsPostUpdated", "NewsPost", post.Id, m, ct);
        return CommunityResult<NewsPostDto>.Success(await NewsDtoAsync(post, m, true, ct), id is null ? 201 : 200);
    }

    public async Task<CommunityResult<bool>> WithdrawNewsAsync(CommunityMember m, Guid id, CancellationToken ct = default)
    {
        var post = await _store.NewsPostAsync(id, ct);
        if (post is null) return CommunityResult<bool>.NotFound("News post not found.");
        if (!CanManage(post, m)) return CommunityResult<bool>.Forbidden("You cannot change this post.");
        if (post.Status == NewsStatus.Draft && post.NotifiedAt is null) _store.Remove(post); // never seen: no record to keep
        else post.Withdraw();
        await _store.SaveChangesAsync(ct);
        await AuditAsync("NewsPostWithdrawn", "NewsPost", id, m, ct);
        return CommunityResult<bool>.Success(true);
    }

    private async Task AnnounceAsync(NewsPost post, DateTime now, CancellationToken ct)
    {
        post.MarkNotified(now);
        await _store.SaveChangesAsync(ct);
        await NotifyAsync(post, post.Title, post.Summary ?? Short(post.Body, 200), null, ct);
    }

    private async Task<NewsPostDto> NewsDtoAsync(NewsPost p, CommunityMember m, bool manage, CancellationToken ct)
    {
        var names = await _store.AudienceNamesAsync([p], ct);
        var author = (await _store.NamesAsync([p.AuthorUserId], ct)).GetValueOrDefault(p.AuthorUserId) ?? "";
        int? seen = null, size = null;
        if (manage)
        {
            seen = (await _store.SeenCountsAsync([p.Id], ct)).GetValueOrDefault(p.Id);
            size = (await _store.PeopleAsync(p, ct)).UserIds(p.Roles).Count;
        }
        return new NewsPostDto(p.Id, p.Title, p.Summary, p.Body, p.ImageUrl, p.PublishAt, p.ExpiresAt, p.IsPinned, p.Status.ToString(), author,
            Describe(p, names), manage, seen, size);
    }

    /// <summary>"Draft", "Scheduled", "Live", "Ended" or "Withdrawn".</summary>
    public static string NewsState(NewsPost p, DateTime now) => p.Status switch
    {
        NewsStatus.Draft => "Draft",
        NewsStatus.Withdrawn => "Withdrawn",
        _ when p.PublishAt > now => "Scheduled",
        _ when p.ExpiresAt is { } e && e <= now => "Ended",
        _ => "Live"
    };
}
