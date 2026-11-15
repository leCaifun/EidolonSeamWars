namespace Game.Core.Content.Dto;

/// <summary>
/// Манифест контентного пака. Определяет версию, зависимости и порядок загрузки.
/// </summary>
public sealed class ModManifestDto
{
    /// <summary>Версия схемы манифеста.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Стабильный ID пака; для core-контента — <c>core</c>.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Версия пака в SemVer.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Диапазон совместимых версий игры.</summary>
    public string GameVersionRange { get; init; } = string.Empty;

    /// <summary>ID паков, которые обязаны загрузиться раньше.</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>Порядок загрузки среди паков с одинаковыми зависимостями.</summary>
    public int LoadOrder { get; init; }
}
