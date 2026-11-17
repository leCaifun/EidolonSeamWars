using Game.Core.Domain;
using Godot;

/// <summary>
/// Панель семи ресурсов: иконка и количество. Значения передаёт AdventureMapController.
/// </summary>
public partial class ResourcePanelController : PanelContainer
{
	private const string IconFolder = "res://assets/sprites/ui/resources/";

	private readonly Dictionary<ResourceKind, Label> _amounts = new();

	/// <inheritdoc />
	public override void _Ready()
	{
		HBoxContainer list = GetNode<HBoxContainer>("ResourceList");

		foreach (ResourceKind kind in Enum.GetValues<ResourceKind>())
		{
			string id = kind.ToString().ToLowerInvariant();
			HBoxContainer slot = new() { TooltipText = GameSession.Text($"resource.{id}.name") };

			string iconPath = $"{IconFolder}icon_{id}.svg";
			TextureRect icon = new()
			{
				Texture = ResourceLoader.Exists(iconPath) ? GD.Load<Texture2D>(iconPath) : null,
				CustomMinimumSize = new Vector2(24f, 24f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
			};

			Label amount = new() { Text = "0" };
			slot.AddChild(icon);
			slot.AddChild(amount);
			list.AddChild(slot);
			_amounts[kind] = amount;
		}
	}

	/// <summary>Показывает количество ресурсов.</summary>
	/// <param name="pool">Текущие ресурсы игрока.</param>
	public void SetResources(ResourcePool pool)
	{
		foreach (KeyValuePair<ResourceKind, Label> pair in _amounts)
		{
			pair.Value.Text = pool.Get(pair.Key).ToString(System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
