using System.Text.Json;

namespace StrategicoAudit.Api.Helpers;

public static class JsonHelpers
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static JsonDocument ToJsonDocument<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        return JsonDocument.Parse(json);
    }

    public static Dictionary<string, object?> MaskSensitive(Dictionary<string, object?> data)
    {
        
        var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "apiKey", "secret", "authorization"
        };

        foreach (var key in data.Keys.ToList())
        {
            if (sensitiveKeys.Contains(key))
                data[key] = "***MASKED***";
        }

        return data;
    }

    public static Dictionary<string, object?> Diff(
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues)
    {
        var diff = new Dictionary<string, object?>();

        foreach (var key in oldValues.Keys.Union(newValues.Keys))
        {
            oldValues.TryGetValue(key, out var oldV);
            newValues.TryGetValue(key, out var newV);

            if (!Equals(oldV, newV))
            {
                diff[key] = new Dictionary<string, object?>
                {
                    ["old"] = oldV,
                    ["new"] = newV
                };
            }
        }

        return diff;
    }
}
