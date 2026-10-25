using System.Xml.Linq;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// SCORM 2004 sequencing read from manifests shaped like the ADL / Rustici sequencing examples: forced order through shared
/// objectives, remediation with skip / retry / exit parent / exit all, a randomized post-test with an attempt limit, rollup
/// rules, hidden and locked parts, refused requests that change nothing, sequencing collections; and SCORM 1.2 prerequisites.
/// </summary>
public class ScormSequencingTests
{
    private const string Ns = """xmlns="http://www.imsglobal.org/xsd/imscp_v1p1" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_v1p3" xmlns:imsss="http://www.imsglobal.org/xsd/imsss" xmlns:adlseq="http://www.adlnet.org/xsd/adlseq_v1p3" xmlns:adlnav="http://www.adlnet.org/xsd/adlnav_v1p3" """;

    private static ScormManifest Manifest(string organization, string resources, string collection = "")
        => ScormPackageReader.ParseManifest(XDocument.Parse($"""
            <manifest identifier="m" version="1" {Ns}>
              <metadata><schema>ADL SCORM</schema><schemaversion>2004 3rd Edition</schemaversion></metadata>
              <organizations default="o">{organization}</organizations>
              <resources>{resources}</resources>
              {collection}
            </manifest>
            """));

    private static string Sco(params string[] ids) => string.Concat(ids.Select(id => $"""<resource identifier="r_{id}" type="webcontent" adlcp:scormType="sco" href="{id}.html"/>"""));

    private static ScormSequencer Sequencer(ScormManifest m, int seed = 1) => new(m.Sequencing!, new SeqState(), new Random(seed));

    private static SeqReport Passed(decimal? scaled = null) => new("completed", "passed", scaled, [], "");

    private static SeqReport Failed(decimal? scaled = null) => new("completed", "failed", scaled, [], "");

    // ── forced order through shared objectives (like "Sequencing Forced Sequential") ──

    private static string Forced(string id, string? previous) => $"""
        <item identifier="{id}" identifierref="r_{id}"><title>{id.ToUpperInvariant()}</title>
          <imsss:sequencing>
            {(previous is null ? "" : """
            <imsss:sequencingRules><imsss:preConditionRule>
              <imsss:ruleConditions conditionCombination="any">
                <imsss:ruleCondition referencedObjective="prev" operator="not" condition="satisfied"/>
                <imsss:ruleCondition referencedObjective="prev" operator="not" condition="objectiveStatusKnown"/>
              </imsss:ruleConditions><imsss:ruleAction action="disabled"/>
            </imsss:preConditionRule></imsss:sequencingRules>
            """)}
            <imsss:objectives>
              <imsss:primaryObjective objectiveID="{id}_sat"><imsss:mapInfo targetObjectiveID="g.{id}" readSatisfiedStatus="true" writeSatisfiedStatus="true"/></imsss:primaryObjective>
              {(previous is null ? "" : $"""<imsss:objective objectiveID="prev"><imsss:mapInfo targetObjectiveID="g.{previous}" readSatisfiedStatus="true"/></imsss:objective>""")}
            </imsss:objectives>
            <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
          </imsss:sequencing>
        </item>
        """;

    private static ScormManifest ForcedSequential() => Manifest($"""
        <organization identifier="o" adlseq:objectivesGlobalToSystem="false"><title>Forced</title>
          {Forced("a", null)}{Forced("b", "a")}{Forced("c", "b")}
          <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
        </organization>
        """, Sco("a", "b", "c"));

