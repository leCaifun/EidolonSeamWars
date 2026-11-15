using Game.Core.Content;

namespace Game.Core.Tests;

/// <summary>
/// Доступ к настоящему контенту, скопированному рядом со сборкой тестов.
/// </summary>
internal static class RealContent
{
    private static readonly Lazy<ContentLoadResult> CoreLoad = new(LoadCore);

    public static string Directory => Path.Combine(AppContext.BaseDirectory, "content", "core");

    public static ContentLoadResult Load => CoreLoad.Value;

    public static ContentPack Pack => CoreLoad.Value.Pack
        ?? throw new InvalidOperationException("Базовый контент не загружен.");

    public static ContentMergeResult MergeWith(params ContentPack[] extraPacks)
    {
        List<ContentPack> packs = new() { Pack };
        packs.AddRange(extraPacks);

        return new ContentMerge().Merge(packs);
    }

    public static ContentPack LoadPack(IContentFileSystem fileSystem, string root)
    {
        ContentLoadResult result = new ContentLoader(fileSystem).Load(root);

        return result.Pack
            ?? throw new InvalidOperationException(
                $"Тестовый пак «{root}» не загружен: {result.Diagnostics.ErrorCount} ошибок.");
    }

    private static ContentLoadResult LoadCore()
    {
        FileContentFileSystem fileSystem = new(Directory);

        return new ContentLoader(fileSystem).Load(Directory);
    }
}
