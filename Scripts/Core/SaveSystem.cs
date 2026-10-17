using Godot;

namespace SerpentinaRacer.Core;

public partial class SaveSystem : Node
{
	public static SaveSystem Instance { get; private set; }

	private const string SavePath = "user://savegame.json";

	public float BestLapTime { get; private set; } = -1f;
	public float BestTotalTime { get; private set; } = -1f;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		Load();
	}

	private void Load()
	{
		if (!FileAccess.FileExists(SavePath))
			return;

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		if (file == null)
			return;

		var text = file.GetAsText();
		var parsed = Json.ParseString(text);
		if (parsed.VariantType != Variant.Type.Dictionary)
			return;

		var dict = parsed.AsGodotDictionary();
		if (dict.TryGetValue("bestLap", out var bestLap))
			BestLapTime = bestLap.AsSingle();
		if (dict.TryGetValue("bestTotal", out var bestTotal))
			BestTotalTime = bestTotal.AsSingle();
	}

	private void Save()
	{
		var dict = new Godot.Collections.Dictionary
		{
			{ "bestLap", BestLapTime },
			{ "bestTotal", BestTotalTime }
		};

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		file?.StoreString(Json.Stringify(dict));
	}

	public bool SubmitLapTime(float seconds)
	{
		if (BestLapTime >= 0f && seconds >= BestLapTime)
			return false;

		BestLapTime = seconds;
		Save();
		return true;
	}

	public bool SubmitTotalTime(float seconds)
	{
		if (BestTotalTime >= 0f && seconds >= BestTotalTime)
			return false;

		BestTotalTime = seconds;
		Save();
		return true;
	}
}
