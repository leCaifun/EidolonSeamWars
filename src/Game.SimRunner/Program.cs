using System.Reflection;
using System.Text;
using Game.Core.Content;
using Game.Core.Diagnostics;
using Game.SimRunner;

const string ProductName = "Eidolon: Seam Wars";
const int ExitSuccess = 0;
const int ExitValidationFailed = 1;
const int ExitUnknownCommand = 2;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

string version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? "0.3.0";

if (args.Length == 0 || IsOneOf(args[0], "help", "--help", "-h"))
{
    PrintUsage(version);
    return ExitSuccess;
}

if (IsOneOf(args[0], "version", "--version", "-v"))
{
    Console.WriteLine($"{ProductName} SimRunner {version}");
    return ExitSuccess;
}

if (IsOneOf(args[0], "validate"))
{
    return RunValidate(args);
}

Console.Error.WriteLine($"Неизвестная команда: {args[0]}");
Console.Error.WriteLine("Валидатор контента — команда validate; симуляция боя появится на этапе 05/08.");
Console.Error.WriteLine("См. docs/ROADMAP.md и docs/RUNBOOK.md.");
Console.Error.WriteLine();
PrintUsage(version);
return ExitUnknownCommand;

static int RunValidate(string[] args)
{
    string? contentPath = null;

    for (int index = 1; index < args.Length; index++)
    {
        switch (args[index])
        {
            case "--content":
                if (index + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Аргумент --content требует путь к каталогу пака.");
                    return ExitUnknownCommand;
                }

                contentPath = args[index + 1];
                index++;
                break;
            default:
                Console.Error.WriteLine($"Неизвестный аргумент команды validate: {args[index]}");
                return ExitUnknownCommand;
        }
    }

    string directory;

    try
    {
        directory = ContentPathResolver.Resolve(contentPath, AppContext.BaseDirectory);
    }
    catch (InvalidOperationException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return ExitValidationFailed;
    }

    ConsoleGameLogger logger = new();
    DiskContentFileSystem fileSystem = new();
    ContentLoader loader = new(fileSystem, logger);
    ContentDiagnosticCollection diagnostics = new();

    Console.WriteLine($"{ProductName} — валидация контента");
    Console.WriteLine($"Каталог: {directory}");
    Console.WriteLine();

    ContentLoadResult loadResult = loader.Load(directory);
    diagnostics.AddRange(loadResult.Diagnostics.Items);

    if (loadResult.Pack is null)
    {
        PrintDiagnostics(diagnostics);
        Console.WriteLine();
        Console.WriteLine("ИТОГ: пак не загружен.");
        return ExitValidationFailed;
    }

    ContentPack pack = loadResult.Pack;
    ContentMerge merge = new(logger);
    ContentMergeResult mergeResult = merge.Merge(new[] { pack });
    diagnostics.AddRange(mergeResult.Diagnostics.Items);

    if (mergeResult.Registry is null)
    {
        PrintPackSummary(pack);
        PrintDiagnostics(diagnostics);
        Console.WriteLine();
        Console.WriteLine("ИТОГ: merge не выполнен.");
        return ExitValidationFailed;
    }

    ContentRegistry registry = mergeResult.Registry;
    ContentValidator validator = new(logger);
    ContentValidationResult validation = validator.Validate(registry);
    diagnostics.AddRange(validation.Diagnostics.Items);

    PrintPackSummary(pack);
    PrintRegistrySummary(registry);
    PrintDiagnostics(diagnostics);

    Console.WriteLine();

    if (diagnostics.HasErrors)
    {
        Console.WriteLine(
            $"ИТОГ: ошибок {diagnostics.ErrorCount}, предупреждений {diagnostics.WarningCount}. Контент невалиден.");
        return ExitValidationFailed;
    }

    Console.WriteLine($"ИТОГ: контент валиден, предупреждений {diagnostics.WarningCount}.");
    return ExitSuccess;
}

static void PrintPackSummary(ContentPack pack)
{
    Console.WriteLine(
        $"Пак: {pack.Manifest.Id} {pack.Manifest.Version} (schema {pack.Manifest.SchemaVersion}, " +
        $"диапазон {pack.Manifest.GameVersionRange}, loadOrder {pack.Manifest.LoadOrder})");
}

static void PrintRegistrySummary(ContentRegistry registry)
{
    Console.WriteLine(
        $"Сущности: {registry.EntityCount} " +
        $"(фракций {registry.Factions.Count}, юнитов {registry.Units.Count}, " +
        $"классов героев {registry.HeroClasses.Count}, героев {registry.Heroes.Count}, " +
        $"способностей {registry.Abilities.Count}, заклинаний {registry.Spells.Count}, " +
        $"зданий {registry.Buildings.Count}, поселений {registry.Settlements.Count}, " +
        $"объектов карты {registry.MapObjects.Count}, ресурсов {registry.Resources.Count})");

    int changes = registry.Changes.Count;
    Console.WriteLine($"Merge: записей {changes}, паков {registry.PackCount}");

    if (registry.Localization is not { } localization)
    {
        Console.WriteLine("Локализация: не загружена");
        return;
    }

    int englishKeys = localization.Find("en")?.Count ?? 0;
    int russianKeys = localization.Find("ru")?.Count ?? 0;
    Console.WriteLine(
        $"Локализация: языков {localization.Count}, ключей en {englishKeys}, ru {russianKeys}");
}

static void PrintDiagnostics(ContentDiagnosticCollection diagnostics)
{
    if (diagnostics.Items.Count == 0)
    {
        Console.WriteLine("Сообщений нет.");
        return;
    }

    foreach (ContentDiagnostic diagnostic in diagnostics.Items)
    {
        if (diagnostic.Severity == ContentDiagnosticSeverity.Error)
        {
            Console.WriteLine(diagnostic.ToString());
        }
    }

    foreach (ContentDiagnostic diagnostic in diagnostics.Items)
    {
        if (diagnostic.Severity == ContentDiagnosticSeverity.Warning)
        {
            Console.WriteLine(diagnostic.ToString());
        }
    }
}

static void PrintUsage(string version)
{
    Console.WriteLine($"{ProductName} SimRunner {version}");
    Console.WriteLine();
    Console.WriteLine("Headless-инструменты. Бой и карта появятся на этапах 04–08.");
    Console.WriteLine();
    Console.WriteLine("Команды:");
    Console.WriteLine("  help, --help, -h          показать эту справку");
    Console.WriteLine("  version, -v               показать версию");
    Console.WriteLine("  validate [--content PATH] загрузить, слить и проверить контент");
    Console.WriteLine();
    Console.WriteLine("Код возврата validate: 0 — контент валиден, 1 — есть ошибки.");
}

static bool IsOneOf(string value, params string[] candidates)
{
    foreach (string candidate in candidates)
    {
        if (string.Equals(value, candidate, StringComparison.Ordinal))
        {
            return true;
        }
    }

    return false;
}
