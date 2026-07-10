using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.Employees.Authorization;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeePayloadSplitResult
{
    public JsonObject ApplyNow { get; init; } = new();
    public IReadOnlyList<EmployeePayloadPendingChange> PendingApproval { get; init; } = [];
    public IReadOnlyList<string> Rejected { get; init; } = [];
}

public sealed class EmployeePayloadPendingChange
{
    public required string FieldPath { get; init; }
    public JsonNode? OldValue { get; init; }
    public required JsonNode NewValue { get; init; }
}

/// <summary>
/// Splits an employee self-service update payload into apply-now, pending approval, and rejected paths.
/// </summary>
public static class EmployeePayloadFilter
{
    private static readonly HashSet<string> AdminOnlyTopLevelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "employment",
        "document_ids",
        "delete_document_ids",
        "is_draft",
    };

    private static readonly HashSet<string> TopLevelArraySections = new(StringComparer.OrdinalIgnoreCase)
    {
        "education",
        "certifications",
        "skills",
        "experiences",
        "referrals",
    };

    private static readonly Dictionary<string, string> SyncFlagToPolicyPath = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sync_education"] = "education",
        ["sync_certifications"] = "certifications",
        ["sync_identifications"] = "identity.identifications",
    };

    private static readonly Dictionary<string, string> DeleteIdsToPolicyPath = new(StringComparer.OrdinalIgnoreCase)
    {
        ["delete_education_ids"] = "education",
        ["delete_certification_ids"] = "certifications",
        ["delete_identification_ids"] = "identity.identifications",
    };

    public static EmployeePayloadSplitResult Split(JsonObject payload, Func<string, JsonNode?> resolveCurrentValue)
    {
        var applyNow = new JsonObject();
        var pending = new List<EmployeePayloadPendingChange>();
        var rejected = new List<string>();
        var consumedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, node) in payload)
        {
            if (node is null)
                continue;

            if (AdminOnlyTopLevelKeys.Contains(key))
            {
                if (HasMeaningfulValue(node))
                    rejected.Add(key.Equals("employment", StringComparison.OrdinalIgnoreCase) ? "employment" : key);
                continue;
            }

            if (TopLevelArraySections.Contains(key))
            {
                RouteSection(key, payload, node, applyNow, pending, rejected, resolveCurrentValue, consumedKeys);
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "identity":
                    if (node is JsonObject identity)
                        RouteNestedSection("identity", identity, payload, applyNow, pending, rejected,
                            resolveCurrentValue, consumedKeys);
                    break;
                case "compensation":
                    if (node is JsonObject compensation)
                        RouteNestedSection("compensation", compensation, payload, applyNow, pending, rejected,
                            resolveCurrentValue, consumedKeys);
                    break;
                case "medical":
                    if (node is JsonObject medical)
                        RouteNestedSection("medical", medical, payload, applyNow, pending, rejected,
                            resolveCurrentValue, consumedKeys);
                    break;
                case "sync_education":
                case "sync_certifications":
                case "sync_identifications":
                case "delete_education_ids":
                case "delete_certification_ids":
                case "delete_identification_ids":
                    if (!consumedKeys.Contains(key) && HasMeaningfulValue(node))
                    {
                        var policyPath = SyncFlagToPolicyPath.GetValueOrDefault(key)
                            ?? DeleteIdsToPolicyPath.GetValueOrDefault(key);
                        if (policyPath is not null)
                            RouteStandaloneFlag(policyPath, key, node, applyNow, pending, rejected, resolveCurrentValue);
                    }
                    break;
                default:
                    if (HasMeaningfulValue(node))
                        rejected.Add(key);
                    break;
            }
        }

        return new EmployeePayloadSplitResult
        {
            ApplyNow = applyNow,
            PendingApproval = pending,
            Rejected = rejected.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        };
    }

    private static void RouteNestedSection(
        string section,
        JsonObject sectionNode,
        JsonObject fullPayload,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue,
        HashSet<string> consumedKeys)
    {
        foreach (var (prop, node) in sectionNode)
        {
            if (node is null)
                continue;

            var path = $"{section}.{prop}";
            if (string.Equals(prop, "identifications", StringComparison.OrdinalIgnoreCase))
            {
                RouteIdentificationsSection(fullPayload, node, applyNow, pending, rejected, resolveCurrentValue,
                    consumedKeys);
                continue;
            }

            RouteValue(path, prop, node, applyNow, pending, rejected, resolveCurrentValue, nestedSection: section);
        }
    }

    private static void RouteIdentificationsSection(
        JsonObject fullPayload,
        JsonNode identificationsNode,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue,
        HashSet<string> consumedKeys)
    {
        const string path = "identity.identifications";
        if (!HasMeaningfulValue(identificationsNode)
            && !HasMeaningfulValue(fullPayload["sync_identifications"])
            && !HasMeaningfulValue(fullPayload["delete_identification_ids"]))
            return;

        consumedKeys.Add("sync_identifications");
        consumedKeys.Add("delete_identification_ids");

        var wrapped = BuildSectionWrapper(
            identificationsNode,
            fullPayload["sync_identifications"],
            fullPayload["delete_identification_ids"]);

        RouteWrappedSection(path, wrapped, applyNow, pending, rejected, resolveCurrentValue, nestedSection: "identity",
            wireKey: "identifications", syncWireKey: "sync_identifications", deleteWireKey: "delete_identification_ids");
    }

    private static void RouteSection(
        string section,
        JsonObject fullPayload,
        JsonNode node,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue,
        HashSet<string> consumedKeys)
    {
        var syncKey = $"sync_{section}";
        var deleteKey = section switch
        {
            "education" => "delete_education_ids",
            "certifications" => "delete_certification_ids",
            _ => null,
        };

        if (!HasMeaningfulValue(node)
            && (deleteKey is null || !HasMeaningfulValue(fullPayload[deleteKey]))
            && !HasMeaningfulValue(fullPayload[syncKey]))
            return;

        consumedKeys.Add(syncKey);
        if (deleteKey is not null)
            consumedKeys.Add(deleteKey);

        var wrapped = BuildSectionWrapper(node, fullPayload[syncKey], deleteKey is null ? null : fullPayload[deleteKey]);
        RouteWrappedSection(section, wrapped, applyNow, pending, rejected, resolveCurrentValue,
            topLevelKey: section, syncWireKey: syncKey, deleteWireKey: deleteKey);
    }

    private static void RouteStandaloneFlag(
        string policyPath,
        string wireKey,
        JsonNode node,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue)
    {
        var access = FieldPolicy.ForEmployee(policyPath);
        if (access == FieldAccess.Free)
            applyNow[wireKey] = EmployeePayloadJsonNormalizer.Normalize(node)!;
        else if (access == FieldAccess.Approval)
        {
            var wrapped = BuildSectionWrapper(null, node, null);
            RouteWrappedSection(policyPath, wrapped, applyNow, pending, rejected, resolveCurrentValue,
                topLevelKey: wireKey);
        }
        else
            rejected.Add(policyPath);
    }

    private static JsonObject BuildSectionWrapper(JsonNode? value, JsonNode? syncFlag, JsonNode? deleteIds)
    {
        var wrapped = new JsonObject();
        var normalizedValue = EmployeePayloadJsonNormalizer.Normalize(value);
        if (normalizedValue is not null)
            wrapped["value"] = normalizedValue;

        if (syncFlag is JsonValue sync && sync.TryGetValue<bool>(out var syncBool) && syncBool)
            wrapped["sync"] = true;

        var normalizedDeletes = EmployeePayloadJsonNormalizer.Normalize(deleteIds);
        if (normalizedDeletes is not null)
            wrapped["delete_ids"] = normalizedDeletes;

        return wrapped;
    }

    private static void RouteWrappedSection(
        string policyPath,
        JsonObject wrapped,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue,
        string? topLevelKey = null,
        string? nestedSection = null,
        string? wireKey = null,
        string? syncWireKey = null,
        string? deleteWireKey = null)
    {
        if (wrapped.Count == 0)
            return;

        var access = FieldPolicy.ForEmployee(policyPath);
        switch (access)
        {
            case FieldAccess.Free:
                UnwrapToApplyNow(wrapped, applyNow, topLevelKey, nestedSection, wireKey, syncWireKey, deleteWireKey);
                break;
            case FieldAccess.Approval:
            {
                var oldValue = resolveCurrentValue(policyPath);
                if (EmployeePayloadJsonNormalizer.JsonDeepEquals(oldValue, wrapped))
                    break;
                pending.Add(new EmployeePayloadPendingChange
                {
                    FieldPath = policyPath,
                    OldValue = oldValue?.DeepClone(),
                    NewValue = wrapped.DeepClone(),
                });
                break;
            }
            default:
                rejected.Add(policyPath);
                break;
        }
    }

    private static void RouteValue(
        string policyPath,
        string wireKey,
        JsonNode node,
        JsonObject applyNow,
        List<EmployeePayloadPendingChange> pending,
        List<string> rejected,
        Func<string, JsonNode?> resolveCurrentValue,
        string? nestedSection = null)
    {
        var normalized = EmployeePayloadJsonNormalizer.Normalize(node);
        if (normalized is null)
            return;

        var access = FieldPolicy.ForEmployee(policyPath);
        switch (access)
        {
            case FieldAccess.Free:
                if (nestedSection is not null)
                {
                    if (applyNow[nestedSection] is not JsonObject sectionObj)
                    {
                        sectionObj = new JsonObject();
                        applyNow[nestedSection] = sectionObj;
                    }
                    sectionObj[wireKey] = normalized.DeepClone();
                }
                else
                    applyNow[wireKey] = normalized.DeepClone();
                break;
            case FieldAccess.Approval:
            {
                var oldValue = resolveCurrentValue(policyPath);
                if (EmployeePayloadJsonNormalizer.JsonDeepEquals(oldValue, normalized))
                    break;
                pending.Add(new EmployeePayloadPendingChange
                {
                    FieldPath = policyPath,
                    OldValue = oldValue?.DeepClone(),
                    NewValue = normalized.DeepClone(),
                });
                break;
            }
            default:
                rejected.Add(policyPath);
                break;
        }
    }

    private static void UnwrapToApplyNow(
        JsonObject wrapped,
        JsonObject applyNow,
        string? topLevelKey,
        string? nestedSection,
        string? wireKey,
        string? syncWireKey,
        string? deleteWireKey)
    {
        if (wrapped.TryGetPropertyValue("sync", out var syncNode) && syncWireKey is not null)
            applyNow[syncWireKey] = syncNode?.DeepClone();

        if (wrapped.TryGetPropertyValue("delete_ids", out var deleteNode) && deleteWireKey is not null)
            applyNow[deleteWireKey] = deleteNode?.DeepClone();

        if (!wrapped.TryGetPropertyValue("value", out var valueNode) || valueNode is null)
            return;

        if (nestedSection is not null && wireKey is not null)
        {
            if (applyNow[nestedSection] is not JsonObject sectionObj)
            {
                sectionObj = new JsonObject();
                applyNow[nestedSection] = sectionObj;
            }
            sectionObj[wireKey] = valueNode.DeepClone();
            return;
        }

        if (topLevelKey is not null)
            applyNow[topLevelKey] = valueNode.DeepClone();
    }

    private static bool HasMeaningfulValue(JsonNode? node) =>
        EmployeePayloadJsonNormalizer.Normalize(node) is not null;
}

internal static class EmployeePayloadJsonNormalizer
{
    public static JsonNode? Normalize(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonValue value:
                if (value.TryGetValue<string>(out var s))
                {
                    var trimmed = s.Trim();
                    return string.IsNullOrEmpty(trimmed) ? null : JsonValue.Create(trimmed);
                }
                return value.DeepClone();
            case JsonObject obj:
            {
                var normalized = new JsonObject();
                foreach (var (k, v) in obj)
                {
                    var child = Normalize(v);
                    if (child is not null)
                        normalized[k] = child;
                }
                return normalized.Count == 0 ? null : normalized;
            }
            case JsonArray arr:
            {
                var normalizedArr = new JsonArray();
                foreach (var item in arr)
                {
                    var child = Normalize(item);
                    if (child is not null)
                        normalizedArr.Add(child);
                }
                return normalizedArr.Count == 0 ? null : normalizedArr;
            }
            default:
                return node.DeepClone();
        }
    }

    public static bool JsonDeepEquals(JsonNode? a, JsonNode? b) =>
        JsonNode.DeepEquals(Normalize(a), Normalize(b));
}
