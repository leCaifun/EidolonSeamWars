namespace Game.Core.Content.Dto;

/// <summary>Правила биомов из <c>rules/biomes.json</c>.</summary>
public sealed class BiomesRulesDto
{
    /// <summary>Версия схемы.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>ID набора правил.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Список биомов.</summary>
    public IReadOnlyList<BiomeDto> Biomes { get; init; } = Array.Empty<BiomeDto>();
}
