using Game.Core.Content;

namespace Game.Core.Tests;

/// <summary>
/// Файловая абстракция в памяти: позволяет собирать тестовые паки без диска.
/// Корневой каталог игнорируется, пути нормализуются к прямым слэшам.
/// </summary>
internal sealed class InMemoryContentFileSystem : IContentFileSystem
{
    private readonly Dictionary<string, string> _files;

    public InMemoryContentFileSystem(IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        _files = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> pair in files)
        {
            _files[Normalize(pair.Key)] = pair.Value;
        }
    }

    public IReadOnlyList<string> ListFiles(string directory)
    {
        return _files.Keys
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public string ReadAllText(string directory, string relativePath)
    {
        string normalized = Normalize(relativePath);

        if (!_files.TryGetValue(normalized, out string? content))
        {
            throw new InvalidOperationException($"Файл «{normalized}» не найден в тестовом паке.");
        }

        return content;
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
