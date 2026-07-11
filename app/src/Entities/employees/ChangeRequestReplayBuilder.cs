using System.Text.Json;
using System.Text.Json.Nodes;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Rebuilds <see cref="UpdateEmployeeAggregateRequest"/> from an approved change-request row.</summary>
public static class ChangeRequestReplayBuilder
{
    public static UpdateEmployeeAggregateRequest Build(string fieldPath, string newValueJson)
    {
        var node = JsonNode.Parse(newValueJson);
        var request = new UpdateEmployeeAggregateRequest();

        if (node is JsonObject wrapper && wrapper.ContainsKey("value"))
            return BuildFromSectionWrapper(fieldPath, wrapper);

        return BuildFromScalarOrArray(fieldPath, node);
    }

    private static UpdateEmployeeAggregateRequest BuildFromSectionWrapper(string fieldPath, JsonObject wrapper)
    {
        var sync = wrapper.TryGetPropertyValue("sync", out var syncNode)
            && syncNode is JsonValue syncValue
            && syncValue.TryGetValue<bool>(out var syncBool)
            && syncBool;

        var deleteIds = ParseGuidList(wrapper["delete_ids"]);
        wrapper.TryGetPropertyValue("value", out var valueNode);

        return fieldPath.ToLowerInvariant() switch
        {
            "education" => new UpdateEmployeeAggregateRequest
            {
                Education = DeserializeList<EmployeeEducationUpsertDto>(valueNode),
                SyncEducation = sync,
                DeleteEducationIds = deleteIds,
            },
            "certifications" => new UpdateEmployeeAggregateRequest
            {
                Certifications = DeserializeList<EmployeeCertificationUpsertDto>(valueNode),
                SyncCertifications = sync,
                DeleteCertificationIds = deleteIds,
            },
            "identity.identifications" => new UpdateEmployeeAggregateRequest
            {
                Identity = new EmployeeAggregateIdentityDto
                {
                    Identifications = DeserializeList<EmployeeIdentificationUpsertDto>(valueNode),
                },
                SyncIdentifications = sync,
                DeleteIdentificationIds = deleteIds,
            },
            "compensation.payment" => new UpdateEmployeeAggregateRequest
            {
                Compensation = new EmployeeAggregateCompensationDto(),
            },
            _ => BuildFromScalarOrArray(fieldPath, wrapper),
        };
    }

    private static UpdateEmployeeAggregateRequest BuildFromScalarOrArray(string fieldPath, JsonNode? node)
    {
        if (node is null)
            return new UpdateEmployeeAggregateRequest();

        return fieldPath.ToLowerInvariant() switch
        {
            "education" => new UpdateEmployeeAggregateRequest
            {
                Education = DeserializeList<EmployeeEducationUpsertDto>(node),
            },
            "certifications" => new UpdateEmployeeAggregateRequest
            {
                Certifications = DeserializeList<EmployeeCertificationUpsertDto>(node),
            },
            "identity.identifications" => new UpdateEmployeeAggregateRequest
            {
                Identity = new EmployeeAggregateIdentityDto
                {
                    Identifications = DeserializeList<EmployeeIdentificationUpsertDto>(node),
                },
            },
            "skills" or "experiences" or "referrals" => new UpdateEmployeeAggregateRequest(),
            _ when fieldPath.StartsWith("identity.", StringComparison.OrdinalIgnoreCase) =>
                new UpdateEmployeeAggregateRequest
                {
                    Identity = BuildIdentityPatch(fieldPath, node),
                },
            _ when fieldPath.StartsWith("compensation.", StringComparison.OrdinalIgnoreCase) =>
                new UpdateEmployeeAggregateRequest
                {
                    Compensation = BuildCompensationPatch(fieldPath, node),
                },
            _ when fieldPath.StartsWith("medical.", StringComparison.OrdinalIgnoreCase) =>
                new UpdateEmployeeAggregateRequest(),
            _ => new UpdateEmployeeAggregateRequest(),
        };
    }

    private static EmployeeAggregateIdentityDto BuildIdentityPatch(string fieldPath, JsonNode node)
    {
        var prop = fieldPath["identity.".Length..];
        var json = new JsonObject { [prop] = node.DeepClone() };
        return JsonSerializer.Deserialize<EmployeeAggregateIdentityDto>(json, PlatformJson.SerializerOptions)
            ?? new EmployeeAggregateIdentityDto();
    }

    private static EmployeeAggregateCompensationDto BuildCompensationPatch(string fieldPath, JsonNode node)
    {
        var prop = fieldPath["compensation.".Length..];
        var wireProp = prop.Equals("ssnit_insurance_number", StringComparison.OrdinalIgnoreCase)
            ? "ssnit_number"
            : prop;
        var json = new JsonObject { [wireProp] = node.DeepClone() };
        return JsonSerializer.Deserialize<EmployeeAggregateCompensationDto>(json, PlatformJson.SerializerOptions)
            ?? new EmployeeAggregateCompensationDto();
    }

    private static IReadOnlyList<T>? DeserializeList<T>(JsonNode? node)
    {
        if (node is null)
            return null;

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(node, PlatformJson.SerializerOptions);
    }

    private static IReadOnlyList<Guid>? ParseGuidList(JsonNode? node)
    {
        if (node is not JsonArray arr || arr.Count == 0)
            return null;

        var ids = new List<Guid>();
        foreach (var item in arr)
        {
            if (item is null)
                continue;
            if (Guid.TryParse(item.ToString(), out var id))
                ids.Add(id);
        }

        return ids.Count == 0 ? null : ids;
    }
}
