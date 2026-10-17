using Godot;

namespace SerpentinaRacer.Core;

public static class TimeFormat
{
	public static string Format(float seconds)
	{
		if (seconds < 0)
			return "--:--.---";

		int minutes = (int)(seconds / 60f);
		float remaining = seconds - minutes * 60f;
		return $"{minutes:00}:{remaining:00.000}";
	}
}
