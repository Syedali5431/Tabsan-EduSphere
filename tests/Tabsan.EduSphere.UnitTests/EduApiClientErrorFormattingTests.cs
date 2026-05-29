using System.Net;
using System.Reflection;
using FluentAssertions;
using Tabsan.EduSphere.Web.Services;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

public class EduApiClientErrorFormattingTests
{
    [Fact]
    public void BuildException_JsonMessageBody_UsesMessageValue()
    {
        var exception = BuildException(HttpStatusCode.NotFound, "{\"message\":\"Timetable 2525 not found.\"}");

        exception.Message.Should().Be("Timetable 2525 not found.");
    }

    [Fact]
    public void BuildException_JsonTitleBody_FallsBackToTitleValue()
    {
        var exception = BuildException(HttpStatusCode.BadRequest, "{\"title\":\"Validation failed.\"}");

        exception.Message.Should().Be("Validation failed.");
    }

    [Fact]
    public void BuildException_PlainTextBody_UsesOriginalBody()
    {
        var exception = BuildException(HttpStatusCode.InternalServerError, "Raw API failure body");

        exception.Message.Should().Be("Raw API failure body");
    }

    private static InvalidOperationException BuildException(HttpStatusCode statusCode, string body)
    {
        var method = typeof(EduApiClient).GetMethod("BuildException", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildException method was not found on EduApiClient.");

        return method.Invoke(null, new object[] { statusCode, body }) as InvalidOperationException
            ?? throw new InvalidOperationException("BuildException did not return InvalidOperationException.");
    }
}
