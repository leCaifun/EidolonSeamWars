using System.Text.Json;
using Game.Core.Content.Dto;
using Game.Core.Content.Internal;
using Game.Core.Content.Localization;

namespace Game.Core.Content;

/// <summary>
/// Результат загрузки локализации пака: манифест языков и таблицы переводов.
/// </summary>
public sealed class LocalizationLoadResult
{
    /// <summary>
    /// Создаёт результат.
    /// </summary>
    public LocalizationLoadResult(
        ContentEntry<LanguagesManifestDto>? manifest,
        IReadOnlyList<LocalizationTable> tables,
        ContentDiagnosticCollection diagnostics)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Manifest = manifest;
        Tables = tables;
        Diagnostics = diagnostics;
    }

    /// <summary>Манифест языков или null, если файл отсутствует или не прочитан.</summary>
    public ContentEntry<LanguagesManifestDto>? Manifest { get; }

    /// <summary>Загруженные языковые таблицы.</summary>
    public IReadOnlyList<LocalizationTable> Tables { get; }

    /// <summary>Сообщения загрузчика локализации.</summary>
    public ContentDiagnosticCollection Diagnostics { get; }

    /// <summary>Успешна ли загрузка: манифест есть и ошибок нет.</summary>
    public bool IsSuccess => Manifest is not null && !Diagnostics.HasErrors;
}

/// <summary>
/// Загружает <c>localization/languages.json</c> и языковые файлы. Проверяет
/// обязательные поля, статусы и дубликаты языков, но не сравнивает покрытие
/// ключей — это делает <see cref="ContentValidator"/>.
/// </summary>
public sealed class LocalizationLoader
{
    private readonly IContentFileSystem _fileSystem;

