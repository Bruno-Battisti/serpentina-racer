using Godot;

namespace SerpentinaRacer.Core;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public const int TotalLaps = 3;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public void GoToMainMenu()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public void GoToRace()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/Race.tscn");
	}

	public void QuitGame()
	{
		GetTree().Quit();
	}
}