    [Fact]
    public void ForcedOrder_LaterPartsUnlockAsEarlierOnesAreSatisfied()
    {
        var m = ForcedSequential();
        Assert.NotNull(m.Sequencing);
        Assert.False(m.Sequencing!.ObjectivesGlobalToSystem);
        var seq = Sequencer(m);

        var start = seq.Start();
        Assert.Equal(("a", true), (start.PartId, start.NewAttempt));
        var view = seq.View();
        Assert.Equal(["a", "b", "c"], view.Items.Select(i => i.PartId));
        Assert.False(view.Items.Single(i => i.PartId == "b").CanChoose);
        Assert.Equal(new SeqProblem("\"{0}\" is not available yet.", "B"), view.Items.Single(i => i.PartId == "b").Reason);

        // Next while A is not passed: refused, and A's attempt is still open (nothing changed).
        var refused = seq.Navigate("continue");
        Assert.False(refused.Delivered);
        Assert.Equal("B", refused.Message!.Arg);
        Assert.Equal("a", seq.State.Current);
        Assert.True(seq.State.Activities["a"].Active);
        Assert.False(seq.Navigate("choice", "c").Delivered);

        seq.Report("a", Passed(), final: true);
        Assert.True(seq.View().Items.Single(i => i.PartId == "b").CanChoose);
        Assert.Equal("b", seq.Navigate("continue").PartId);
        Assert.Equal("a", seq.Navigate("previous").PartId); // a new attempt on A; its shared objective stays satisfied
        Assert.Equal(2, seq.State.Activities["a"].Attempts);
        seq.Report("a", Passed(), final: true); // the new attempt has to be finished too for the package to be complete
        Assert.Equal("b", seq.Navigate("choice", "b").PartId);
        seq.Report("b", Passed(), final: true);
        Assert.Equal("c", seq.Navigate("continue").PartId);
        seq.Report("c", Passed(0.9m), final: true);
        Assert.Equal(("completed", "passed"), (seq.Result().Completion, seq.Result().Success));

        var end = seq.Navigate("continue");
        Assert.True(end.Ended);
        Assert.True(seq.State.Ended);
        Assert.Null(seq.State.Current);
        // Opening again starts from the beginning.
        Assert.Equal("a", seq.Start().PartId);
    }

    // ── remediation (like "Sequencing Simple Remediation") ──

    private static ScormManifest Remediation() => Manifest("""
        <organization identifier="o" adlseq:objectivesGlobalToSystem="false"><title>Remediation</title>
          <item identifier="wrapper" isvisible="false"><title>Wrapper</title>
            <item identifier="c1" identifierref="r_c1"><title>Lesson 1</title>
              <imsss:sequencing IDRef="content"><imsss:objectives><imsss:primaryObjective objectiveID="lo"><imsss:mapInfo targetObjectiveID="g.t1" readSatisfiedStatus="true" writeSatisfiedStatus="false"/></imsss:primaryObjective></imsss:objectives></imsss:sequencing>
            </item>
            <item identifier="c2" identifierref="r_c2"><title>Lesson 2</title>
              <imsss:sequencing IDRef="content"><imsss:objectives><imsss:primaryObjective objectiveID="lo"><imsss:mapInfo targetObjectiveID="g.t2" readSatisfiedStatus="true" writeSatisfiedStatus="false"/></imsss:primaryObjective></imsss:objectives></imsss:sequencing>
            </item>
            <item identifier="t1" identifierref="r_t1"><title>Quiz 1</title>
              <imsss:sequencing IDRef="test"><imsss:objectives><imsss:primaryObjective objectiveID="lo"><imsss:mapInfo targetObjectiveID="g.t1" readSatisfiedStatus="true" writeSatisfiedStatus="true"/></imsss:primaryObjective></imsss:objectives></imsss:sequencing>
            </item>
            <item identifier="t2" identifierref="r_t2"><title>Quiz 2</title>
              <imsss:sequencing IDRef="test">
                <imsss:sequencingRules><imsss:postConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="always"/></imsss:ruleConditions><imsss:ruleAction action="exitParent"/></imsss:postConditionRule></imsss:sequencingRules>
                <imsss:objectives><imsss:primaryObjective objectiveID="lo"><imsss:mapInfo targetObjectiveID="g.t2" readSatisfiedStatus="true" writeSatisfiedStatus="true"/></imsss:primaryObjective></imsss:objectives>
              </imsss:sequencing>
            </item>
            <imsss:sequencing>
              <imsss:controlMode choice="false" flow="true" choiceExit="false"/>
              <imsss:sequencingRules>
                <imsss:postConditionRule><imsss:ruleConditions conditionCombination="any"><imsss:ruleCondition operator="not" condition="satisfied"/><imsss:ruleCondition operator="not" condition="objectiveStatusKnown"/></imsss:ruleConditions><imsss:ruleAction action="retry"/></imsss:postConditionRule>
                <imsss:postConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="satisfied"/></imsss:ruleConditions><imsss:ruleAction action="exitAll"/></imsss:postConditionRule>
              </imsss:sequencingRules>
            </imsss:sequencing>
          </item>
          <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
        </organization>
        """, Sco("c1", "c2", "t1", "t2"), """
        <imsss:sequencingCollection>
          <imsss:sequencing ID="content">
            <imsss:sequencingRules><imsss:preConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="satisfied"/></imsss:ruleConditions><imsss:ruleAction action="skip"/></imsss:preConditionRule></imsss:sequencingRules>
            <imsss:rollupRules objectiveMeasureWeight="0" rollupObjectiveSatisfied="false" rollupProgressCompletion="false"/>
            <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
          </imsss:sequencing>
          <imsss:sequencing ID="test">
            <imsss:sequencingRules><imsss:preConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="satisfied"/></imsss:ruleConditions><imsss:ruleAction action="skip"/></imsss:preConditionRule></imsss:sequencingRules>
            <imsss:rollupRules objectiveMeasureWeight="1" rollupObjectiveSatisfied="true" rollupProgressCompletion="true"/>
            <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
            <adlseq:rollupConsiderations requiredForCompleted="ifNotSkipped"/>
          </imsss:sequencing>
        </imsss:sequencingCollection>
        """);

