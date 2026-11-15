using System.Text.Json;

namespace Game.Core.Tests;

/// <summary>
/// Сборщик тестовых JSON-файлов контента. Форма файлов повторяет реальные:
/// одиночные сущности и коллекции без <c>schemaVersion</c> на элементе.
/// </summary>
internal static class TestPacks
{
    public static string Manifest(
        string id,
        int loadOrder = 0,
        params string[] dependencies)
    {
        string list = string.Join(", ", dependencies.Select(dependency => $"\"{dependency}\""));

        return $$"""
        {
          "schemaVersion": 1,
          "id": "{{id}}",
          "version": "0.0.1",
          "gameVersionRange": ">=0.3.0 <0.4.0",
          "dependencies": [{{list}}],
          "loadOrder": {{loadOrder}}
        }
        """;
    }

    /// <summary>
    /// Юнит с разумными значениями по умолчанию. <paramref name="attack"/>
    /// строкой, чтобы можно было подать дробное число для негативного теста.
    /// <paramref name="extraMembers"/> вставляется как дополнительные поля
    /// и должен начинаться с запятой.
    /// </summary>
    public static string Unit(
        string id,
        string factionId = "core.faction.brass",
        string? nameKey = null,
        string? descriptionKey = null,
        string attack = "4",
        string extraMembers = "")
    {
        string name = nameKey ?? $"{id}.name";
        string description = descriptionKey ?? $"{id}.desc";

        return $$"""
        {
          "id": "{{id}}",
          "factionId": "{{factionId}}",
          "tier": 1,
          "unitClass": "infantry",
          "movementType": "foot",
          "damageType": "physical",
          "leadershipCost": 1,
          "attack": {{attack}},
          "defense": 5,
          "initiative": 9,
          "speed": 4,
          "damageMin": 2,
          "damageMax": 4,
          "critChancePercent": 5,
          "critDamagePercent": 150,
          "hitPoints": 12,
          "resistances": { "physical": 0, "magical": 0, "poison": 0, "fire": 0, "firearm": 0 },
          "abilities": [],
          "recruitCost": { "gold": 25 },
          "nameKey": "{{name}}",
          "descriptionKey": "{{description}}"{{extraMembers}}
        }
        """;
    }

    public static string UnitsFile(string fileId, params string[] units)
    {
        return $$"""
        {
          "schemaVersion": 1,
          "id": "{{fileId}}",
          "units": [
        {{string.Join(",\n", units)}}
          ]
        }
        """;
    }

    public static string LocalizationFile(
        string language,
        string status,
        IReadOnlyDictionary<string, string> entries)
    {
        Dictionary<string, object> payload = new()
        {
            ["schemaVersion"] = 1,
            ["id"] = $"test.localization.{language}",
            ["language"] = language,
            ["status"] = status,
            ["fallback"] = "en",
            ["entries"] = entries
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    public static InMemoryContentFileSystem Pack(
        string id,
        int loadOrder = 0,
        string[]? dependencies = null,
        params (string Path, string Json)[] files)
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal)
        {
            ["manifest.json"] = Manifest(id, loadOrder, dependencies ?? Array.Empty<string>())
        };

        foreach ((string path, string json) in files)
        {
            map[path] = json;
        }

        return new InMemoryContentFileSystem(map);
    }
}
