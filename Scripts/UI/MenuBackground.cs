using System.Collections.Generic;
using Godot;

namespace SerpentinaRacer.UI;

/// <summary>Paints a cheap stylized sky/mountains/winding-road backdrop for the menus
/// (plain Control drawing, no texture assets needed).</summary>
public partial class MenuBackground : Control
{
	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		Resized += QueueRedraw;
	}

	public override void _Draw()
	{
		var size = Size;
		if (size.X <= 0 || size.Y <= 0)
			return;

		DrawRect(new Rect2(Vector2.Zero, size), new Color(0.47f, 0.73f, 0.92f));
		DrawCircle(new Vector2(size.X * 0.8f, size.Y * 0.2f), size.Y * 0.08f, new Color(1f, 0.96f, 0.8f));

		DrawMountainLayer(size, size.Y * 0.46f, new Color(0.4f, 0.45f, 0.56f), 42);
		DrawMountainLayer(size, size.Y * 0.55f, new Color(0.28f, 0.32f, 0.4f), 73);

		DrawRect(new Rect2(new Vector2(0, size.Y * 0.6f), new Vector2(size.X, size.Y * 0.4f)), new Color(0.21f, 0.37f, 0.19f));

		DrawRoad(size);
		DrawRect(new Rect2(Vector2.Zero, size), new Color(0f, 0f, 0f, 0.18f));
	}

	private void DrawMountainLayer(Vector2 size, float baseY, Color color, int seed)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)seed };
		const int peaks = 6;
		var points = new List<Vector2> { new(0, size.Y) };
		for (int i = 0; i <= peaks; i++)
		{
			float x = size.X * i / (float)peaks;
			float h = baseY - rng.RandfRange(35f, 150f);
			points.Add(new Vector2(x, h));
		}
		points.Add(new Vector2(size.X, size.Y));
		DrawColoredPolygon(points.ToArray(), color);
	}

	private void DrawRoad(Vector2 size)
	{
		var points = new[]
		{
			new Vector2(size.X * 0.5f, size.Y * 1.05f),
			new Vector2(size.X * 0.4f, size.Y * 0.86f),
			new Vector2(size.X * 0.6f, size.Y * 0.74f),
			new Vector2(size.X * 0.46f, size.Y * 0.63f),
			new Vector2(size.X * 0.55f, size.Y * 0.58f)
		};
		for (int i = 0; i < points.Length - 1; i++)
			DrawLine(points[i], points[i + 1], new Color(0.17f, 0.17f, 0.19f), 46f, true);
		for (int i = 0; i < points.Length - 1; i++)
			DrawLine(points[i], points[i + 1], new Color(0.92f, 0.86f, 0.25f), 3f, true);
	}
}
