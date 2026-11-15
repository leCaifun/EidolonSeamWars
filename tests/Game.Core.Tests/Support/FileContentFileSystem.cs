using System.Text;
using Game.Core.Content;

namespace Game.Core.Tests;

/// <summary>
/// Файловая абстракция поверх реального каталога. Нужна тестам, которые
/// проверяют настоящий контент в <c>content/core</c> рядом со сборкой.
/// </summary>
internal sealed class FileContentFileSystem : IContentFileSystem
{
    private readonly string _root;

    public FileContentFileSystem(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
    }

    public IReadOnlyList<string> ListFiles(string directory)
    {
        if (!Directory.Exists(_root))
        {
            throw new InvalidOperationException($"Каталог контента не найден: {_root}");
        }

        return Directory
            .EnumerateFiles(_root, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public string ReadAllText(string directory, string relativePath)
    {
        string path = Path.Combine(
            _root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        return File.ReadAllText(path, Encoding.UTF8);
    }
}