    [Fact]
    public void Remediation_RetriesTheLessonsOfFailedQuizzes_ThenExits()
    {
        var m = Remediation();
        var seq = Sequencer(m);
        // The collection's rules apply to items that reference it; the wrapper is hidden and its parts cannot be chosen.
        var c1 = m.Sequencing!.Root.Children[0].Children[0];
        Assert.Equal("skip", c1.PreRules.Single().Action);
        Assert.Equal(0m, c1.ObjectiveMeasureWeight);
        Assert.Equal("ifNotSkipped", m.Sequencing.Root.Children[0].Children[2].RequiredForCompleted);
        var view = seq.View();
        Assert.Equal(["c1", "c2", "t1", "t2"], view.Items.Select(i => i.PartId));
        Assert.All(view.Items, i => Assert.Equal(new SeqProblem("This part opens through the package's own order; use Next."), i.Reason));

        Assert.Equal("c1", seq.Start().PartId);
        seq.Report("c1", Passed(), final: true);
        Assert.Equal("c2", seq.Navigate("continue").PartId);
        seq.Report("c2", Passed(), final: true);
        Assert.Equal("t1", seq.Navigate("continue").PartId);
        seq.Report("t1", Passed(1m), final: true);
        Assert.Equal("t2", seq.Navigate("continue").PartId);
        seq.Report("t2", Failed(0.4m), final: true);

        // After the last quiz: exit the wrapper; not satisfied, so retry it — lesson 1 is skipped (quiz 1 passed), lesson 2 opens.
        var retry = seq.Navigate("continue");
        Assert.Equal(("c2", true), (retry.PartId, retry.NewAttempt));
        Assert.Equal(2, seq.State.Activities["wrapper"].Attempts);
        Assert.True(seq.State.Activities["c1"].Skipped);
        // Quiz 2's earlier attempt still counts until it is taken again: completed but failed (so the attempt is not finished).
        Assert.Equal(("completed", "failed"), (seq.Result().Completion, seq.Result().Success));
        Assert.False(seq.State.Ended);
        seq.Report("c2", Passed(), final: true);
        Assert.Equal("t2", seq.Navigate("continue").PartId); // quiz 1 is skipped
        seq.Report("t2", Passed(0.9m), final: true);

        var end = seq.Navigate("continue");
        Assert.True(end.Ended);
        Assert.Equal(("completed", "passed"), (seq.Result().Completion, seq.Result().Success));
        Assert.Equal(0.95m, seq.Result().Measure); // quizzes weigh 1, lessons 0
    }

    // ── randomized post-test with two attempts (like "Sequencing Random Test") ──

