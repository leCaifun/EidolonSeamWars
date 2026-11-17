using Game.Core.Domain;
using Game.Core.Map;
using Godot;

/// <summary>
/// Рисует туман войны одним проходом _Draw: скрытые гексы — почти непрозрачно,
/// исследованные — полупрозрачно, видимые — без заливки. Состояние берётся из Core.
/// </summary>
public partial class FogOfWarVisual : Node2D
{
	private static readonly Color HiddenColor = new(0.03f, 0.05f, 0.08f, 1f);
	private static readonly Color ExploredColor = new(0.03f, 0.05f, 0.08f, 0.55f);

	private readonly Vector2[] _corners = HexMetrics.Corners(1.03f);
	private GameMap? _map;
	private FogOfWar? _fog;
	private string _factionId = string.Empty;

	/// <summary>
	/// Привязывает визуал к карте и туману фракции игрока.
	/// </summary>
	/// <param name="map">Карта Core.</param>
	/// <param name="fog">Состояние тумана.</param>
	/// <param name="factionId">Фракция, глазами которой рисуется туман.</param>
	public void Bind(GameMap map, FogOfWar fog, string factionId)
	{
		_map = map;
		_fog = fog;
		_factionId = factionId;
		QueueRedraw();
	}

	/// <summary>Перерисовывает туман после изменения состояния Core.</summary>
	public void Refresh()
	{
		QueueRedraw();
	}

	/// <inheritdoc />
	public override void _Draw()
	{
		if (_map is null || _fog is null)
		{
			return;
		}

		Vector2[] points = new Vector2[6];

		foreach (Hex hex in _map.Grid.AllHexes())
		{
			FogState state = _fog.GetState(_factionId, hex);

			if (state == FogState.Visible)
			{
				continue;
			}

			Color color = state == FogState.Hidden ? HiddenColor : ExploredColor;
			Vector2 center = HexMetrics.ToPixel(hex);

			for (int i = 0; i < 6; i++)
			{
				points[i] = center + _corners[i];
			}

			DrawColoredPolygon(points, color);
		}
	}
}
