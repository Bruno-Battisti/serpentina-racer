using Godot;

namespace SerpentinaRacer.Core;

/// <summary>Small helpers that synthesize tileable textures at runtime (no image assets on disk).</summary>
public static class ProceduralTextures
{
	public static ImageTexture MakeNoiseTexture(int size, Color baseColor, float variation, int seed)
	{
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var rng = new RandomNumberGenerator { Seed = (ulong)seed };
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float n = rng.RandfRange(-variation, variation);
				var c = new Color(
					Mathf.Clamp(baseColor.R + n, 0f, 1f),
					Mathf.Clamp(baseColor.G + n, 0f, 1f),
					Mathf.Clamp(baseColor.B + n, 0f, 1f));
				image.SetPixel(x, y, c);
			}
		}
		return ImageTexture.CreateFromImage(image);
	}

	public static ImageTexture MakeCheckerTexture(int size, Color colorA, Color colorB, int cells)
	{
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		int cellSize = Mathf.Max(1, size / cells);
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				bool a = ((x / cellSize) + (y / cellSize)) % 2 == 0;
				image.SetPixel(x, y, a ? colorA : colorB);
			}
		}
		return ImageTexture.CreateFromImage(image);
	}
}
