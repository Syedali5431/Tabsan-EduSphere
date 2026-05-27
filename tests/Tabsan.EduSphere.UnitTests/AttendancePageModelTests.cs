using FluentAssertions;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.UnitTests;

public class AttendancePageModelTests
{
    [Fact]
    public void CanSaveAttendance_WhenRequiredFiltersMissing_ReturnsFalse()
    {
        var model = new AttendancePageModel
        {
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = null,
            SelectedCourseId = Guid.NewGuid(),
            SelectedSemesterName = "Semester 1"
        };

        model.CanSaveAttendance.Should().BeFalse();
        model.SaveAttendanceDisabledReason.Should().ContainEquivalentOf("Select department, course, and semester/class");
    }

    [Fact]
    public void CanSaveAttendance_WhenRequiredFiltersSelected_ReturnsTrue()
    {
        var model = new AttendancePageModel
        {
            SelectedOfferingId = Guid.NewGuid(),
            SelectedDepartmentId = Guid.NewGuid(),
            SelectedCourseId = Guid.NewGuid(),
            SelectedSemesterName = "Semester 1"
        };

        model.CanSaveAttendance.Should().BeTrue();
    }
}