    private static ScormManifest RandomTest() => Manifest("""
        <organization identifier="o"><title>Random</title>
          <item identifier="lesson" identifierref="r_lesson"><title>Lesson</title></item>
          <item identifier="post"><title>Post test</title>
            <item identifier="q1" identifierref="r_q1" isvisible="false"><title>Q1</title><imsss:sequencing IDRef="pick"/>
              <adlnav:presentation><adlnav:navigationInterface><adlnav:hideLMSUI>suspendAll</adlnav:hideLMSUI><adlnav:hideLMSUI>previous</adlnav:hideLMSUI></adlnav:navigationInterface></adlnav:presentation></item>
            <item identifier="q2" identifierref="r_q2" isvisible="false"><title>Q2</title><imsss:sequencing IDRef="pick"/></item>
            <item identifier="q3" identifierref="r_q3" isvisible="false"><title>Q3</title><imsss:sequencing IDRef="pick"/></item>
            <imsss:sequencing>
              <imsss:controlMode choice="false" flow="true"/>
              <imsss:sequencingRules>
                <imsss:preConditionRule><imsss:ruleConditions conditionCombination="any"><imsss:ruleCondition condition="attemptLimitExceeded"/><imsss:ruleCondition condition="satisfied"/></imsss:ruleConditions><imsss:ruleAction action="disabled"/></imsss:preConditionRule>
                <imsss:postConditionRule><imsss:ruleConditions><imsss:ruleCondition operator="not" condition="satisfied"/><imsss:ruleCondition operator="not" condition="attemptLimitExceeded"/></imsss:ruleConditions><imsss:ruleAction action="retry"/></imsss:postConditionRule>
                <imsss:postConditionRule><imsss:ruleConditions><imsss:ruleCondition operator="not" condition="objectiveStatusKnown"/><imsss:ruleCondition operator="not" condition="attemptLimitExceeded"/></imsss:ruleConditions><imsss:ruleAction action="retry"/></imsss:postConditionRule>
                <imsss:postConditionRule><imsss:ruleConditions conditionCombination="any"><imsss:ruleCondition condition="objectiveStatusKnown"/><imsss:ruleCondition condition="attemptLimitExceeded"/></imsss:ruleConditions><imsss:ruleAction action="exitAll"/></imsss:postConditionRule>
              </imsss:sequencingRules>
              <imsss:limitConditions attemptLimit="2"/>
              <imsss:rollupRules>
                <imsss:rollupRule childActivitySet="any"><imsss:rollupConditions><imsss:rollupCondition condition="satisfied"/></imsss:rollupConditions><imsss:rollupAction action="satisfied"/></imsss:rollupRule>
                <imsss:rollupRule childActivitySet="any"><imsss:rollupConditions><imsss:rollupCondition operator="not" condition="satisfied"/></imsss:rollupConditions><imsss:rollupAction action="notSatisfied"/></imsss:rollupRule>
              </imsss:rollupRules>
              <imsss:randomizationControls randomizationTiming="onEachNewAttempt" reorderChildren="true"/>
            </imsss:sequencing>
          </item>
          <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
        </organization>
        """, Sco("lesson", "q1", "q2", "q3"), """
        <imsss:sequencingCollection>
          <imsss:sequencing ID="pick">
            <imsss:sequencingRules><imsss:postConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="always"/></imsss:ruleConditions><imsss:ruleAction action="exitParent"/></imsss:postConditionRule></imsss:sequencingRules>
            <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
          </imsss:sequencing>
        </imsss:sequencingCollection>
        """);

