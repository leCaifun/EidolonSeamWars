namespace Game.SimRunner;

/// <summary>
/// Ищет корневой каталог контентного пака. Явный аргумент имеет приоритет;
/// иначе каталог <c>game/content/core</c> (или <c>content/core</c> рядом с
/// исполняемым файлом) ищется вверх по дереву от каталога сборки.
/// </summary>
internal static class ContentPathResolver
{
    private static readonly string[] CandidateSuffixes =
    {
        "game/content/core",
        "content/core"
    };

    /// <summary>
    /// Возвращает абсолютный путь к каталогу пака.
    /// </summary>
    /// <param name="explicitPath">Путь из аргумента <c>--content</c> или null.</param>
    /// <param name="startDirectory">Каталог, от которого начинается поиск.</param>
    /// <exception cref="InvalidOperationException">Каталог не найден или не содержит манифест.</exception>
    public static string Resolve(string? explicitPath, string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            string full = Path.GetFullPath(explicitPath);
            EnsurePack(full, $"--content {explicitPath}");
            return full;
        }

        DirectoryInfo? directory = new(startDirectory);

        while (directory is not null)
        {
            foreach (string suffix in CandidateSuffixes)
            {
                string candidate = Path.Combine(
                    directory.FullName,
                    suffix.Replace('/', Path.DirectorySeparatorChar));

                if (IsPack(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Не найден каталог контента. Проверены пути {string.Join(", ", CandidateSuffixes)} " +
            $"вверх от «{startDirectory}». Укажите путь явно: --content <path>.");
    }

    private static void EnsurePack(string path, string source)
    {
        if (!IsPack(path))
        {
            throw new InvalidOperationException(
                $"Каталог «{path}» из {source} не существует или не содержит manifest.json.");
        }
    }

    private static bool IsPack(string path)
    {
        return Directory.Exists(path) && File.Exists(Path.Combine(path, "manifest.json"));
    }
}
