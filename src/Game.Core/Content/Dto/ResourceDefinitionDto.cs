namespace Game.Core.Content.Dto;

/// <summary>Описание одного из семи ресурсов (ADR-005).</summary>
public sealed class ResourceDefinitionDto
{
    /// <summary>ID ресурса: gold, wood, ore, crystal, sulfur, mercury, essence.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>Роль ресурса: currency или material.</summary>
    public string Role { get; init; } = string.Empty;
}
