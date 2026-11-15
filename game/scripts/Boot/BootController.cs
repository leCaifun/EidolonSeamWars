using Godot;
using Godot.Collections;
using Game.Core.Content;
using Game.Core.Diagnostics;

/// <summary>
/// Стартовый узел Godot-слоя. Не содержит игровой логики:
/// выводит версию проекта и фактическую версию движка в лог,
/// загружает и валидирует контент, а при ошибке завершает работу.
/// </summary>
public partial class BootController : Node
{
	/// <summary>
	/// Ожидаемая версия Godot, зафиксированная ADR-023.
	/// Константа живёт в презентационном слое: Game.Core о движке не знает.
	/// </summary>
	public const string ExpectedGodotVersion = "4.7.2";

	/// <summary>
	/// Корень базового контентного пака в ресурсах Godot.
	/// </summary>
	public const string ContentRoot = "res://content/core";

	/// <summary>
	/// Вызывается при входе узла в дерево сцены.
	/// Версия проекта читается из настроек, версия движка — из Engine.GetVersionInfo().
	/// После проверки версии загружается и валидируется контент.
	/// </summary>
	public override void _Ready()
	{
		LogVersions();

		if (!TryLoadContent())
		{
			GD.PushError("[Boot] Контент не прошёл загрузку или валидацию. Запуск остановлен.");
			GetTree().Quit(1);
		}
	}

	private void LogVersions()
	{
		string projectVersion = ProjectSettings
			.GetSetting("application/config/version")
			.AsString();

		if (string.IsNullOrWhiteSpace(projectVersion))
		{
			projectVersion = "unknown";
			GD.PushWarning("application/config/version не задан в project.godot");
		}

		string engineVersion = ReadEngineVersion();
		GD.Print($"[Boot] Eidolon: Seam Wars v{projectVersion}");
		GD.Print($"[Boot] Godot {engineVersion} (.NET)");

		if (engineVersion != ExpectedGodotVersion)
		{
			GD.PushWarning(
				$"[Boot] Ожидался Godot {ExpectedGodotVersion} (ADR-023), " +
				$"фактически {engineVersion}. Проверьте docs/TOOLCHAIN.md.");
		}
	}

	/// <summary>
	/// Загружает, сливает и валидирует базовый пак. Возвращает <c>false</c>,
	/// если контент непригоден для запуска; диагностика уже записана в журнал.
	/// </summary>
	private static bool TryLoadContent()
	{
		GodotGameLogger logger = new();
		GodotContentFileSystem fileSystem = new();

		try
		{
			ContentLoadResult load = new ContentLoader(fileSystem, logger).Load(ContentRoot);

			if (load.Pack is null || load.Diagnostics.HasErrors)
			{
				GD.PushError(
					$"[Boot] Пак {ContentRoot} не загружен: ошибок {load.Diagnostics.ErrorCount}.");
				return false;
			}

			LogWarnings(load.Diagnostics, "Загрузка контента");

			ContentMergeResult merge = new ContentMerge(logger).Merge(new[] { load.Pack });

			if (merge.Registry is null || merge.Diagnostics.HasErrors)
			{
				GD.PushError(
					$"[Boot] Merge контента не выполнен: ошибок {merge.Diagnostics.ErrorCount}.");
				return false;
			}

			LogWarnings(merge.Diagnostics, "Merge контента");

			ContentRegistry registry = merge.Registry;
			ContentValidationResult validation = new ContentValidator(logger).Validate(registry);

			if (validation.Diagnostics.HasErrors)
			{
				GD.PushError(
					$"[Boot] Валидация контента не пройдена: ошибок {validation.Diagnostics.ErrorCount}.");
				return false;
			}

			LogWarnings(validation.Diagnostics, "Валидация контента");

			int warningCount = load.Diagnostics.WarningCount
				+ merge.Diagnostics.WarningCount
				+ validation.Diagnostics.WarningCount;
			LogContentSummary(registry, warningCount);
			return true;
		}
		catch (InvalidOperationException exception)
		{
			GD.PushError($"[Boot] Не удалось прочитать контент: {exception.Message}");
			return false;
		}
	}

	private static void LogWarnings(ContentDiagnosticCollection diagnostics, string stage)
	{
		foreach (ContentDiagnostic diagnostic in diagnostics.Items)
		{
			if (diagnostic.Severity == ContentDiagnosticSeverity.Warning)
			{
				GD.PushWarning($"[Boot] {stage}: {diagnostic}");
			}
		}
	}

	private static void LogContentSummary(ContentRegistry registry, int warningCount)
	{
		int languageCount = registry.Localization?.Count ?? 0;
		int russianKeys = registry.Localization?.Find("ru")?.Count ?? 0;
		int englishKeys = registry.Localization?.Find("en")?.Count ?? 0;

		GD.Print(
			$"[Boot] Контент: паков {registry.PackCount}, фракций {registry.Factions.Count}, " +
			$"юнитов {registry.Units.Count}, классов героев {registry.HeroClasses.Count}, " +
			$"героев {registry.Heroes.Count}, способностей {registry.Abilities.Count}, " +
			$"заклинаний {registry.Spells.Count}, зданий {registry.Buildings.Count}, " +
			$"поселений {registry.Settlements.Count}, объектов карты {registry.MapObjects.Count}, " +
			$"ресурсов {registry.Resources.Count}.");
		GD.Print(
			$"[Boot] Локализация: языков {languageCount}, ключей ru {russianKeys}, en {englishKeys}.");

		if (warningCount > 0)
		{
			GD.PushWarning($"[Boot] Контент: предупреждений {warningCount}.");
		}
	}

	/// <summary>
	/// Собирает версию движка из major/minor/patch. При отсутствии полей
	/// возвращает строку из поля string либо "unknown".
	/// </summary>
	private static string ReadEngineVersion()
	{
		Dictionary info = Engine.GetVersionInfo();

		if (info.TryGetValue("major", out Variant major)
			&& info.TryGetValue("minor", out Variant minor)
			&& info.TryGetValue("patch", out Variant patch))
		{
			return $"{major.AsInt32()}.{minor.AsInt32()}.{patch.AsInt32()}";
		}

		if (info.TryGetValue("string", out Variant fullVersion))
		{
			return fullVersion.AsString();
		}

		return "unknown";
	}
}
