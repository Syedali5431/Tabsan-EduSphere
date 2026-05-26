using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Application.Interfaces;

public interface IResultCalculationService
{
    Task<ResultCalculationSettingsResponse> GetSettingsAsync(InstitutionType institutionType, CancellationToken ct = default);
    Task SaveSettingsAsync(SaveResultCalculationSettingsRequest request, CancellationToken ct = default);
}