namespace Game.Core.Content;

/// <summary>
/// Результат загрузки одного пака: сам пак (если манифест и файлы удалось прочитать)
/// и полный список диагностических сообщений.
/// </summary>
public sealed class ContentLoadResult
{
    /// <summary>
    /// Создаёт результат загрузки.
    /// </summary>
    /// <param name="pack">Загруженный пак или null, если манифест не прочитан.</param>
    /// <param name="diagnostics">Сообщения загрузчика.</param>
    /// <exception cref="ArgumentNullException">Набор сообщений не задан.</exception>
    public ContentLoadResult(ContentPack? pack, ContentDiagnosticCollection diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Pack = pack;
        Diagnostics = diagnostics;
    }

    /// <summary>Загруженный пак или null.</summary>
    public ContentPack? Pack { get; }

    /// <summary>Сообщения загрузчика.</summary>
    public ContentDiagnosticCollection Diagnostics { get; }

    /// <summary>Успешна ли загрузка: пак существует и ошибок нет.</summary>
    public bool IsSuccess => Pack is not null && !Diagnostics.HasErrors;
}
