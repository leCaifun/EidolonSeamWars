using Game.Core.Content;
using Game.Core.Content.Dto;
using Game.Core.Diagnostics;

namespace Game.Core.Tests;

public class ContentPipelineTests
{
    public static IEnumerable<object[]> GddUnits => new[]
    {
        new object[] { "core.unit.brass.militia", 1, 1, 4, 5, 9, 4, 2, 4, 5, 150, 12, "foot" },
        new object[] { "core.unit.brass.arquebusier", 2, 2, 6, 3, 10, 4, 4, 7, 10, 175, 10, "shooter" },
        new object[] { "core.unit.brass.reitar", 3, 3, 8, 5, 12, 6, 6, 10, 12, 175, 22, "cavalry" },
        new object[] { "core.unit.brass.standard_bearer", 4, 5, 6, 8, 9, 4, 5, 8, 5, 150, 30, "foot" },
        new object[] { "core.unit.brass.brass_golem", 5, 8, 10, 12, 7, 3, 10, 16, 5, 150, 45, "foot" },
        new object[] { "core.unit.ash.bone_servant", 1, 1, 4, 4, 8, 4, 2, 4, 5, 150, 11, "foot" },
        new object[] { "core.unit.ash.corpse_whisperer", 2, 2, 5, 3, 10, 4, 3, 6, 8, 160, 10, "shooter" },
        new object[] { "core.unit.ash.ghoul", 3, 3, 8, 4, 12, 5, 6, 9, 15, 180, 20, "foot" },
        new object[] { "core.unit.ash.bone_knight", 4, 5, 9, 9, 9, 4, 8, 12, 8, 160, 32, "foot" },
        new object[] { "core.unit.ash.ash_avatar", 5, 8, 11, 10, 8, 4, 11, 17, 10, 170, 44, "foot" },
        new object[] { "core.unit.root.pathfinder", 1, 1, 5, 3, 11, 5, 2, 5, 12, 175, 10, "shooter" },
        new object[] { "core.unit.root.wolfdog", 2, 2, 7, 4, 13, 6, 4, 7, 15, 180, 16, "beast" },
        new object[] { "core.unit.root.grove_druid", 3, 3, 5, 5, 10, 4, 4, 8, 8, 160, 18, "shooter" },
        new object[] { "core.unit.root.stone_ent", 4, 5, 9, 11, 6, 3, 9, 14, 5, 150, 40, "foot" },
        new object[] { "core.unit.root.horned_warden", 5, 8, 12, 9, 10, 5, 12, 18, 12, 175, 42, "foot" }
    };

