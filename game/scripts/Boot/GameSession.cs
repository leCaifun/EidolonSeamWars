using Game.Core.Content;
using Game.Core.Content.Localization;
using Game.Core.Diagnostics;

/// <summary>
/// Сессия презентационного слоя: хранит загруженный контент и язык интерфейса.
/// Заполняется BootController после успешной загрузки; игровой логики не содержит.
/// </summary>
public static class GameSession
{
	/// <summary>Язык интерфейса по умолчанию.</summary>
	public const string DefaultLanguage = "ru";

	private static LocalizationResolver? _resolver;

	/// <summary>Загруженный и провалидированный реестр контента либо <c>null</c> до загрузки.</summary>
	public static ContentRegistry? Registry { get; private set; }

	/// <summary>Текущий язык интерфейса.</summary>
	public static string Language { get; set; } = DefaultLanguage;

	/// <summary>
	/// Сохраняет реестр контента и создаёт резолвер локализации.
	/// </summary>
	/// <param name="registry">Готовый реестр контента.</param>
	/// <param name="logger">Логгер резолвера; может быть <c>null</c>.</param>
	public static void Initialize(ContentRegistry registry, IGameLogger? logger)
	{
		Registry = registry;
		_resolver = registry.Localization is null
			? null
			: new LocalizationResolver(registry.Localization, logger);
	}

	/// <summary>
	/// Возвращает локализованный текст для интерфейса; при отсутствии ключа — «[ключ]».
	/// </summary>
	/// <param name="key">Ключ локализации.</param>
	public static string Text(string key)
	{
		return _resolver is null ? $"[{key}]" : _resolver.ResolveForUi(Language, key);
	}

	/// <summary>
	/// Возвращает локализованный текст и подставляет именованные плейсхолдеры вида <c>{name}</c>.
	/// </summary>
	/// <param name="key">Ключ локализации.</param>
	/// <param name="name">Имя плейсхолдера без скобок.</param>
	/// <param name="value">Подставляемое значение.</param>
	public static string Text(string key, string name, string value)
	{
		return Text(key).Replace("{" + name + "}", value, StringComparison.Ordinal);
	}
}
