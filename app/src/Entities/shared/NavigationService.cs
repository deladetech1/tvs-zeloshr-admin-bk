using Microsoft.AspNetCore.Mvc.ApiExplorer;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Shared;

public sealed class NavigationService(IApiDescriptionGroupCollectionProvider apiDescriptions)
{
    public NavigationMapResponse Build()
    {
        var endpointsByGroup = apiDescriptions.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => !string.IsNullOrWhiteSpace(d.RelativePath))
            .Where(d => SwaggerGroups.IsVisibleInSwagger(d.GroupName))
            .GroupBy(d => d.GroupName ?? "default", StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var modules = endpointsByGroup
            .Select(g => new NavigationModuleDto
            {
                Id = ToModuleId(g.Key),
                Label = g.Key,
                Endpoints = g
                    .Select(FormatEndpoint)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(e => e, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            })
            .ToList();

        return new NavigationMapResponse { Modules = modules };
    }

    private static string FormatEndpoint(ApiDescription description)
    {
        var method = description.HttpMethod?.ToUpperInvariant() ?? "GET";
        var path = "/" + description.RelativePath!.TrimStart('/');
        return $"{method} {path}";
    }

    private static string ToModuleId(string groupName) =>
        groupName.Trim().Replace(" ", "-").ToLowerInvariant();
}
