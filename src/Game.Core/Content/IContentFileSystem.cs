namespace Game.Core.Content;

/// <summary>
/// Абстракция чтения файлов контента. Прямой файловый ввод-вывод в Game.Core
/// запрещён (ADR-024): реализации живут в Game.SimRunner и Godot-слое.
/// </summary>
public interface IContentFileSystem
{
    /// <summary>
    /// Возвращает файлы каталога рекурсивно. Пути относительны переданного
    /// каталога, разделители — прямые слэши, регистр сохраняется.
    /// </summary>
    /// <param name="directory">Корневой каталог пака.</param>
    /// <returns>Отсортированный по возрастанию список относительных путей.</returns>
    IReadOnlyList<string> ListFiles(string directory);

    /// <summary>
    /// Читает текстовый файл пака в кодировке UTF-8.
    /// </summary>
    /// <param name="directory">Корневой каталог пака.</param>
    /// <param name="relativePath">Путь, полученный из <see cref="ListFiles"/>.</param>
    /// <exception cref="InvalidOperationException">Файл не найден или недоступен.</exception>
    string ReadAllText(string directory, string relativePath);
}
