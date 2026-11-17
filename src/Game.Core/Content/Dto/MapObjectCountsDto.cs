namespace Game.Core.Content.Dto;

/// <summary>Количество объектов каждого типа при генерации карты.</summary>
public sealed class MapObjectCountsDto
{
    /// <summary>Количество шахт.</summary>
    public int Mines { get; init; }

    /// <summary>Количество внешних жилищ.</summary>
    public int Dwellings { get; init; }

    /// <summary>Количество эхо-схронов.</summary>
    public int EchoCaches { get; init; }

    /// <summary>Количество патрулей.</summary>
    public int Patrols { get; init; }
}
