using Game.Core.Map;
using Godot;

/// <summary>
/// Рисует сетку карты: по одному HexTileNode на гекс. Текстуры берутся по
/// <c>tileId</c> биома из <c>res://assets/sprites/map/{tileId}.svg</c>.
/// </summary>
public partial class HexGridVisual : Node2D
{
	private const string TileScenePath = "res://scenes/map/HexTile.tscn";
	private const string TileTextureFolder = "res://assets/sprites/map/";

	private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);

	/// <summary>
	/// Создаёт узлы для всех гексов карты; прежние узлы удаляются.
	/// </summary>
	/// <param name="map">Карта Core.</param>
	public void Build(GameMap map)
	{
		foreach (Node child in GetChildren())
		{
			child.QueueFree();
		}

		PackedScene? scene = GD.Load<PackedScene>(TileScenePath);

		foreach (Game.Core.Domain.Hex hex in map.Grid.AllHexes())
		{
			Texture2D? texture = LoadTexture(map.GetBiome(hex).TileId);
			HexTileNode? tile = scene?.Instantiate<HexTileNode>() ?? new HexTileNode();
			tile.Setup(hex, texture);
			AddChild(tile);
		}
	}

	private Texture2D? LoadTexture(string tileId)
	{
		if (_textures.TryGetValue(tileId, out Texture2D? cached))
		{
			return cached;
		}

		string path = $"{TileTextureFolder}{tileId}.svg";
		Texture2D? texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

		if (texture is null)
		{
			GD.PushWarning($"[Map] Нет текстуры гекса: {path}");
		}

		_textures[tileId] = texture;
		return texture;
	}
}
