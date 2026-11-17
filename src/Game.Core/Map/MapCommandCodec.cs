using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Domain;

namespace Game.Core.Map;

/// <summary>
/// Сериализация команд карты в стабильный JSON и обратно. Формат — часть
/// контракта реплея (ADR-018): имена полей и строковые имена типов команд
/// нельзя менять без нового ADR и миграции.
/// </summary>
public static class MapCommandCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        WriteIndented = false
    };

    /// <summary>
    /// Сериализует последовательность команд в JSON-массив.
    /// </summary>
    /// <param name="commands">Команды в порядке применения.</param>
    /// <exception cref="ArgumentNullException">Список не задан.</exception>
    public static string Serialize(IEnumerable<MapCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        List<CommandDto> dto = new();

        foreach (MapCommand command in commands)
        {
            dto.Add(new CommandDto
            {
                Kind = command.Kind.ToString(),
                PartyId = command.PartyId,
                TargetQ = command.Target?.Q,
                TargetR = command.Target?.R,
                ObjectInstanceId = command.ObjectInstanceId
            });
        }

        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>
    /// Разбирает JSON-массив команд.
    /// </summary>
    /// <param name="json">JSON, созданный <see cref="Serialize"/>.</param>
    /// <exception cref="ArgumentException">Строка пуста.</exception>
    /// <exception cref="JsonException">JSON некорректен или содержит неизвестный тип команды.</exception>
    public static IReadOnlyList<MapCommand> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON команд не может быть пустым.", nameof(json));
        }

        List<CommandDto>? dto = JsonSerializer.Deserialize<List<CommandDto>>(json, Options);

        if (dto is null)
        {
            throw new JsonException("JSON команд не содержит массива.");
        }

        List<MapCommand> commands = new(dto.Count);

        foreach (CommandDto item in dto)
        {
            commands.Add(ToCommand(item));
        }

        return commands;
    }

    private static MapCommand ToCommand(CommandDto dto)
    {
        if (!Enum.TryParse(dto.Kind, ignoreCase: false, out MapCommandKind kind))
        {
            throw new JsonException($"Неизвестный тип команды карты «{dto.Kind}».");
        }

        return kind switch
        {
            MapCommandKind.MoveParty => MapCommand.MoveParty(
                RequirePartyId(dto.PartyId),
                new Hex(RequireCoordinate(dto.TargetQ, "targetQ"), RequireCoordinate(dto.TargetR, "targetR"))),
            MapCommandKind.EndDay => MapCommand.EndDay(),
            MapCommandKind.EnterObject => MapCommand.EnterObject(
                RequirePartyId(dto.PartyId),
                RequireObjectId(dto.ObjectInstanceId)),
            _ => throw new JsonException($"Тип команды карты «{dto.Kind}» не поддерживается.")
        };
    }

    private static string RequirePartyId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("У команды карты отсутствует partyId.");
        }

        return value;
    }

    private static string RequireObjectId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("У команды входа в объект отсутствует objectInstanceId.");
        }

        return value;
    }

    private static int RequireCoordinate(int? value, string field)
    {
        return value ?? throw new JsonException($"У команды движения отсутствует поле «{field}».");
    }

    private sealed class CommandDto
    {
        public string Kind { get; init; } = string.Empty;

        public string? PartyId { get; init; }

        public int? TargetQ { get; init; }

        public int? TargetR { get; init; }

        public string? ObjectInstanceId { get; init; }
    }
}
