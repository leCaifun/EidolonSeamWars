using Game.Core.Domain;
using Godot;

/// <summary>
/// Геометрия гексов в пикселях для pointy-top сетки (спрайты 64×64, радиус гекса 30).
/// Единственное место, где аксиальные координаты Core переводятся в экранные.
/// </summary>
public static class HexMetrics
{
	/// <summary>Радиус гекса (расстояние от центра до вершины) в пикселях.</summary>
	public const float Size = 30f;

	private static readonly float Sqrt3 = Mathf.Sqrt(3f);

	/// <summary>Центр гекса в мировых координатах.</summary>
	public static Vector2 ToPixel(Hex hex)
	{
		float x = Size * Sqrt3 * (hex.Q + (hex.R / 2f));
		float y = Size * 1.5f * hex.R;
		return new Vector2(x, y);
	}

	/// <summary>Гекс под мировой точкой (может лежать вне сетки карты).</summary>
	public static Hex FromPixel(Vector2 point)
	{
		float q = ((Sqrt3 / 3f * point.X) - (point.Y / 3f)) / Size;
		float r = (2f / 3f * point.Y) / Size;
		return RoundToHex(q, r);
	}

	/// <summary>Шесть вершин гекса относительно центра, начиная с верхней.</summary>
	/// <param name="scale">Множитель радиуса.</param>
	public static Vector2[] Corners(float scale = 1f)
	{
		Vector2[] corners = new Vector2[6];

		for (int i = 0; i < 6; i++)
		{
			float angle = Mathf.DegToRad((60f * i) - 90f);
			corners[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (Size * scale);
		}

		return corners;
	}

	private static Hex RoundToHex(float q, float r)
	{
		float s = -q - r;
		int roundedQ = Mathf.RoundToInt(q);
		int roundedR = Mathf.RoundToInt(r);
		int roundedS = Mathf.RoundToInt(s);

		float deltaQ = Mathf.Abs(roundedQ - q);
		float deltaR = Mathf.Abs(roundedR - r);
		float deltaS = Mathf.Abs(roundedS - s);

		if (deltaQ > deltaR && deltaQ > deltaS)
		{
			roundedQ = -roundedR - roundedS;
		}
		else if (deltaR > deltaS)
		{
			roundedR = -roundedQ - roundedS;
		}

		return new Hex(roundedQ, roundedR);
	}
}
