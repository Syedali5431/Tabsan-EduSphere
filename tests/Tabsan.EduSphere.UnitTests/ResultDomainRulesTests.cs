using FluentAssertions;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.UnitTests;

public class ResultDomainRulesTests
{
    [Fact]
    public void CorrectMarks_WhenResultIsDraft_ThrowsInvalidOperationException()
    {
        var result = new Result(Guid.NewGuid(), Guid.NewGuid(), "Final", 82m, 100m);

        var act = () => result.CorrectMarks(85m, 100m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*published results*");
    }

    [Fact]
    public void CorrectMarks_WhenResultIsPublished_UpdatesMarks()
    {
        var result = new Result(Guid.NewGuid(), Guid.NewGuid(), "Final", 82m, 100m);
        result.Publish(Guid.NewGuid());

        result.CorrectMarks(88m, 100m);

        result.MarksObtained.Should().Be(88m);
        result.MaxMarks.Should().Be(100m);
        result.IsPublished.Should().BeTrue();
    }
}
