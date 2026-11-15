using Game.Core.Content.Dto;

namespace Game.Core.Content.Internal;

/// <summary>Файл коллекции юнитов.</summary>
internal sealed class UnitsFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<UnitDto> Units { get; init; } = new();
}

/// <summary>Файл коллекции способностей.</summary>
internal sealed class AbilitiesFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<AbilityDto> Abilities { get; init; } = new();
}

/// <summary>Файл коллекции заклинаний.</summary>
internal sealed class SpellsFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<SpellDto> Spells { get; init; } = new();
}

/// <summary>Файл коллекции зданий.</summary>
internal sealed class BuildingsFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<BuildingDto> Buildings { get; init; } = new();
}

/// <summary>Файл коллекции объектов карты.</summary>
internal sealed class MapObjectsFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<MapObjectDto> Objects { get; init; } = new();
}

/// <summary>Файл списка ресурсов.</summary>
internal sealed class ResourcesFileDto
{
    public int SchemaVersion { get; init; }

    public string Id { get; init; } = string.Empty;

    public List<ResourceDefinitionDto> Resources { get; init; } = new();
}
