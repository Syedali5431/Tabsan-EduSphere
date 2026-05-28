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
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSubjectOfferingId = null,
            SelectedSemesterName = "Semester 1",
            SelectedExamType = "Midterm",
            SelectedAssessmentComponent = "Theory"
        };

        model.CanWriteResults.Should().BeFalse();
        model.SaveResultDisabledReason.Should().ContainEquivalentOf("Select department, course, subject, semester/class, exam type, and assessment component");
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
}
