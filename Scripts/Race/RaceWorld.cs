using Godot;
using SerpentinaRacer.Audio;
using SerpentinaRacer.Car;
using SerpentinaRacer.Environment;
using SerpentinaRacer.Track;
using SerpentinaRacer.UI;

namespace SerpentinaRacer.Race;

/// <summary>
/// Scene root for Race.tscn. Orchestrates generation order (track, then
/// scenery, then car/camera/race-logic/UI) so nothing reads geometry before
/// it exists, and owns the pause flow.
/// </summary>
public partial class RaceWorld : Node3D
{
	private CarController _car;
	private RaceManager _raceManager;
	private PauseMenu _pauseMenu;
	private ResultsScreen _resultsScreen;
	private bool _paused;

	public override void _Ready()
	{
		var track = new TrackBuilder { Name = "Track" };
		AddChild(track);
		track.Generate();

		BuildVoidFloor();

		var environment = new EnvironmentBuilder { Name = "Environment" };
		AddChild(environment);
		environment.Generate(track);

		_car = new CarController { Name = "PlayerCar" };
		AddChild(_car);
		_car.GlobalTransform = track.StartTransform;
		_car.SetSpawn(track.StartTransform);

		var camera = new CameraRig { Name = "ChaseCamera", Current = true };
		AddChild(camera);
		camera.Init(_car);

		_raceManager = new RaceManager { Name = "RaceManager" };
		AddChild(_raceManager);
		_raceManager.Setup(_car, track, track.Checkpoints);
		_raceManager.RaceFinished += OnRaceFinished;

		BuildUi();
	}

	/// <summary>Last-resort catch-all floor far below the track, in case the car
	/// ever ends up somewhere with no nearby collision (e.g. launched off the
	/// map) - resets it instead of letting it fall forever.</summary>
	private void BuildVoidFloor()
	{
		var floor = new StaticBody3D { Name = "VoidFloor" };
		floor.SetCollisionLayerValue(1, true);
		var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(4000, 2, 4000) }, Position = new Vector3(0, -120, 0) };
		floor.AddChild(shape);
		AddChild(floor);
	}

	private void BuildUi()
	{
		var canvas = new CanvasLayer { Name = "UI" };
		AddChild(canvas);

		var hud = new RaceHud { Name = "Hud" };
		canvas.AddChild(hud);
		hud.Setup(_raceManager, _car);

		_pauseMenu = new PauseMenu { Name = "PauseMenu" };
		canvas.AddChild(_pauseMenu);
		_pauseMenu.ResumeRequested += TogglePause;

		_resultsScreen = new ResultsScreen { Name = "ResultsScreen" };
		canvas.AddChild(_resultsScreen);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("pause_game") && _raceManager.CurrentState != RaceState.Finished)
		{
			TogglePause();
			GetViewport().SetInputAsHandled();
		}
	}

	private void TogglePause()
	{
		_paused = !_paused;
		GetTree().Paused = _paused;

		if (_paused)
		{
			_pauseMenu.Open();
			AudioManager.Instance?.StopEngine();
		}
		else
		{
			_pauseMenu.Close();
			if (_raceManager.CurrentState == RaceState.Racing)
				AudioManager.Instance?.StartEngine();
		}
	}

	private void OnRaceFinished(float totalTime, bool isNewBest)
	{
		_resultsScreen.Show(totalTime, isNewBest);
	}
}
