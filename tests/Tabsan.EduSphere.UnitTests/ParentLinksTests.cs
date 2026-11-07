using FluentAssertions;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Web.Models.Portal;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Parent Links screen (2026-10-30): relationship choices and the administrator's scope.</summary>
public class ParentLinksTests
{
    [Fact]
    public void Relationships_OfferTheStandardChoices_PlusAWordSavedThroughTheApi()
    {
        ParentLinkRelationships.Standard.Should().StartWith(["Father", "Mother", "Guardian"]);
        ParentLinkRelationships.ChoicesFor(null).Should().BeSameAs(ParentLinkRelationships.Standard);
        ParentLinkRelationships.ChoicesFor("mother").Should().BeSameAs(ParentLinkRelationships.Standard, "the comparison ignores case");
        ParentLinkRelationships.ChoicesFor(" Uncle ").Should().EndWith("Uncle").And.HaveCount(ParentLinkRelationships.Standard.Count + 1);
    }

    [Theory]
    [InlineData("Mother", "mother", true)]
    [InlineData("Mother", " Mother ", true)]
    [InlineData("Father", "Mother", false)]
    [InlineData("Father", null, false)]
    public void Relationships_SelectTheSavedChoice(string choice, string? saved, bool selected)
        => ParentLinkRelationships.IsSelected(choice, saved).Should().Be(selected);

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(" Guardian ", "Guardian")]
    public void Relationships_AreTrimmed_AndEmptyMeansNone(string? input, string? expected)
        => ParentLinkRelationships.Normalize(input).Should().Be(expected);

    [Fact]
    public void Scope_SuperAdminSeesEveryDepartment_AdminOnlyTheirs()
    {
        var mine = Guid.NewGuid();
        ParentLinkScope.Unrestricted.AllowsDepartment(Guid.NewGuid()).Should().BeTrue();

        var admin = new ParentLinkScope([mine], TenantId: null);
        admin.AllowsDepartment(mine).Should().BeTrue();
        admin.AllowsDepartment(Guid.NewGuid()).Should().BeFalse();
        new ParentLinkScope([], null).AllowsDepartment(mine).Should().BeFalse("an Admin without departments manages no students");
    }

    [Fact]
    public void Page_KnowsWhichStudentsAreAlreadyLinked()
    {
        var linked = Guid.NewGuid();
        var model = new ParentLinksPageModel
        {
            Links = [new ParentStudentLinkDetailDto(Guid.NewGuid(), linked, "TMS-01", "Fatima", "Class 9", null, 9, "Mother", true, DateTime.UtcNow)]
        };

        model.IsLinked(linked).Should().BeTrue();
        model.IsLinked(Guid.NewGuid()).Should().BeFalse();
    }
}
