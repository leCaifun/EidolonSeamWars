using Game.Core.Content;
using Game.Core.Content.Localization;
using Game.Core.Diagnostics;

namespace Game.Core.Tests;

public class ContentValidatorTests
{
    [Fact]
    public void RealContentPassesValidationWithoutWarnings()
    {
        ContentMergeResult merge = RealContent.MergeWith();
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Equal(0, result.Diagnostics.ErrorCount);
        Assert.Equal(0, result.Diagnostics.WarningCount);
        Assert.Equal(registry.EntityCount, result.EntityCount);
    }

    [Fact]
    public void RejectsBrokenUnitFactionReference()
    {
        ContentPack overlay = LoadOverlay(
            "broken-ref",
            ("units/broken.json", TestPacks.UnitsFile(
                "broken.units",
                TestPacks.Unit(
                    "test.unit.broken",
                    factionId: "core.faction.unknown",
                    nameKey: "unit.brass.militia.name",
                    descriptionKey: "unit.brass.militia.desc"))));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        ContentDiagnostic diagnostic = Assert.Single(
            result.Diagnostics.Items,
            item => item.Code == "CONTENT_REF_FACTION_MISSING");
        Assert.Equal(ContentDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("test.unit.broken", diagnostic.EntityId);
        Assert.Equal("factionId", diagnostic.Field);
    }

    [Fact]
    public void RejectsMissingRequiredLocalizationKey()
    {
        ContentPack overlay = LoadOverlay(
            "missing-key",
            ("units/keyless.json", TestPacks.UnitsFile(
                "keyless.units",
                TestPacks.Unit("test.unit.keyless"))));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "LOC_KEY_MISSING_EN"
                && item.EntityId == "test.unit.keyless.name"
                && item.Severity == ContentDiagnosticSeverity.Error);
        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "LOC_KEY_MISSING_RU"
                && item.EntityId == "test.unit.keyless.name"
                && item.Severity == ContentDiagnosticSeverity.Error);
    }

    [Fact]
    public void WarnsAboutMissingKeyInDraftLanguage()
    {
        ContentPack overlay = LoadOverlay(
            "draft-gap",
            ("units/draft.json", TestPacks.UnitsFile(
                "draft.units",
                TestPacks.Unit("test.unit.draft"))),
            ("localization/en.json", TestPacks.LocalizationFile(
                "en",
                "complete",
                new Dictionary<string, string>
                {
                    ["test.unit.draft.name"] = "Draft",
                    ["test.unit.draft.desc"] = "Draft description."
                })),
            ("localization/ru.json", TestPacks.LocalizationFile(
                "ru",
                "complete",
                new Dictionary<string, string>
                {
                    ["test.unit.draft.name"] = "Черновой",
                    ["test.unit.draft.desc"] = "Черновое описание."
                })));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.False(result.Diagnostics.HasErrors);
        ContentDiagnostic warning = Assert.Single(
            result.Diagnostics.Items,
            item => item.Code == "LOC_OPTIONAL_KEYS_MISSING" && item.EntityId == "hr");
        Assert.Equal(ContentDiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WarnsWhenLanguageStatusDisagreesWithManifest()
    {
        ContentPack overlay = LoadOverlay(
            "status-mismatch",
            ("localization/hr.json", TestPacks.LocalizationFile(
                "hr",
                "experimental",
                new Dictionary<string, string>
                {
                    ["unit.brass.militia.name"] = "Milicija"
                })));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "LOC_STATUS_MISMATCH"
                && item.EntityId == "hr"
                && item.Severity == ContentDiagnosticSeverity.Warning);
    }

    [Fact]
    public void RejectsFractionalNumber()
    {
        InMemoryContentFileSystem fileSystem = TestPacks.Pack(
            "fraction",
            files: ("units/fraction.json", TestPacks.UnitsFile(
                "fraction.units",
                TestPacks.Unit("test.unit.fraction", attack: "1.5"))));

        ContentLoadResult result = new ContentLoader(fileSystem).Load("mem://fraction");

        Assert.True(result.Diagnostics.HasErrors);
        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Severity == ContentDiagnosticSeverity.Error
                && item.Code == "CONTENT_JSON_SHAPE");
        Assert.Empty(result.Pack!.Units);
    }

    [Fact]
    public void RejectsUnknownField()
    {
        InMemoryContentFileSystem fileSystem = TestPacks.Pack(
            "unknown-field",
            files: ("units/unknown.json", TestPacks.UnitsFile(
                "unknown.units",
                TestPacks.Unit(
                    "test.unit.unknown",
                    extraMembers: ",\n          \"mystery\": 1"))));

        ContentLoadResult result = new ContentLoader(fileSystem).Load("mem://unknown-field");

        Assert.True(result.Diagnostics.HasErrors);
        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Severity == ContentDiagnosticSeverity.Error
                && item.Code == "CONTENT_JSON_SHAPE");
        Assert.Empty(result.Pack!.Units);
    }

    [Fact]
    public void RejectsUnitTierOutsideRange()
    {
        ContentPack overlay = LoadOverlay(
            "tier",
            ("units/tier.json", TestPacks.UnitsFile(
                "tier.units",
                TestPacks.Unit(
                    "test.unit.tier",
                    nameKey: "unit.brass.militia.name",
                    descriptionKey: "unit.brass.militia.desc")
                    .Replace("\"tier\": 1", "\"tier\": 9", StringComparison.Ordinal))));
        ContentMergeResult merge = RealContent.MergeWith(overlay);
        ContentRegistry registry = Assert.IsType<ContentRegistry>(merge.Registry);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(
            result.Diagnostics.Items,
            item => item.Code == "CONTENT_UNIT_TIER"
                && item.EntityId == "test.unit.tier"
                && item.Severity == ContentDiagnosticSeverity.Error);
    }

    [Fact]
    public void ResolvesRealContentTranslations()
    {
        LocalizationSet localization = Assert.IsType<LocalizationSet>(
            RealContent.MergeWith().Registry!.Localization);
        LocalizationResolver resolver = new(localization);

        string russian = resolver.Resolve("ru", "unit.brass.militia.name");
        string german = resolver.Resolve("de", "unit.brass.militia.name");

        Assert.NotEqual("unit.brass.militia.name", russian);
        Assert.NotEqual("unit.brass.militia.name", german);
        Assert.False(string.IsNullOrWhiteSpace(russian));
        Assert.False(string.IsNullOrWhiteSpace(german));
    }

    [Fact]
    public void MissingKeyReturnsKeyAndLogsWarning()
    {
        LocalizationSet localization = Assert.IsType<LocalizationSet>(
            RealContent.MergeWith().Registry!.Localization);
        TestLogger logger = new();
        LocalizationResolver resolver = new(localization, logger);

        string resolved = resolver.Resolve("ru", "no.such.key");

        Assert.Equal("no.such.key", resolved);
        LogEvent entry = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Loc", entry.Category);
        Assert.Equal("no.such.key", entry.Fields["key"]);
        Assert.Equal("ru", entry.Fields["language"]);
    }

    [Fact]
    public void UsesLanguageFallbackBeforeGlobalFallback()
    {
        LocalizationManifest manifest = new(
            "ru",
            "en",
            new[] { "ru", "en" },
            new[]
            {
                new LanguageDefinition("de", "language.de.name", "Latin", LocalizationStatus.Draft, true, "en", false),
                new LanguageDefinition("en", "language.en.name", "Latin", LocalizationStatus.Complete, true, null, false)
            });
        LocalizationTable german = new(
            "de",
            LocalizationStatus.Draft,
            "en",
            new Dictionary<string, string> { ["a"] = "de-a" },
            "mem://de.json");
        LocalizationTable english = new(
            "en",
            LocalizationStatus.Complete,
            null,
            new Dictionary<string, string> { ["b"] = "en-b" },
            "mem://en.json");
        LocalizationResolver resolver = new(new LocalizationSet(manifest, new[] { german, english }));

        Assert.Equal("de-a", resolver.Resolve("de", "a"));
        Assert.Equal("en-b", resolver.Resolve("de", "b"));
        Assert.Equal("missing", resolver.Resolve("de", "missing"));
    }

    private static ContentPack LoadOverlay(
        string id,
        params (string Path, string Json)[] files)
    {
        return RealContent.LoadPack(TestPacks.Pack(id, loadOrder: 10, files: files), $"mem://{id}");
    }
}
