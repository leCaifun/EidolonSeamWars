namespace Game.Core.Content;

/// <summary>Важность диагностического сообщения загрузчика или валидатора.</summary>
public enum ContentDiagnosticSeverity
{
    /// <summary>Предупреждение: контент загружается, но требует внимания.</summary>
    Warning = 0,

    /// <summary>Ошибка: контент не считается валидным.</summary>
    Error = 1
}
