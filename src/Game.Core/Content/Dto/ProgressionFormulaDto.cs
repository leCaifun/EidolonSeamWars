namespace Game.Core.Content.Dto;

/// <summary>Формула опыта героя из <c>rules/progression.json</c> (ADR-025).</summary>
public sealed class ProgressionFormulaDto
{
    /// <summary>Тип формулы; в MVP — <c>quadratic</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Коэффициент формулы.</summary>
    public int Coefficient { get; init; }

    /// <summary>Человекочитаемое выражение для документации.</summary>
    public string Expression { get; init; } = string.Empty;
}
