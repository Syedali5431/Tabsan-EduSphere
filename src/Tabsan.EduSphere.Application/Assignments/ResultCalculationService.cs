using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

public class ResultCalculationService : IResultCalculationService
{
    private readonly IResultRepository _repo;

    public ResultCalculationService(IResultRepository repo)
    {
        _repo = repo;
    }

    public async Task<ResultCalculationSettingsResponse> GetSettingsAsync(InstitutionType institutionType, CancellationToken ct = default)
    {
        var gpaRules = await _repo.GetGpaScaleRulesAsync(institutionType, ct);
        var componentRules = await _repo.GetAllComponentRulesAsync(institutionType, ct);

        return new ResultCalculationSettingsResponse(
            institutionType,
            gpaRules.OrderBy(r => r.DisplayOrder)
                .Select(r => new GpaScaleRuleDto(r.Id, r.GradePoint, r.MinimumScore, r.DisplayOrder))
                .ToList(),
            componentRules.OrderBy(r => r.DisplayOrder)
                .Select(r => new ResultComponentRuleDto(r.Id, r.Name, r.Weightage, r.DisplayOrder, r.IsActive))
                .ToList());
    }

    public async Task SaveSettingsAsync(SaveResultCalculationSettingsRequest request, CancellationToken ct = default)
    {
        if (request.GpaScaleRules.Count == 0)
            throw new ArgumentException("At least one GPA mapping row is required.");
        if (request.ComponentRules.Count == 0)
            throw new ArgumentException("At least one assessment component row is required.");

        var normalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var components = new List<ResultComponentRule>();
        var totalWeight = 0m;
        var displayOrder = 1;

        foreach (var component in request.ComponentRules.OrderBy(x => x.DisplayOrder))
        {
            if (string.IsNullOrWhiteSpace(component.Name))
                throw new ArgumentException("Component name is required.");
            if (!normalizedNames.Add(component.Name.Trim()))
                throw new ArgumentException($"Duplicate component name '{component.Name}' is not allowed.");

            var item = new ResultComponentRule(component.Name.Trim(), component.Weightage, displayOrder++, component.IsActive, request.InstitutionType);
            components.Add(item);
            if (component.IsActive)
                totalWeight += component.Weightage;
        }

        if (totalWeight != 100m)
            throw new ArgumentException("Active component weightages must total exactly 100.");

        var gpaRules = request.GpaScaleRules
            .OrderBy(x => x.MinimumScore)
            .ThenBy(x => x.DisplayOrder)
            .Select((rule, index) => new GpaScaleRule(rule.GradePoint, rule.MinimumScore, index + 1, request.InstitutionType))
            .ToList();

        for (var i = 1; i < gpaRules.Count; i++)
        {
            if (gpaRules[i].MinimumScore == gpaRules[i - 1].MinimumScore)
                throw new ArgumentException("Duplicate GPA score thresholds are not allowed.");
        }

        await _repo.ReplaceCalculationRulesAsync(request.InstitutionType, gpaRules, components, ct);
        await _repo.SaveChangesAsync(ct);
    }
}