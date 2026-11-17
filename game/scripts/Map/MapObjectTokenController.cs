using Game.Core.Map;
using Godot;

/// <summary>
/// Токен объекта карты (шахта, жилище, поселение, схрон, патруль).
/// Позицию и видимость задаёт AdventureMapController по состоянию Core.
/// </summary>
public partial class MapObjectTokenController : Node2D
{
	/// <summary>Экземпляр объекта в Core.</summary>
	public MapObjectInstance? Subject { get; private set; }

	/// <summary>
	/// Привязывает токен к объекту и ставит его в центр гекса.
	/// </summary>
	/// <param name="instance">Экземпляр объекта карты.</param>
	/// <param name="texture">Текстура токена; может быть <c>null</c>.</param>
	public void Bind(MapObjectInstance instance, Texture2D? texture)
	{
		Subject = instance;
		Position = HexMetrics.ToPixel(instance.Position);

		AddChild(new Sprite2D
		{
			Texture = texture,
			Centered = true,
			Scale = new Vector2(0.8f, 0.8f)
		});
	}

	/// <summary>Показывает или скрывает токен.</summary>
	/// <param name="isShown">Нужно ли показывать токен.</param>
	public void SetShown(bool isShown)
	{
		Visible = isShown;
	}
}
