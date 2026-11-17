using Godot;

/// <summary>
/// Камера карты: WASD/стрелки — сдвиг, колесо мыши — масштаб, средняя кнопка — перетаскивание.
/// </summary>
public partial class CameraController : Camera2D
{
	private const float MinZoom = 0.4f;
	private const float MaxZoom = 2.5f;
	private const float ZoomStep = 1.1f;
	private const float PanSpeed = 700f;

	/// <inheritdoc />
	public override void _Ready()
	{
		MakeCurrent();
	}

	/// <summary>Ставит камеру в мировую точку.</summary>
	/// <param name="point">Точка в мировых координатах.</param>
	public void FocusOn(Vector2 point)
	{
		Position = point;
	}

	/// <summary>Ограничивает камеру прямоугольником мира.</summary>
	/// <param name="bounds">Границы мира.</param>
	public void SetBounds(Rect2 bounds)
	{
		LimitLeft = (int)bounds.Position.X;
		LimitTop = (int)bounds.Position.Y;
		LimitRight = (int)bounds.End.X;
		LimitBottom = (int)bounds.End.Y;
	}

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		Vector2 direction = Vector2.Zero;

		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
		{
			direction.Y -= 1f;
		}

		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
		{
			direction.Y += 1f;
		}

		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
		{
			direction.X -= 1f;
		}

		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
		{
			direction.X += 1f;
		}

		if (direction != Vector2.Zero)
		{
			Position += direction.Normalized() * PanSpeed * (float)delta / Zoom.X;
		}
	}

	/// <inheritdoc />
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } button)
		{
			if (button.ButtonIndex == MouseButton.WheelUp)
			{
				ApplyZoom(ZoomStep);
			}
			else if (button.ButtonIndex == MouseButton.WheelDown)
			{
				ApplyZoom(1f / ZoomStep);
			}
		}
		else if (@event is InputEventMouseMotion motion
			&& (motion.ButtonMask & MouseButtonMask.Middle) != 0)
		{
			Position -= motion.Relative / Zoom.X;
		}
	}

	private void ApplyZoom(float factor)
	{
		float zoom = Mathf.Clamp(Zoom.X * factor, MinZoom, MaxZoom);
		Zoom = new Vector2(zoom, zoom);
	}
}
