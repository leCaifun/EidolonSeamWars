using Game.Core.Domain;
using Godot;

/// <summary>
/// Один гекс карты: спрайт биома в центре гекса. Логики не содержит.
/// </summary>
public partial class HexTileNode : Node2D
{
	/// <summary>Координата гекса в Core.</summary>
	public Hex Coordinate { get; private set; }

	/// <summary>
	/// Задаёт гекс и текстуру. Вызывается после добавления узла в дерево или до него.
	/// </summary>
	/// <param name="coordinate">Координата гекса.</param>
	/// <param name="texture">Текстура биома; может быть <c>null</c> (тогда гекс пуст).</param>
	public void Setup(Hex coordinate, Texture2D? texture)
	{
		Coordinate = coordinate;
		Position = HexMetrics.ToPixel(coordinate);

		AddChild(new Sprite2D
		{
			Texture = texture,
			Centered = true
		});
	}
}
