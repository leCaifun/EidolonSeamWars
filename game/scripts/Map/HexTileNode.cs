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

		if (texture != null)
		{
			var sprite = new Sprite2D
			{
				Texture = texture,
				Centered = true
			};

			// Масштабируем текстуру под размер гекса
			// Для pointy-top: ширина = Size * √3, высота = Size * 2
			float targetWidth = HexMetrics.Size * Mathf.Sqrt(3f);
			float targetHeight = HexMetrics.Size * 2f;
			
			// Получаем реальный размер текстуры
			Vector2 textureSize = texture.GetSize();
			
			sprite.Scale = new Vector2(
				targetWidth / textureSize.X,
				targetHeight / textureSize.Y
			);

			AddChild(sprite);
		}
	}
}