using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Map;
using Godot;

/// <summary>
/// Экран приключенческой карты (этап 04). Строит карту и партии из контента, отправляет
/// команды в MapState и перерисовывает визуалы по его состоянию. Правил игры не содержит.
/// </summary>
public partial class AdventureMapController : Node2D
{
	/// <summary>Мастер-сид карты по умолчанию (до экрана новой игры).</summary>
	public const ulong DefaultSeed = 20261116UL;

	private const string TokenFolder = "res://assets/sprites/map/objects/";
	private const string CorePrefix = "core.";

	private static readonly string[] FactionIds =
	{
		"core.faction.brass",
		"core.faction.ash",
		"core.faction.root"
	};

	private readonly List<MapObjectTokenController> _objectTokens = new();
	private readonly List<PartyTokenController> _partyTokens = new();
	private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);

	private MapState? _state;
	private Party? _player;
	private HexGridVisual? _gridVisual;
	private FogOfWarVisual? _fogVisual;
	private Node2D? _objectLayer;
	private Node2D? _partyLayer;
	private CameraController? _camera;
	private DayCounterController? _dayCounter;
	private EndDayButtonController? _endDayButton;
	private ResourcePanelController? _resourcePanel;
	private Label? _movementLabel;
	private Label? _statusLabel;

	/// <inheritdoc />
	public override void _Ready()
	{
		ContentRegistry? registry = GameSession.Registry;

		if (registry is null)
		{
			GD.PushError("[Map] Контент не загружен: откройте игру через scenes/boot/Boot.tscn.");
			return;
		}

		_gridVisual = GetNode<HexGridVisual>("HexGridVisual");
		_fogVisual = GetNode<FogOfWarVisual>("FogOfWarVisual");
		_objectLayer = GetNode<Node2D>("MapObjectTokens");
		_partyLayer = GetNode<Node2D>("PartyTokens");
		_camera = GetNode<CameraController>("Camera");
		_dayCounter = GetNode<DayCounterController>("UI/DayCounter");
		_endDayButton = GetNode<EndDayButtonController>("UI/EndDayButton");
		_resourcePanel = GetNode<ResourcePanelController>("UI/ResourcePanel");
		_movementLabel = GetNode<Label>("UI/MovementLabel");
		_statusLabel = GetNode<Label>("UI/StatusLabel");

		// Порядок отрисовки: сетка, туман, токены объектов, токены партий.
		_gridVisual.ZIndex = 0;
		_fogVisual.ZIndex = 1;
		_objectLayer.ZIndex = 2;
		_partyLayer.ZIndex = 3;

		if (!TryCreateState(registry))
		{
			return;
		}

		MapState state = _state!;
		Party player = _player!;

		_gridVisual.Build(state.Map);
		_fogVisual.Bind(state.Map, state.Fog, player.FactionId);
		CreateObjectTokens(state.Map);
		CreatePartyTokens(state);

		_endDayButton.EndDayRequested += OnEndDayRequested;
		_resourcePanel.SetResources(ResourcePool.Empty);

		Vector2 corner = HexMetrics.ToPixel(
			state.Map.Grid.HexAt(state.Map.Grid.Width - 1, state.Map.Grid.Height - 1));
		_camera.SetBounds(new Rect2(-120f, -120f, corner.X + 240f, corner.Y + 240f));
		_camera.FocusOn(HexMetrics.ToPixel(player.Position));

		RefreshAll();
	}

	/// <inheritdoc />
	public override void _UnhandledInput(InputEvent @event)
	{
		if (_state is null || _player is null)
		{
			return;
		}

		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			return;
		}

		Hex target = HexMetrics.FromPixel(GetGlobalMousePosition());

		if (!_state.Map.Grid.Contains(target))
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		string factionId = _player.FactionId;

		if (!_state.Fog.IsVisible(factionId, target) && !_state.Fog.IsExplored(factionId, target))
		{
			ShowStatus(GameSession.Text("ui.map.unexplored"));
			return;
		}

		MapCommandResult result = _state.Apply(MapCommand.MoveParty(_player.Id, target));
		ShowOutcome(result);
		RefreshAll();
	}

	private bool TryCreateState(ContentRegistry registry)
	{
		try
		{
			MapGenerationSettings settings = MapContentAdapter.CreateSettings(registry);
			BiomeCatalog biomes = MapContentAdapter.CreateBiomes(registry);
			MapObjectCatalog objects = MapContentAdapter.CreateMapObjects(registry);

			IReadOnlyList<MapStartDefinition> starts = MapGenerator.CreateDefaultStarts(settings, FactionIds);
			MapLayoutDefinition layout = new MapGenerator(biomes, objects)
				.Generate(new MapStartRequest(DefaultSeed, settings, starts));

			GameMap map = MapContentAdapter.CreateMap(layout, biomes, objects);
			IReadOnlyList<Party> parties = MapContentAdapter.CreateParties(layout, settings, registry);

			_state = new MapState(map, parties, new FogOfWar(map.Grid), settings);
			_player = parties.FirstOrDefault(party => party.Role == PartyRole.Player) ?? parties.FirstOrDefault();

			if (_player is null)
			{
				GD.PushError("[Map] На карте нет ни одной партии.");
				return false;
			}

			return true;
		}
		catch (Exception exception) when (
			exception is ArgumentException or InvalidOperationException)
		{
			GD.PushError($"[Map] Не удалось создать карту: {exception.Message}");
			return false;
		}
	}

	private void CreateObjectTokens(GameMap map)
	{
		foreach (MapObjectInstance instance in map.Objects)
		{
			string name = instance.DefinitionId.StartsWith(CorePrefix, StringComparison.Ordinal)
				? instance.DefinitionId[CorePrefix.Length..]
				: instance.DefinitionId;
			// core.map_object.mine_gold -> token_mine_gold
			string suffix = name.StartsWith("map_object.", StringComparison.Ordinal)
				? name["map_object.".Length..]
				: name;

			MapObjectTokenController token = new();
			token.Bind(instance, LoadTexture($"token_{suffix}"));
			_objectLayer!.AddChild(token);
			_objectTokens.Add(token);
		}
	}

	private void CreatePartyTokens(MapState state)
	{
		foreach (Party party in state.Parties.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
		{
			string textureName = "token_army_neutral";

			if (party.HasHero)
			{
				string factionSuffix = party.FactionId[(party.FactionId.LastIndexOf('.') + 1)..];
				textureName = $"token_hero_{factionSuffix}";
			}

			PartyTokenController token = new();
			token.Bind(party, LoadTexture(textureName));
			_partyLayer!.AddChild(token);
			_partyTokens.Add(token);
		}
	}

	private Texture2D? LoadTexture(string name)
	{
		if (_textures.TryGetValue(name, out Texture2D? cached))
		{
			return cached;
		}

		string path = $"{TokenFolder}{name}.svg";
		Texture2D? texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

		if (texture is null)
		{
			GD.PushWarning($"[Map] Нет текстуры токена: {path}");
		}

		_textures[name] = texture;
		return texture;
	}

	private void OnEndDayRequested()
	{
		if (_state is null)
		{
			return;
		}

		_state.Apply(MapCommand.EndDay());
		ShowStatus(string.Empty);
		RefreshAll();
	}

	private void RefreshAll()
	{
		if (_state is null || _player is null)
		{
			return;
		}

		string factionId = _player.FactionId;
		_fogVisual?.Refresh();

		foreach (MapObjectTokenController token in _objectTokens)
		{
			MapObjectInstance? instance = token.Subject;

			if (instance is null)
			{
				continue;
			}

			// Объекты помнятся после разведки, патрули (армии) видны только в зоне обзора.
			bool isPatrol = string.Equals(instance.Type, MapObjectDefinition.PatrolType, StringComparison.Ordinal);
			bool shown = isPatrol
				? _state.Fog.IsVisible(factionId, instance.Position)
				: _state.Fog.IsVisible(factionId, instance.Position) || _state.Fog.IsExplored(factionId, instance.Position);
			token.SetShown(shown);
		}

		foreach (PartyTokenController token in _partyTokens)
		{
			Party? party = token.Subject;

			if (party is null)
			{
				continue;
			}

			bool own = string.Equals(party.FactionId, factionId, StringComparison.Ordinal);
			token.Refresh(own || _state.Fog.IsVisible(factionId, party.Position));
		}

		_dayCounter?.SetDay(_state.Day);

		if (_movementLabel is not null)
		{
			_movementLabel.Text = GameSession.Text("ui.map.movement", "current", _player.MovementPoints.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Replace("{max}", _player.MovementPointsPerDay.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
		}
	}

	private void ShowOutcome(MapCommandResult result)
	{
		switch (result.Outcome)
		{
			case MapCommandOutcome.Collision:
				ShowStatus(GameSession.Text("ui.map.battle_pending"));
				break;
			case MapCommandOutcome.NotEnoughMovement:
				ShowStatus(GameSession.Text("ui.map.not_enough_movement"));
				break;
			case MapCommandOutcome.Impassable:
			case MapCommandOutcome.NoPath:
			case MapCommandOutcome.OutOfBounds:
				ShowStatus(GameSession.Text("ui.map.blocked"));
				break;
			default:
				ShowStatus(string.Empty);
				break;
		}
	}

	private void ShowStatus(string text)
	{
		if (_statusLabel is not null)
		{
			_statusLabel.Text = text;
		}
	}
}
