using System.Collections.ObjectModel;

namespace Game.Core.Map;

/// <summary>
/// Иммутабельное описание объекта карты из <c>map_objects/*.json</c>:
/// шахта, жилище, эхо-схрон, патруль или поселение. Код не содержит
/// имён объектов; тип — токен схемы, а не ID контента.
/// </summary>
public sealed class MapObjectDefinition
{
    /// <summary>Токен типа «патруль» из схемы контента.</summary>
    public const string PatrolType = "patrol";

    private readonly ReadOnlyDictionary<string, int> _incomePerDay;

    /// <summary>
    /// Создаёт описание объекта.
    /// </summary>
    /// <exception cref="ArgumentException">Пустой ID, тип, ключ названия или описания.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Отрицательный доход.</exception>
    public MapObjectDefinition(
        string id,
        string type,
        string nameKey,
        string descriptionKey,
        bool guarded,
        string? factionId = null,
        string? templateId = null,
        int? initialLevel = null,
        IReadOnlyDictionary<string, int>? incomePerDay = null)
    {
        Id = RequireText(id, nameof(id));
        Type = RequireText(type, nameof(type));
        NameKey = RequireText(nameKey, nameof(nameKey));
        DescriptionKey = RequireText(descriptionKey, nameof(descriptionKey));

        Dictionary<string, int> income = new(StringComparer.Ordinal);

        if (incomePerDay is not null)
        {
            foreach (KeyValuePair<string, int> pair in incomePerDay)
            {
                if (pair.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(incomePerDay),
                        pair.Value,
                        $"Доход ресурса «{pair.Key}» не может быть отрицательным.");
                }

                income[pair.Key] = pair.Value;
            }
        }

        Guarded = guarded;
        FactionId = factionId;
        TemplateId = templateId;
        InitialLevel = initialLevel;
        _incomePerDay = new ReadOnlyDictionary<string, int>(income);
    }

    /// <summary>Стабильный ID объекта.</summary>
    public string Id { get; }

    /// <summary>Тип объекта из схемы: mine, dwelling, echoCache, patrol, settlement.</summary>
    public string Type { get; }

    /// <summary>Ключ локализации названия.</summary>
    public string NameKey { get; }

    /// <summary>Ключ локализации описания.</summary>
    public string DescriptionKey { get; }

    /// <summary>Требует ли объект боя при входе.</summary>
    public bool Guarded { get; }

    /// <summary>ID фракции для жилищ, патрулей и поселений.</summary>
    public string? FactionId { get; }

    /// <summary>ID шаблона поселения.</summary>
    public string? TemplateId { get; }

    /// <summary>Начальный уровень поселения.</summary>
    public int? InitialLevel { get; }

    /// <summary>Доход в день по ID ресурса.</summary>
    public IReadOnlyDictionary<string, int> IncomePerDay => _incomePerDay;

    /// <summary>
    /// Блокирует ли объект вход без боя. Патруль — это армия, поэтому он
    /// блокирует всегда; остальные объекты — только при <see cref="Guarded"/>.
    /// </summary>
    public bool IsBlocking =>
        Guarded || string.Equals(Type, PatrolType, StringComparison.Ordinal);

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строковое поле объекта карты не может быть пустым.", parameterName);
        }

        return value;
    }
}
