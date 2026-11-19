using System.Text.Json;
using Game.Core.Domain;

namespace Game.Core.Battle;

/// <summary>
/// JSON-сериализация журнала команд боя для реплея и сохранений (инвариант 4). Формат строгий:
/// camelCase, неизвестные поля и комментарии запрещены. Вместе с сидом боя и составом сторон журнал
/// полностью определяет результат боя.
/// </summary>
public static class BattleCommandCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        WriteIndented = false
    };

    /// <summary>
    /// Сериализует команды в компактный JSON-массив.
    /// </summary>
    /// <param name="commands">Команды в порядке применения.</param>
    /// <exception cref="ArgumentNullException">Список не задан.</exception>
    public static string Serialize(IEnumerable<BattleCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        List<CommandDto> dto = new();

        foreach (BattleCommand command in commands)
        {
            dto.Add(new CommandDto
            {
                Kind = command.Kind.ToString(),
                UnitId = command.UnitId,
                TargetQ = command.TargetHex?.Q,
                TargetR = command.TargetHex?.R,
                TargetUnitId = command.TargetUnitId,
                AbilityId = command.AbilityId,
                Side = command.Side?.ToString()
            });
        }

        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>
    /// Разбирает JSON-массив команд боя.
    /// </summary>
    /// <param name="json">JSON, созданный <see cref="Serialize"/>.</param>
    /// <exception cref="ArgumentException">Пустая строка.</exception>
    /// <exception cref="JsonException">Неверный формат, неизвестный тип команды или не хватает полей.</exception>
    public static IReadOnlyList<BattleCommand> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON команд боя не может быть пустым.", nameof(json));
        }

        List<CommandDto>? dto = JsonSerializer.Deserialize<List<CommandDto>>(json, Options);

        if (dto is null)
        {
            throw new JsonException("JSON команд боя не содержит массива.");
        }

        List<BattleCommand> commands = new(dto.Count);

        foreach (CommandDto item in dto)
        {
            commands.Add(ToCommand(item));
        }

        return commands;
    }

    private static BattleCommand ToCommand(CommandDto dto)
    {
        if (!Enum.TryParse(dto.Kind, ignoreCase: false, out BattleCommandKind kind))
        {
            throw new JsonException($"Неизвестный тип команды боя «{dto.Kind}».");
        }

        string unitId = RequireText(dto.UnitId, "unitId");

        return kind switch
        {
            BattleCommandKind.Move => BattleCommand.Move(
                unitId,
                new Hex(RequireCoordinate(dto.TargetQ, "targetQ"), RequireCoordinate(dto.TargetR, "targetR"))),
            BattleCommandKind.Wait => BattleCommand.Wait(unitId),
            BattleCommandKind.Defend => BattleCommand.Defend(unitId),
            BattleCommandKind.Attack => BattleCommand.Attack(unitId, RequireText(dto.TargetUnitId, "targetUnitId")),
            BattleCommandKind.UseAbility => BattleCommand.UseAbility(
                unitId,
                RequireText(dto.AbilityId, "abilityId"),
                dto.TargetUnitId),
            BattleCommandKind.CastSpell => BattleCommand.CastSpell(
                RequireSide(dto.Side),
                RequireText(dto.TargetUnitId, "targetUnitId")),
            BattleCommandKind.UseCommandAbility => BattleCommand.UseCommandAbility(RequireSide(dto.Side)),
            _ => throw new JsonException($"Тип команды боя «{dto.Kind}» не поддерживается.")
        };
    }

    private static BattleSide RequireSide(string? value)
    {
        if (!Enum.TryParse(value, ignoreCase: false, out BattleSide side))
        {
            throw new JsonException($"У команды героя неверное или отсутствует поле «side»: «{value}».");
        }

        return side;
    }

    private static string RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"У команды боя отсутствует поле «{field}».");
        }

        return value;
    }

    private static int RequireCoordinate(int? value, string field)
    {
        return value ?? throw new JsonException($"У команды перемещения отсутствует поле «{field}».");
    }

    private sealed class CommandDto
    {
        public string Kind { get; init; } = string.Empty;

        public string? UnitId { get; init; }

        public int? TargetQ { get; init; }

        public int? TargetR { get; init; }

        public string? TargetUnitId { get; init; }

        public string? AbilityId { get; init; }

        public string? Side { get; init; }
    }
}