    /// <summary>
    /// Создаёт загрузчик локализации.
    /// </summary>
    /// <exception cref="ArgumentNullException">Файловая абстракция не задана.</exception>
    public LocalizationLoader(IContentFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Загружает локализацию пака.
    /// </summary>
    /// <param name="rootDirectory">Корень пака.</param>
    /// <param name="relativeFiles">Файлы каталога <c>localization</c>.</param>
    /// <param name="sourcePack">ID пака для сообщений.</param>
    public LocalizationLoadResult Load(
        string rootDirectory,
        IReadOnlyList<string> relativeFiles,
        string sourcePack)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(relativeFiles);

        ContentDiagnosticCollection diagnostics = new();
        ContentEntry<LanguagesManifestDto>? manifest = null;
        List<LocalizationTable> tables = new();
        Dictionary<string, string> languageFiles = new(StringComparer.Ordinal);

        foreach (string relativePath in relativeFiles)
        {
            string normalized = relativePath.Replace('\\', '/');

            if (!normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (normalized.Equals("localization/languages.json", StringComparison.Ordinal))
            {
                manifest = ReadManifest(rootDirectory, normalized, sourcePack, diagnostics);
                continue;
            }

            ReadTable(rootDirectory, normalized, diagnostics, tables, languageFiles);
        }

        if (manifest is null)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_MANIFEST_MISSING",
                "Не найден обязательный файл localization/languages.json.",
                ContentJsonReader.BuildSourcePath(rootDirectory, "localization/languages.json")));
        }

        return new LocalizationLoadResult(manifest, tables.AsReadOnly(), diagnostics);
    }

    private ContentEntry<LanguagesManifestDto>? ReadManifest(
        string rootDirectory,
        string relativePath,
        string sourcePack,
        ContentDiagnosticCollection diagnostics)
    {
        if (!ContentJsonReader.TryReadWithRoot<LanguagesManifestDto>(
                _fileSystem,
                rootDirectory,
                relativePath,
                diagnostics,
                out LanguagesManifestDto? manifest,
                out JsonElement root))
        {
            return null;
        }

        if (manifest is null)
        {
            return null;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);

        RequireString(root, "defaultLanguage", source, diagnostics);
        RequireString(root, "fallbackLanguage", source, diagnostics);
        RequireArray(root, "requiredLanguages", source, diagnostics);
        RequireArray(root, "languages", source, diagnostics);

        if (string.IsNullOrWhiteSpace(manifest.DefaultLanguage))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_DEFAULT_LANGUAGE_REQUIRED",
                "Поле defaultLanguage обязательно.",
                source,
                field: "defaultLanguage"));
        }

        if (string.IsNullOrWhiteSpace(manifest.FallbackLanguage))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_FALLBACK_LANGUAGE_REQUIRED",
                "Поле fallbackLanguage обязательно.",
                source,
                field: "fallbackLanguage"));
        }

        if (manifest.RequiredLanguages.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_REQUIRED_LANGUAGES_EMPTY",
                "Список requiredLanguages не может быть пустым.",
                source,
                field: "requiredLanguages"));
        }

        if (manifest.Languages.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_LANGUAGES_EMPTY",
                "Список languages не может быть пустым.",
                source,
                field: "languages"));
        }

        Dictionary<string, string> seen = new(StringComparer.Ordinal);

        foreach (LocalizationLanguageDto language in manifest.Languages)
        {
            if (string.IsNullOrWhiteSpace(language.Code))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_LANGUAGE_CODE_REQUIRED",
                    "У языка отсутствует код.",
                    source,
                    field: "languages.code"));

                continue;
            }

            if (string.IsNullOrWhiteSpace(language.NameKey))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_LANGUAGE_NAME_KEY_REQUIRED",
                    $"У языка «{language.Code}» отсутствует ключ названия.",
                    source,
                    language.Code,
                    "languages.nameKey"));
            }

            if (string.IsNullOrWhiteSpace(language.Script))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_LANGUAGE_SCRIPT_REQUIRED",
                    $"У языка «{language.Code}» отсутствует письменность.",
                    source,
                    language.Code,
                    "languages.script"));
            }

            if (!LocalizationStatusParser.TryParse(language.Status, out _))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_LANGUAGE_STATUS_INVALID",
                    $"Неизвестный статус языка «{language.Status}»; допустимы complete, draft, experimental.",
                    source,
                    language.Code,
                    "languages.status"));
            }

            if (!seen.TryAdd(language.Code, language.NameKey))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_LANGUAGE_DUPLICATE",
                    $"Код языка «{language.Code}» зарегистрирован более одного раза.",
                    source,
                    language.Code,
                    "languages.code"));
            }
        }

        return new ContentEntry<LanguagesManifestDto>(manifest, source, sourcePack);
    }

    private void ReadTable(
        string rootDirectory,
        string relativePath,
        ContentDiagnosticCollection diagnostics,
        List<LocalizationTable> tables,
        Dictionary<string, string> languageFiles)
    {
        if (!ContentJsonReader.TryReadWithRoot<LocalizationTableDto>(
                _fileSystem,
                rootDirectory,
                relativePath,
                diagnostics,
                out LocalizationTableDto? dto,
                out JsonElement root))
        {
            return;
        }

        if (dto is null)
        {
            return;
        }

        string source = ContentJsonReader.BuildSourcePath(rootDirectory, relativePath);

        RequireString(root, "language", source, diagnostics);
        RequireString(root, "status", source, diagnostics);
        RequireProperty(root, "fallback", source, diagnostics);
        RequireObject(root, "entries", source, diagnostics);

        if (string.IsNullOrWhiteSpace(dto.Language))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_TABLE_LANGUAGE_REQUIRED",
                "Поле language обязательно.",
                source,
                field: "language"));

            return;
        }

        if (!LocalizationStatusParser.TryParse(dto.Status, out LocalizationStatus status))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_TABLE_STATUS_INVALID",
                $"Неизвестный статус языка «{dto.Status}»; допустимы complete, draft, experimental.",
                source,
                dto.Language,
                "status"));
        }

        if (dto.Entries.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_TABLE_ENTRIES_EMPTY",
                "Словарь entries не может быть пустым.",
                source,
                dto.Language,
                "entries"));
        }

        foreach (KeyValuePair<string, string> pair in dto.Entries)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "LOC_KEY_EMPTY",
                    "Ключ локализации не может быть пустым.",
                    source,
                    dto.Language,
                    "entries"));
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                diagnostics.Add(ContentDiagnostic.Warning(
                    "LOC_VALUE_EMPTY",
                    $"Пустое значение перевода для ключа «{pair.Key}».",
                    source,
                    dto.Language,
                    "entries"));
            }
        }

        int slashIndex = relativePath.LastIndexOf('/');
        string fileName = slashIndex >= 0 ? relativePath[(slashIndex + 1)..] : relativePath;
        int dotIndex = fileName.LastIndexOf('.');
        string stem = dotIndex > 0 ? fileName[..dotIndex] : fileName;

        if (!string.Equals(stem, dto.Language, StringComparison.Ordinal))
        {
            diagnostics.Add(ContentDiagnostic.Warning(
                "LOC_FILENAME_MISMATCH",
                $"Имя файла «{stem}.json» не совпадает с полем language «{dto.Language}».",
                source,
                dto.Language,
                "language"));
        }

        if (languageFiles.TryGetValue(dto.Language, out string? existingFile))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "LOC_TABLE_DUPLICATE",
                $"Язык «{dto.Language}» уже загружен из файла {existingFile}.",
                source,
                dto.Language,
                "language"));

            return;
        }

        languageFiles[dto.Language] = relativePath;
        tables.Add(new LocalizationTable(
            dto.Language,
            status,
            string.IsNullOrWhiteSpace(dto.Fallback) ? null : dto.Fallback,
            dto.Entries,
            source));
    }

    private static void RequireProperty(
        JsonElement root,
        string property,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!root.TryGetProperty(property, out _))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FIELD_REQUIRED",
                $"Обязательное поле «{property}» отсутствует.",
                source,
                field: property));
        }
    }

    private static void RequireString(
        JsonElement root,
        string property,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!root.TryGetProperty(property, out JsonElement element)
            || element.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FIELD_REQUIRED",
                $"Обязательное строковое поле «{property}» отсутствует.",
                source,
                field: property));
        }
    }

    private static void RequireArray(
        JsonElement root,
        string property,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!root.TryGetProperty(property, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FIELD_REQUIRED",
                $"Обязательное поле-массив «{property}» отсутствует.",
                source,
                field: property));
        }
    }

    private static void RequireObject(
        JsonElement root,
        string property,
        string source,
        ContentDiagnosticCollection diagnostics)
    {
        if (!root.TryGetProperty(property, out JsonElement element)
            || element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FIELD_REQUIRED",
                $"Обязательное поле-объект «{property}» отсутствует.",
                source,
                field: property));
        }
    }
}
