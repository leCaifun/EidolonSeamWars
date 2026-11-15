using System.Text;
using Game.Core.Content;

namespace Game.SimRunner;

/// <summary>
/// Файловая абстракция для CLI: читает контент с диска. Прямой файловый
/// ввод-вывод в Game.Core запрещён (ADR-024), поэтому реализация живёт здесь.
/// </summary>
internal sealed class DiskContentFileSystem : IContentFileSystem
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Каталог не найден или недоступен.</exception>
    public IReadOnlyList<string> ListFiles(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        try
        {
            if (!Directory.Exists(directory))
            {
                throw new InvalidOperationException($"Каталог контента не найден: {directory}");
            }

            return Directory
                .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(directory, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Не удалось прочитать каталог контента «{directory}»: {exception.Message}",
                exception);
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Файл не найден или недоступен.</exception>
    public string ReadAllText(string directory, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string path = Path.Combine(
            directory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Не удалось прочитать файл контента «{path}»: {exception.Message}",
                exception);
        }
    }
}
