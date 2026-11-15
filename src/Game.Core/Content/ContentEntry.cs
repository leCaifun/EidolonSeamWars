namespace Game.Core.Content;

/// <summary>
/// Запись контента вместе с источником. Нужна для точных сообщений валидатора
/// и для журналирования override между паками.
/// </summary>
/// <typeparam name="T">Тип определения из <c>Game.Core.Content.Dto</c>.</typeparam>
public sealed record ContentEntry<T>(
    T Value,
    string SourceFile,
    string SourcePack)
    where T : class;
