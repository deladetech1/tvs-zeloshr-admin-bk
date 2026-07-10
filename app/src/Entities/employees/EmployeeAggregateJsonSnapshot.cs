using System.Text.Json;
using System.Text.Json.Nodes;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Projects employee aggregate read data into update-payload JSON paths for change-request diffs.</summary>
public static class EmployeeAggregateJsonSnapshot
{
    public static JsonObject ToUpdateShape(EmployeeAggregateReadDto dto)
    {
        var json = JsonSerializer.SerializeToNode(dto, PlatformJson.SerializerOptions);
        if (json is not JsonObject root)
            return new JsonObject();

        var update = new JsonObject();

        if (root["identity"] is JsonObject identity)
            update["identity"] = ProjectIdentity(identity);

        if (root["compensation"] is JsonObject compensation)
            update["compensation"] = ProjectCompensation(compensation);

        if (root["education"] is JsonArray education)
            update["education"] = education.DeepClone();

        if (root["certifications"] is JsonArray certifications)
            update["certifications"] = certifications.DeepClone();

        if (root["medical"] is JsonObject medical)
            update["medical"] = medical.DeepClone();

        if (root["skills"] is JsonArray skills)
            update["skills"] = skills.DeepClone();

        if (root["experiences"] is JsonArray experiences)
            update["experiences"] = experiences.DeepClone();

        if (root["referrals"] is JsonArray referrals)
            update["referrals"] = referrals.DeepClone();

        return update;
    }

    public static JsonNode? ResolvePath(JsonObject updateShape, string fieldPath)
    {
        if (string.IsNullOrWhiteSpace(fieldPath))
            return null;

        var segments = fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        JsonNode? current = updateShape;
        foreach (var segment in segments)
        {
            if (current is JsonObject obj && obj.TryGetPropertyValue(segment, out var next))
                current = next;
            else
                return null;
        }

        return current?.DeepClone();
    }

    private static JsonObject ProjectIdentity(JsonObject identity)
    {
        var projected = new JsonObject();
        CopyIfPresent(identity, projected, "full_name", "date_of_birth", "gender", "country", "marital_status",
            "personal_email", "phone", "linked_in_url", "residential_address", "next_of_kin_name",
            "next_of_kin_phone", "relationship_to_next_of_kin", "custom_fields", "emergency");

        if (identity.TryGetPropertyValue("identifications", out var identifications))
            projected["identifications"] = identifications?.DeepClone();

        if (identity.TryGetPropertyValue("profile_url", out var profileUrl))
        {
            if (profileUrl is JsonObject profileObj
                && profileObj.TryGetPropertyValue("doc_id", out var docId))
                projected["profile_url"] = docId?.DeepClone();
            else
                projected["profile_url"] = profileUrl?.DeepClone();
        }

        return projected;
    }

    private static JsonObject ProjectCompensation(JsonObject compensation)
    {
        var projected = new JsonObject();
        CopyIfPresent(compensation, projected, "ssnit_insurance_number", "ssnit_number", "payment", "custom_fields");
        return projected;
    }

    private static void CopyIfPresent(JsonObject source, JsonObject target, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.TryGetPropertyValue(key, out var value) && value is not null)
                target[key] = value.DeepClone();
        }
    }
}
