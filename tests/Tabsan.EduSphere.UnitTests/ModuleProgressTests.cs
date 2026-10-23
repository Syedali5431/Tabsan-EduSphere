using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M7 — completion requirements, module locks (date, prerequisites, sequential) and the module item model.</summary>
public class ModuleProgressTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static ModuleItemSnapshot Item(string title, ModuleRequirementType req = ModuleRequirementType.MustView, decimal? min = null)
        => new(Guid.NewGuid(), title, req, min);

    private static ModuleSnapshot Module(string title, IReadOnlyList<ModuleItemSnapshot> items, DateTime? unlock = null, bool sequential = false,
        ModuleCompletionMode mode = ModuleCompletionMode.All, params Guid[] prerequisites)
        => new(Guid.NewGuid(), title, unlock, sequential, mode, prerequisites, items);

    private static Func<Guid, ModuleItemEvidence> Evidence(Dictionary<Guid, ModuleItemEvidence> map)
        => id => map.TryGetValue(id, out var e) ? e : ModuleItemEvidence.None;

    [Theory]
    [InlineData(ModuleRequirementType.MustView, true, false, false, null, false, true)]
    [InlineData(ModuleRequirementType.MustMarkDone, true, false, false, null, false, false)]
    [InlineData(ModuleRequirementType.MustMarkDone, false, true, false, null, false, true)]
    [InlineData(ModuleRequirementType.MustSubmit, false, false, true, null, false, true)]
    [InlineData(ModuleRequirementType.MustScoreAtLeast, false, false, true, 69.99, false, false)]
    [InlineData(ModuleRequirementType.MustScoreAtLeast, false, false, true, 70.0, false, true)]
    [InlineData(ModuleRequirementType.MustContribute, true, false, false, null, true, true)]
    [InlineData(ModuleRequirementType.None, true, true, true, 100.0, true, false)]
    public void Requirements(ModuleRequirementType type, bool viewed, bool done, bool submitted, double? score, bool contributed, bool met)
        => Assert.Equal(met, ModuleProgressEvaluator.RequirementMet(type, 70,
            new ModuleItemEvidence(viewed, done, submitted, (decimal?)score, contributed, false)));

    [Fact]
    public void UnlockDate_And_Prerequisites_LockModules_WithReasons()
    {
        var a1 = Item("Read the syllabus");
        var a = Module("Week 1", [a1]);
        var b = Module("Week 2", [Item("Lab")], prerequisites: a.Id);
        var c = Module("Week 3", [Item("Quiz")], unlock: Now.AddDays(2));

        var states = ModuleProgressEvaluator.Evaluate([a, b, c], Evidence(new()), Now);
        Assert.False(states[0].Locked);
        Assert.True(states[1].Locked);
        Assert.Equal("Complete “Week 1” first", states[1].LockReason);
        Assert.True(states[1].Items[0].Locked);
        Assert.StartsWith("Opens 21 Oct 2026", states[2].LockReason);

        // Viewing the syllabus completes Week 1, which opens Week 2.
        states = ModuleProgressEvaluator.Evaluate([a, b, c], Evidence(new() { [a1.Id] = ModuleItemEvidence.None with { Viewed = true } }), Now);
        Assert.True(states[0].Completed);
        Assert.True(states[0].Items[0].NewlyCompleted);
        Assert.False(states[1].Locked);
        Assert.Equal(100, states[0].Percent);
        Assert.Equal(33, ModuleProgressEvaluator.CoursePercent(states));
    }

    [Fact]
    public void Sequential_LocksEverythingAfterTheFirstIncompleteRequirement()
    {
        var read = Item("Read");
        var note = Item("Note", ModuleRequirementType.None);
        var quiz = Item("Quiz", ModuleRequirementType.MustScoreAtLeast, 80);
        var m = Module("Week 1", [note, read, quiz], sequential: true);

        var s = ModuleProgressEvaluator.Evaluate([m], Evidence(new() { [quiz.Id] = new ModuleItemEvidence(false, false, true, 90, false, false) }), Now)[0];
        Assert.False(s.Items[0].Locked); // before any requirement
        Assert.False(s.Items[1].Locked);
        Assert.True(s.Items[2].Locked);
        Assert.Equal("Complete “Read” first", s.Items[2].LockReason);
        Assert.False(s.Items[2].Completed); // a locked item never completes, even with a score

        s = ModuleProgressEvaluator.Evaluate([m], Evidence(new()
        {
            [read.Id] = ModuleItemEvidence.None with { Viewed = true },
            [quiz.Id] = new ModuleItemEvidence(false, false, true, 90, false, false)
        }), Now)[0];
        Assert.True(s.Completed);
        Assert.Equal(2, s.Done);
    }

    [Fact]
    public void Completion_IsSticky_AndAnyModeNeedsOne()
    {
        var quiz = Item("Quiz", ModuleRequirementType.MustScoreAtLeast, 80);
        var other = Item("Other");
        var all = Module("All", [quiz, other]);
        var any = Module("Any", [quiz, other], mode: ModuleCompletionMode.Any);

        // Regraded below the minimum, but the stored completion keeps it done.
        var evidence = Evidence(new() { [quiz.Id] = new ModuleItemEvidence(false, false, true, 50, false, CompletedBefore: true) });
        Assert.True(ModuleProgressEvaluator.Evaluate([all], evidence, Now)[0].Items[0].Completed);
        Assert.False(ModuleProgressEvaluator.Evaluate([all], evidence, Now)[0].Completed);
        Assert.True(ModuleProgressEvaluator.Evaluate([any], evidence, Now)[0].Completed);
        Assert.False(ModuleProgressEvaluator.Evaluate([all], evidence, Now)[0].Items[0].NewlyCompleted);
    }

    [Fact]
    public void ModulesWithoutRequirements_CompleteWhenOpen_AndCyclesStayLocked()
    {
        var empty = Module("Intro", [Item("Header", ModuleRequirementType.None)]);
        var state = ModuleProgressEvaluator.Evaluate([empty], Evidence(new()), Now)[0];
        Assert.True(state.Completed);
        Assert.Equal(100, state.Percent);

        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var a = new ModuleSnapshot(aId, "A", null, false, ModuleCompletionMode.All, [bId], [Item("x", ModuleRequirementType.None)]);
        var b = new ModuleSnapshot(bId, "B", null, false, ModuleCompletionMode.All, [aId], [Item("y", ModuleRequirementType.None)]);
        var states = ModuleProgressEvaluator.Evaluate([a, b], Evidence(new()), Now);
        Assert.True(states.All(s => s.Locked));
        Assert.True(states.All(s => !s.Completed));
    }

    [Fact]
    public void ModuleItem_ValidatesTypeTargetUrlAndRequirement()
    {
        var module = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new ModuleItem(module, Guid.NewGuid(), ModuleItemType.Quiz, null, "Quiz", null, null, 1, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new ModuleItem(module, Guid.NewGuid(), ModuleItemType.ExternalUrl, null, "Link", "javascript:alert(1)", null, 1, Guid.NewGuid()));
        var link = new ModuleItem(module, Guid.NewGuid(), ModuleItemType.ExternalUrl, Guid.NewGuid(), " Docs ", "https://docs.example.org/x", "ignored", 1, Guid.NewGuid());
        Assert.Null(link.TargetId);
        Assert.Null(link.Body);
        Assert.Equal("Docs", link.Title);

        var page = new ModuleItem(module, Guid.NewGuid(), ModuleItemType.Page, null, "Notes", null, " Line 1\nLine 2 ", 1, Guid.NewGuid());
        Assert.Equal("Line 1\nLine 2", page.Body);
        page.Update("Notes", null, null, 9);
        Assert.Equal(3, page.IndentLevel);
        Assert.Throws<ArgumentException>(() => page.SetRequirement(ModuleRequirementType.MustSubmit, null));

        var quiz = new ModuleItem(module, Guid.NewGuid(), ModuleItemType.Quiz, Guid.NewGuid(), "Quiz", null, null, 1, Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => quiz.SetRequirement(ModuleRequirementType.MustScoreAtLeast, null));
        Assert.Throws<ArgumentException>(() => quiz.SetRequirement(ModuleRequirementType.MustScoreAtLeast, 120));
        Assert.True(quiz.SetRequirement(ModuleRequirementType.MustScoreAtLeast, 75));
        Assert.False(quiz.SetRequirement(ModuleRequirementType.MustScoreAtLeast, 75));
        Assert.True(quiz.SetRequirement(ModuleRequirementType.MustSubmit, 75));
        Assert.Null(quiz.MinScorePercent);

        Assert.Equal([ModuleRequirementType.None], ModuleItem.AllowedRequirements(ModuleItemType.Header));
        Assert.Contains(ModuleRequirementType.MustContribute, ModuleItem.AllowedRequirements(ModuleItemType.Discussion));
        Assert.Throws<ArgumentException>(() => new ModulePrerequisite(module, module));
    }

    [Fact]
    public void Progress_MarkDoneAndUndo()
    {
        var p = new ModuleItemProgress(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        p.MarkViewed(Now);
        p.MarkViewed(Now.AddHours(1));
        Assert.Equal(Now, p.ViewedAt); // first view kept
        p.MarkDone(Now);
        p.Complete(Now);
        p.UndoDone();
        Assert.Null(p.MarkedDoneAt);
        Assert.Null(p.CompletedAt);
    }
}