    [Fact]
    public void RealContentLoadsWithoutErrors()
    {
        ContentLoadResult result = RealContent.Load;

        Assert.True(
            result.Pack is not null,
            $"Ошибок: {result.Diagnostics.ErrorCount}");
        Assert.False(result.Diagnostics.HasErrors);
        Assert.Equal("core", result.Pack!.Manifest.Id);
        Assert.Equal(1, result.Pack.Manifest.SchemaVersion);

        // Файлы правил, которых ещё нет в этапе 03 (карта, генерация), осознанно
        // пропускаются загрузчиком с предупреждением. Любая другая диагностика —
        // повод разобраться.
        ContentDiagnostic[] unexpected = result.Diagnostics.Items
            .Where(item => item.Code != "CONTENT_RULE_UNKNOWN"
                || !item.SourceFile.Replace('\\', '/').Contains("/rules/", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(unexpected);
    }

    [Fact]
    public void RealContentHasExpectedEntityCounts()
    {
        ContentPack pack = RealContent.Pack;

        Assert.Equal(3, pack.Factions.Count);
        Assert.Equal(15, pack.Units.Count);
        Assert.Equal(3, pack.HeroClasses.Count);
        Assert.Equal(3, pack.Heroes.Count);
        Assert.Equal(21, pack.Abilities.Count);
        Assert.Equal(3, pack.Spells.Count);
        Assert.Equal(18, pack.Buildings.Count);
        Assert.Equal(3, pack.Settlements.Count);
        Assert.Equal(14, pack.MapObjects.Count);
        Assert.Equal(7, pack.Resources.Count);
        Assert.Equal(26, pack.LocalizationTables.Count);
    }

    [Fact]
    public void RealContentLanguageFilesMatchRegisteredLanguages()
    {
        ContentMergeResult merge = RealContent.MergeWith();
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);
        LocalizationSetFixture.Verify(registry);
    }

    [Fact]
    public void RealContentRequiredLanguagesCoverSameKeys()
    {
        ContentMergeResult merge = RealContent.MergeWith();
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        var localization = Assert.IsType<Game.Core.Content.Localization.LocalizationSet>(registry.Localization);
        var english = Assert.IsType<Game.Core.Content.Localization.LocalizationTable>(localization.Find("en"));
        var russian = Assert.IsType<Game.Core.Content.Localization.LocalizationTable>(localization.Find("ru"));

        Assert.True(english.Count > 0);
        Assert.Equal(english.Count, russian.Count);
        Assert.Equal(english.Entries.Keys.OrderBy(key => key), russian.Entries.Keys.OrderBy(key => key));
    }

    [Theory]
    [MemberData(nameof(GddUnits))]
    public void RealContentUnitsMatchGddTables(
        string id,
        int tier,
        int leadershipCost,
        int attack,
        int defense,
        int initiative,
        int speed,
        int damageMin,
        int damageMax,
        int critChancePercent,
        int critDamagePercent,
        int hitPoints,
        string movementType)
    {
        UnitDto? unit = RealContent.Pack.Units
            .Select(entry => entry.Value)
            .SingleOrDefault(item => item.Id == id);

        Assert.NotNull(unit);
        Assert.Equal(tier, unit!.Tier);
        Assert.Equal(leadershipCost, unit.LeadershipCost);
        Assert.Equal(attack, unit.Attack);
        Assert.Equal(defense, unit.Defense);
        Assert.Equal(initiative, unit.Initiative);
        Assert.Equal(speed, unit.Speed);
        Assert.Equal(damageMin, unit.DamageMin);
        Assert.Equal(damageMax, unit.DamageMax);
        Assert.Equal(critChancePercent, unit.CritChancePercent);
        Assert.Equal(critDamagePercent, unit.CritDamagePercent);
        Assert.Equal(hitPoints, unit.HitPoints);
        Assert.Equal(movementType, unit.MovementType);
    }

    [Fact]
    public void PlansPacksByLoadOrderThenId()
    {
        ContentPack alpha = LoadManifestOnly("alpha", loadOrder: 5);
        ContentPack beta = LoadManifestOnly("beta", loadOrder: 1);
        ContentPack zeta = LoadManifestOnly("zeta", loadOrder: 5);
        ContentDiagnosticCollection diagnostics = new();

        IReadOnlyList<ContentPack> ordered = ContentMerge.PlanOrder(
            new[] { zeta, alpha, beta },
            diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(new[] { "beta", "alpha", "zeta" }, ordered.Select(pack => pack.Manifest.Id));
    }

    [Fact]
    public void PlansDependenciesBeforeDependents()
    {
        ContentPack child = LoadManifestOnly("child", loadOrder: 0, dependencies: new[] { "base" });
        ContentPack basePack = LoadManifestOnly("base", loadOrder: 100);
        ContentDiagnosticCollection diagnostics = new();

        IReadOnlyList<ContentPack> ordered = ContentMerge.PlanOrder(
            new[] { child, basePack },
            diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(new[] { "base", "child" }, ordered.Select(pack => pack.Manifest.Id));
    }

    [Fact]
    public void RejectsCyclicDependencies()
    {
        ContentPack first = LoadManifestOnly("first", dependencies: new[] { "second" });
        ContentPack second = LoadManifestOnly("second", dependencies: new[] { "first" });
        ContentMergeResult result = new ContentMerge().Merge(new[] { first, second });

        Assert.Null(result.Registry);
        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "CONTENT_DEPENDENCY_CYCLE"
                && item.Severity == ContentDiagnosticSeverity.Error);
    }

    [Fact]
    public void RejectsDuplicateIdInsidePack()
    {
        string unit = TestPacks.Unit("dup.unit.soldier");
        InMemoryContentFileSystem fileSystem = TestPacks.Pack(
            "dup",
            files: ("units/duplicates.json", TestPacks.UnitsFile("dup.units", unit, unit)));

        ContentLoadResult result = new ContentLoader(fileSystem).Load("mem://dup");

        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "CONTENT_ID_DUPLICATE"
                && item.EntityId == "dup.unit.soldier"
                && item.Severity == ContentDiagnosticSeverity.Error);
    }

    [Fact]
    public void AppliesLastWinsOverrideAndLogsIt()
    {
        string id = "core.unit.brass.militia";
        ContentPack first = LoadPack("overlay-a", TestPacks.Pack(
            "overlay-a",
            loadOrder: 10,
            files: ("units/militia.json", TestPacks.UnitsFile(
                "overlay-a.units",
                TestPacks.Unit(id, attack: "90", nameKey: "unit.brass.militia.name",
                    descriptionKey: "unit.brass.militia.desc")))));
        ContentPack second = LoadPack("overlay-b", TestPacks.Pack(
            "overlay-b",
            loadOrder: 20,
            files: ("units/militia.json", TestPacks.UnitsFile(
                "overlay-b.units",
                TestPacks.Unit(id, attack: "100", nameKey: "unit.brass.militia.name",
                    descriptionKey: "unit.brass.militia.desc")))));
        TestLogger logger = new();

        ContentMergeResult result = new ContentMerge(logger).Merge(
            new[] { RealContent.Pack, first, second });

        ContentRegistry registry = Assert.IsType<ContentRegistry>(result.Registry);
        UnitDto? unit = registry.FindUnit(id);
        Assert.NotNull(unit);
        Assert.Equal(100, unit!.Attack);
        Assert.Equal(
            new[] { "core", "overlay-a", "overlay-b" },
            registry.Manifests.Select(manifest => manifest.Id));

        ContentChange[] changes = registry.Changes
            .Where(item => item.Category == "unit"
                && item.EntityId == id
                && item.Kind == ContentChangeKind.Replace)
            .ToArray();
        Assert.Equal(2, changes.Length);
        ContentChange lastChange = changes[^1];
        Assert.Equal("overlay-b", lastChange.SourcePack);
        Assert.Equal("overlay-a", lastChange.ReplacedPack);
        Assert.Contains(
            logger.Events,
            item => item.Category == "Content"
                && item.Message.Contains("Переопределение", StringComparison.Ordinal)
                && item.Message.Contains(id, StringComparison.Ordinal));
    }

    [Fact]
    public void SeventhUnitCanBeAddedByJsonOnly()
    {
        const string id = "core.unit.brass.seventh";
        ContentPack overlay = LoadPack("seventh", TestPacks.Pack(
            "seventh",
            10,
            null,
            (
                "units/extra.json",
                TestPacks.UnitsFile("seventh.units", TestPacks.Unit(id))
            ),
            (
                "localization/en.json",
                TestPacks.LocalizationFile(
                    "en",
                    "complete",
                    new Dictionary<string, string>
                    {
                        ["core.unit.brass.seventh.name"] = "Seventh",
                        ["core.unit.brass.seventh.desc"] = "Added by JSON only."
                    })
            ),
            (
                "localization/ru.json",
                TestPacks.LocalizationFile(
                    "ru",
                    "complete",
                    new Dictionary<string, string>
                    {
                        ["core.unit.brass.seventh.name"] = "Седьмой",
                        ["core.unit.brass.seventh.desc"] = "Добавлен только через JSON."
                    })
            )));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult validation = new ContentValidator().Validate(registry);

        Assert.False(validation.Diagnostics.HasErrors);
        Assert.NotNull(registry.FindUnit(id));
        Assert.Equal(16, registry.Units.Count);
        Assert.Contains(
            registry.Changes,
            item => item.Category == "unit"
                && item.EntityId == id
                && item.Kind == ContentChangeKind.Add);
        int baselineEnglishKeys = RealContent.Pack.LocalizationTables
            .Single(table => table.Language == "en")
            .Count;
        Assert.Equal(baselineEnglishKeys + 2, registry.Localization!.Find("en")!.Count);
    }

    private static ContentPack LoadManifestOnly(
        string id,
        int loadOrder = 0,
        string[]? dependencies = null)
    {
        return RealContent.LoadPack(
            TestPacks.Pack(id, loadOrder, dependencies),
            $"mem://{id}");
    }

    private static ContentPack LoadPack(string id, InMemoryContentFileSystem fileSystem)
    {
        ContentLoadResult result = new ContentLoader(fileSystem).Load($"mem://{id}");

        Assert.NotNull(result.Pack);
        return result.Pack!;
    }

    /// <summary>
    /// Проверки соответствия languages.json набору языковых файлов.
    /// </summary>
    private static class LocalizationSetFixture
    {
        public static void Verify(ContentRegistry registry)
        {
            var localization = Assert.IsType<Game.Core.Content.Localization.LocalizationSet>(
                registry.Localization);
            string[] registered = localization.Manifest.Languages
                .Select(language => language.Code)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] files = localization.Tables.Keys
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(registered, files);
            Assert.Equal(26, registered.Length);
            Assert.Equal(new[] { "en", "ru" }, localization.Manifest.RequiredLanguages.OrderBy(code => code));
            Assert.Equal("ru", localization.Manifest.DefaultLanguage);
            Assert.Equal("en", localization.Manifest.FallbackLanguage);

            var english = Assert.IsType<Game.Core.Content.Localization.LocalizationTable>(
                localization.Find("en"));
            HashSet<string> englishKeys = new(english.Entries.Keys, StringComparer.Ordinal);
            Assert.True(english.Count > 0);

            foreach (string code in registered)
            {
                var table = Assert.IsType<Game.Core.Content.Localization.LocalizationTable>(
                    localization.Find(code));
                Assert.Equal(code, table.Language);
                Assert.True(table.Count > 0);

                // Лишние ключи запрещены; пропуски допустимы только вне ru/en (ADR-026).
                Assert.All(table.Entries.Keys, key => Assert.Contains(key, englishKeys));

                if (code is "ru" or "en")
                {
                    Assert.Equal(englishKeys.Count, table.Count);
                    Assert.True(string.IsNullOrWhiteSpace(table.Fallback));
                }
                else
                {
                    Assert.False(string.IsNullOrWhiteSpace(table.Fallback));
                }
            }
        }
    }
}