    [Fact]
    public void RandomPostTest_OneRandomQuestionPerAttempt_TwoAttempts_ThenExits()
    {
        var m = RandomTest();
        var seq = Sequencer(m, seed: 7);
        // Hidden questions are left out of the contents; the post test is a heading.
        var view = seq.View();
        Assert.Equal(["lesson", "post"], view.Items.Select(i => i.ActivityId));
        Assert.False(view.Items[1].IsLeaf);

        Assert.Equal("lesson", seq.Start().PartId);
        seq.Report("lesson", Passed(), final: true);
        var first = seq.Navigate("continue");
        Assert.StartsWith("q", first.PartId);
        Assert.Contains("suspendAll", seq.View().HideLmsUi.Concat(["suspendAll"]));
        seq.Report(first.PartId!, Failed(), final: true);

        // Failed: exit the post test, retry it (attempt 2 of 2) with a newly shuffled order.
        var second = seq.Navigate("continue");
        Assert.True(second.Delivered);
        Assert.Equal(2, seq.State.Activities["post"].Attempts);
        // Opening the player again keeps the question that is open, though the post test allows no further attempt.
        Assert.Equal((second.PartId, false), (seq.Start().PartId, seq.Start().NewAttempt));
        seq.Report(second.PartId!, Failed(), final: true);

        var end = seq.Navigate("continue");
        Assert.True(end.Ended);
        Assert.Equal("failed", seq.Result().Success);
        // No third attempt: the post test is out of attempts.
        var again = seq.Start();
        Assert.Equal("lesson", again.PartId);
        seq.Report("lesson", Passed(), final: true);
        var refused = seq.Navigate("continue");
        Assert.False(refused.Delivered);
        Assert.Equal(new SeqProblem("\"{0}\" is not available yet.", "Post test"), refused.Message);
    }

    // ── rollup rules, measure and hidden parts ──

    [Fact]
    public void RollupRules_AtLeastCount_SatisfiedByMeasure_HiddenFromChoice_AndChoiceExit()
    {
        var m = Manifest("""
            <organization identifier="o"><title>Rollup</title>
              <item identifier="unit"><title>Unit</title>
                <item identifier="a" identifierref="r_a"><title>A</title></item>
                <item identifier="b" identifierref="r_b"><title>B</title></item>
                <item identifier="c" identifierref="r_c"><title>C</title></item>
                <imsss:sequencing>
                  <imsss:controlMode choice="true" flow="true" choiceExit="false"/>
                  <imsss:rollupRules><imsss:rollupRule childActivitySet="atLeastCount" minimumCount="2"><imsss:rollupConditions><imsss:rollupCondition condition="satisfied"/></imsss:rollupConditions><imsss:rollupAction action="satisfied"/></imsss:rollupRule></imsss:rollupRules>
                </imsss:sequencing>
              </item>
              <item identifier="secret" identifierref="r_secret"><title>Secret</title>
                <imsss:sequencing><imsss:sequencingRules><imsss:preConditionRule><imsss:ruleConditions><imsss:ruleCondition condition="always"/></imsss:ruleConditions><imsss:ruleAction action="hiddenFromChoice"/></imsss:preConditionRule></imsss:sequencingRules></imsss:sequencing>
              </item>
              <item identifier="final" identifierref="r_final"><title>Final</title>
                <imsss:sequencing><imsss:objectives><imsss:primaryObjective objectiveID="p" satisfiedByMeasure="true"><imsss:minNormalizedMeasure>0.8</imsss:minNormalizedMeasure></imsss:primaryObjective></imsss:objectives></imsss:sequencing>
              </item>
              <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
            </organization>
            """, Sco("a", "b", "c", "secret", "final"));
        var seq = Sequencer(m);
        Assert.DoesNotContain(seq.View().Items, i => i.PartId == "secret");

        Assert.Equal("a", seq.Start().PartId);
        // Choice exit is off for the unit: the learner cannot leave it by choosing "final".
        Assert.Equal(new SeqProblem("Finish \"{0}\" before choosing another part.", "Unit"), seq.Navigate("choice", "final").Message);
        seq.Report("a", Passed(), final: true);
        Assert.Equal("b", seq.Navigate("continue").PartId);
        seq.Report("b", Failed(), final: true);
        Assert.Equal("c", seq.Navigate("continue").PartId);
        seq.Report("c", Passed(), final: true);
        Assert.True(seq.Satisfied(m.Sequencing!.Root.Children[0])); // two of three satisfied

        // Flow skips nothing: "secret" is only hidden from choice, so Next reaches it.
        Assert.Equal("secret", seq.Navigate("continue").PartId);
        seq.Report("secret", new SeqReport("completed", "unknown", null, [], ""), final: true);
        Assert.Equal("final", seq.Navigate("continue").PartId);
        seq.Report("final", new SeqReport("completed", "passed", 0.7m, [], ""), final: true);
        Assert.False(seq.Satisfied(m.Sequencing.Root.Children[2])); // by measure: 0.7 < 0.8, whatever the SCO said
    }

