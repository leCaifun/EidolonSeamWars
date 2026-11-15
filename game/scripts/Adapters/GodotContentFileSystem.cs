using Godot;
using Game.Core.Content;
using GodotFileAccess = Godot.FileAccess;

/// <summary>
/// Адаптер <see cref="IContentFileSystem"/> для Godot: читает файлы пака
/// через <see cref="DirAccess"/> и <see cref="FileAccess"/>. Прямой файловый
/// ввод-вывод в Game.Core запрещён (ADR-024), поэтому реализация живёт
/// в презентационном слое.
/// </summary>
public sealed class GodotContentFileSystem : IContentFileSystem
{
    private const int MaxDepth = 16;

    /// <summary>
    /// Рекурсивно перечисляет JSON-файлы каталога. Пути относительны
    /// <paramref name="directory"/> и используют прямые слэши.
    /// </summary>
    /// <param name="directory">Каталог пака, например <c>res://content/core</c>.</param>
    /// <exception cref="InvalidOperationException">Каталог не открывается.</exception>
    public IReadOnlyList<string> ListFiles(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        List<string> files = new();
        Collect(directory, string.Empty, files, depth: 0);
        files.Sort(StringComparer.Ordinal);

        return files.AsReadOnly();
    }

    /// <summary>
    /// Читает текстовый файл пака в кодировке UTF-8.
    /// </summary>
    /// <param name="directory">Каталог пака.</param>
    /// <param name="relativePath">Путь, полученный из <see cref="ListFiles"/>.</param>
    /// <exception cref="InvalidOperationException">Файл не найден или не читается.</exception>
    public string ReadAllText(string directory, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string path = $"{directory.TrimEnd('/')}/{relativePath}";

        if (!GodotFileAccess.FileExists(path))
        {
            throw new InvalidOperationException($"Файл контента не найден: {path}");
        }

        string content = GodotFileAccess.GetFileAsString(path);

        if (string.IsNullOrEmpty(content))
        {
            throw new InvalidOperationException($"Файл контента пуст или недоступен: {path}");
        }

        return content;
    }

    private static void Collect(string absoluteDirectory, string relativeDirectory, List<string> files, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidOperationException(
                $"Превышена глубина обхода каталога контента: {absoluteDirectory}");
        }

        using DirAccess? directory = DirAccess.Open(absoluteDirectory);

        if (directory is null)
        {
            throw new InvalidOperationException(
                $"Каталог контента не открывается: {absoluteDirectory} " +
                $"(ошибка {DirAccess.GetOpenError()})");
        }

        foreach (string file in directory.GetFiles())
        {
            if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            files.Add(relativeDirectory.Length == 0 ? file : $"{relativeDirectory}/{file}");
        }

        foreach (string subdirectory in directory.GetDirectories())
        {
            string nextRelative = relativeDirectory.Length == 0
                ? subdirectory
                : $"{relativeDirectory}/{subdirectory}";

            Collect($"{absoluteDirectory.TrimEnd('/')}/{subdirectory}", nextRelative, files, depth + 1);
        }
    }
}
