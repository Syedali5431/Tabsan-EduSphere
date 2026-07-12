using FluentAssertions;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.UnitTests;

public class ResultsPageModelTests
{
    [Fact]
    public void CanWriteResults_WhenRequiredFiltersMissing_ReturnsFalse()
    {
        var model = new ResultsPageModel
        {
            SelectedOfferingId = null,
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSubjectOfferingId = null,
            SelectedSemesterName = "Semester 1",
            SelectedExamType = "Midterm",
            SelectedAssessmentComponent = "Theory"
        };

        model.CanWriteResults.Should().BeFalse();
        model.SaveResultDisabledReason.Should().ContainEquivalentOf("Select department, course, subject, exam type, and assessment component");
    }

    [Fact]
    public void CanWriteResults_WhenRequiredFiltersSelected_ReturnsTrue()
    {
        var model = new ResultsPageModel
        {
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSubjectOfferingId = Guid.NewGuid(),
            SelectedSemesterName = "Semester 1",
            SelectedExamType = "Final",
            SelectedAssessmentComponent = "Theory"
        };

        model.CanWriteResults.Should().BeTrue();
    }

    [Fact]
    public void CanPublishResults_WhenFacultyScopeComplete_ReturnsFalse()
    {
        var model = new ResultsPageModel
        {
            Identity = new SessionIdentity { Roles = new List<string> { "Faculty" } },
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSubjectOfferingId = Guid.NewGuid(),
            SelectedSemesterName = "Semester 1",
            SelectedExamType = "Final",
            SelectedAssessmentComponent = "Theory"
        };

        model.CanPublishResults.Should().BeFalse();
        model.PublishResultDisabledReason.Should().ContainEquivalentOf("Admin/SuperAdmin");
    }

    [Fact]
    public void CanPublishResults_WhenAdminScopeComplete_ReturnsTrue()
    {
        var model = new ResultsPageModel
        {
            Identity = new SessionIdentity { Roles = new List<string> { "Admin" } },
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSubjectOfferingId = Guid.NewGuid(),
            SelectedSemesterName = "Semester 1",
            SelectedExamType = "Final",
            SelectedAssessmentComponent = "Theory"
        };

        model.CanPublishResults.Should().BeTrue();
    }
}
