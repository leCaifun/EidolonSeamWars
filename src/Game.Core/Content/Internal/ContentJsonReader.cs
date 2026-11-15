using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Core.Content.Internal;

/// <summary>
/// Строгое чтение JSON-файлов контента: комментарии, trailing commas, неизвестные
/// поля и дробные значения запрещены. Каждый файл обязан содержать
/// <c>schemaVersion</c> поддерживаемой версии и непустой <c>id</c>.
/// </summary>
internal static class ContentJsonReader
{
    /// <summary>Строгие параметры десериализации для всех файлов контента.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        NumberHandling = JsonNumberHandling.Strict
    };

    /// <summary>
    /// Собирает человекочитаемый путь файла для диагностики.
    /// </summary>
    public static string BuildSourcePath(string rootDirectory, string relativePath)
    {
        string root = rootDirectory.Replace('\\', '/').TrimEnd('/');
        string relative = relativePath.Replace('\\', '/').TrimStart('/');

        return root.Length == 0 ? relative : $"{root}/{relative}";
    }

    /// <summary>
    /// Читает файл, проверяет общий конверт (schemaVersion и id) и десериализует DTO.
    /// </summary>
    /// <typeparam name="T">Тип DTO.</typeparam>
    /// <param name="fileSystem">Абстракция чтения.</param>
    /// <param name="rootDirectory">Корень пака.</param>
    /// <param name="relativePath">Относительный путь файла.</param>
    /// <param name="diagnostics">Коллекция диагностики.</param>
    /// <returns>DTO или null при ошибке.</returns>
    public static T? TryRead<T>(
        IContentFileSystem fileSystem,
        string rootDirectory,
        string relativePath,
        ContentDiagnosticCollection diagnostics)
        where T : class
    {
        TryReadWithRoot<T>(fileSystem, rootDirectory, relativePath, diagnostics, out T? result, out _);
        return result;
    }

    /// <summary>
    /// Читает файл и дополнительно возвращает корневой JSON-объект для проверки
    /// наличия обязательных полей до десериализации.
    /// </summary>
    public static bool TryReadWithRoot<T>(
        IContentFileSystem fileSystem,
        string rootDirectory,
        string relativePath,
        ContentDiagnosticCollection diagnostics,
        out T? value,
        out JsonElement rootElement)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(relativePath);

        value = null;
        rootElement = default;
        string source = BuildSourcePath(rootDirectory, relativePath);
        string text;

        try
        {
            text = fileSystem.ReadAllText(rootDirectory, relativePath);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_FILE_READ",
                $"Не удалось прочитать файл: {exception.Message}",
                source));

            return false;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            });
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                "CONTENT_JSON_PARSE",
                $"Ошибка разбора JSON: {exception.Message}",
                source));

            return false;
        }

        using (document)
        {
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_JSON_OBJECT_REQUIRED",
                    "Корень файла обязан быть JSON-объектом.",
                    source));

                return false;
            }

            if (!TryReadSchemaVersion(root, out int schemaVersion))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_SCHEMA_VERSION_INVALID",
                    $"Поле schemaVersion обязано быть целым числом, ожидается {ContentSchema.SupportedSchemaVersion}.",
                    source,
                    field: "schemaVersion"));

                return false;
            }

            if (schemaVersion != ContentSchema.SupportedSchemaVersion)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_SCHEMA_VERSION_UNSUPPORTED",
                    $"Неподдерживаемая версия схемы: ожидается {ContentSchema.SupportedSchemaVersion}, указано {schemaVersion}.",
                    source,
                    field: "schemaVersion"));

                return false;
            }

            if (!TryReadFileId(root, out string fileId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_FILE_ID_REQUIRED",
                    "Поле id обязательно и не может быть пустым.",
                    source,
                    field: "id"));

                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(root.GetRawText(), Options);
            }
            catch (JsonException exception)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_JSON_SHAPE",
                    $"Файл не соответствует схеме: {exception.Message}",
                    source,
                    fileId));

                return false;
            }

            if (value is null)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    "CONTENT_JSON_EMPTY",
                    "Файл не содержит данных.",
                    source,
                    fileId));

                return false;
            }

            rootElement = root.Clone();
            return true;
        }
    }

    private static bool TryReadSchemaVersion(JsonElement root, out int version)
    {
        version = 0;

        return root.TryGetProperty("schemaVersion", out JsonElement element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out version);
    }

    private static bool TryReadFileId(JsonElement root, out string id)
    {
        id = string.Empty;

        if (!root.TryGetProperty("id", out JsonElement element)
            || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        id = element.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }
}
