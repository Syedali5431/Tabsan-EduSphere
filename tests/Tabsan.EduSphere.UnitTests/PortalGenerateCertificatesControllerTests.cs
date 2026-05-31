using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Tabsan.EduSphere.Web.Controllers;
using Tabsan.EduSphere.Web.Services;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

public class PortalGenerateCertificatesControllerTests
{
    [Fact]
    public async Task DownloadCertificateTemplate_UniversityScope_WithNonUniversityType_RedirectsWithMessage()
    {
        var sut = CreateSut(isConnected: true);

        var result = await sut.DownloadCertificateTemplate(
            templateType: "completion",
            tenantId: Guid.NewGuid(),
            campusId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            institutionType: 0,
            semesterId: Guid.NewGuid(),
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PortalController.GenerateCertificates));
        sut.TempData["PortalMessage"].Should().Be("Selected template type is not available for university scope.");
    }

    [Fact]
    public async Task GenerateAdditionalCertificate_UniversityScope_RedirectsWithMessage()
    {
        var sut = CreateSut(isConnected: true);

        var result = await sut.GenerateAdditionalCertificate(
            studentProfileId: Guid.NewGuid(),
            documentType: "completion",
            tenantId: Guid.NewGuid(),
            campusId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            semesterId: Guid.NewGuid(),
            institutionType: 0,
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PortalController.GenerateCertificates));
        sut.TempData["PortalMessage"].Should().Be("Completion Certificate and Report Card generation is available only for school/college scope.");
    }

    [Fact]
    public async Task GenerateAdditionalCertificate_NonUniversityScope_WithInvalidType_RedirectsWithMessage()
    {
        var sut = CreateSut(isConnected: true);

        var result = await sut.GenerateAdditionalCertificate(
            studentProfileId: Guid.NewGuid(),
            documentType: "degree",
            tenantId: Guid.NewGuid(),
            campusId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            semesterId: Guid.NewGuid(),
            institutionType: 1,
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PortalController.GenerateCertificates));
        sut.TempData["PortalMessage"].Should().Be("Please select Completion Certificate or Report Card before generating.");
    }

    private static PortalController CreateSut(bool isConnected)
    {
        var api = DispatchProxy.Create<IEduApiClient, CertificateTestEduApiClientProxy>();
        var proxy = (CertificateTestEduApiClientProxy)(object)api;
        proxy.IsConnectedValue = isConnected;

        var sut = new PortalController(api, new CertificateTestWebHostEnvironment(), NullLogger<PortalController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        sut.TempData = new TempDataDictionary(sut.ControllerContext.HttpContext, new DictionaryTempDataProvider());
        return sut;
    }

    private class CertificateTestEduApiClientProxy : DispatchProxy
    {
        public bool IsConnectedValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new NotSupportedException();

            return targetMethod.Name switch
            {
                nameof(IEduApiClient.IsConnected) => IsConnectedValue,
                _ => throw new NotSupportedException($"Method '{targetMethod.Name}' is not configured for this test proxy.")
            };
        }
    }

    private sealed class DictionaryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _store = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context)
            => _store;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            => _store = values;
    }

    private sealed class CertificateTestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tabsan.EduSphere.Web.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