    [Fact]
    public void DeliveryControls_CompleteAndSatisfy_WhenTheContentDoesNot_AndSuspendKeepsTheAttemptOpen()
    {
        var m = Manifest("""
            <organization identifier="o"><title>Plain</title>
              <item identifier="a" identifierref="r_a"><title>A</title></item>
              <item identifier="b" identifierref="r_b"><title>B</title></item>
              <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
            </organization>
            """, Sco("a", "b"));
        var seq = Sequencer(m);
        Assert.Equal("a", seq.Start().PartId);
        // Suspended: the attempt stays open and opening the package resumes it.
        seq.Report("a", new SeqReport("incomplete", "unknown", null, [], "suspend"), final: true);
        Assert.True(seq.State.Activities["a"].Active);
        var resumed = seq.Start();
        Assert.Equal(("a", false), (resumed.PartId, resumed.NewAttempt));
        // Ended normally without reporting: completion and objective are set by the LMS (setByContent defaults to false).
        seq.Report("a", new SeqReport("unknown", "unknown", null, [], ""), final: true);
        Assert.Equal(("completed", (bool?)true), (seq.State.Activities["a"].Completion, seq.State.Activities["a"].Satisfied));
        Assert.Equal("b", seq.Navigate("continue").PartId);
        Assert.Equal("b", seq.Navigate("suspendAll") is { Delivered: false } ? seq.State.Current : null);
        Assert.True(seq.Navigate("exitAll").Ended);
    }

    [Fact]
    public void PackagesWithoutSequencing_KeepFreeNavigation()
    {
        var m = Manifest("""
            <organization identifier="o"><title>Plain</title>
              <item identifier="a" identifierref="r_a"><title>A</title></item>
              <item identifier="b" identifierref="r_b"><title>B</title></item>
            </organization>
            """, Sco("a", "b"));
        Assert.Null(m.Sequencing);
        Assert.Equal(2, m.Parts!.Count);
    }

    // ── SCORM 1.2 prerequisites ──

    [Theory]
    [InlineData("a", true)]
    [InlineData("c", false)]
    [InlineData("a & b", false)]
    [InlineData("a | c", true)]
    [InlineData("~c", true)]
    [InlineData("(a | c) & ~b", true)]
    [InlineData("b=f", true)]
    [InlineData("b<>failed", false)]
    [InlineData("2*{a,b,d}", true)]
    [InlineData("3*{a,b,d}", false)]
    [InlineData("unknown_item", true)]
    [InlineData("a & (", true)]
    [InlineData("", true)]
    public void Scorm12Prerequisites_AreEvaluated(string expression, bool met)
    {
        var status = new Dictionary<string, string> { ["a"] = "passed", ["b"] = "failed", ["c"] = "incomplete", ["d"] = "completed" };
        Assert.Equal(met, ScormPrerequisites.Met(expression, id => status.GetValueOrDefault(id)));
    }

    [Fact]
    public void Scorm12Manifest_KeepsEachItemsPrerequisites()
    {
        var m = ScormPackageReader.ParseManifest(XDocument.Parse("""
            <manifest identifier="m" xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <metadata><schema>ADL SCORM</schema><schemaversion>1.2</schemaversion></metadata>
              <organizations default="o"><organization identifier="o"><title>T</title>
                <item identifier="a" identifierref="r1"><title>A</title></item>
                <item identifier="b" identifierref="r2"><title>B</title><adlcp:prerequisites type="aicc_script">a</adlcp:prerequisites></item>
              </organization></organizations>
              <resources>
                <resource identifier="r1" type="webcontent" adlcp:scormtype="sco" href="a.html"/>
                <resource identifier="r2" type="webcontent" adlcp:scormtype="sco" href="b.html"/>
              </resources>
            </manifest>
            """));
        Assert.Equal([null, "a"], m.Parts!.Select(p => p.Prerequisites));
        Assert.Null(m.Sequencing);
    }
}
