using System.Collections.Generic;
using Godot;
using SerpentinaRacer.Audio;
using SerpentinaRacer.Car;
using SerpentinaRacer.Core;
using SerpentinaRacer.Track;

namespace SerpentinaRacer.Race;

public enum RaceState
{
	Countdown,
	Racing,
	Finished
}

/// <summary>
/// Drives the whole race flow: countdown, ordered checkpoint validation (so a
/// lap can only be scored by actually driving the loop forward), lap/race
/// timers and the finish condition.
/// </summary>
public partial class RaceManager : Node
{
	[Signal] public delegate void CountdownTickEventHandler(int secondsLeft);
	[Signal] public delegate void RaceStartedEventHandler();
	[Signal] public delegate void LapCompletedEventHandler(int lapNumber, float lapTime, bool isNewBest);
	[Signal] public delegate void RaceFinishedEventHandler(float totalTime, bool isNewBest);
	[Signal] public delegate void CheckpointPassedEventHandler(int index);

	public const int TotalLaps = GameManager.TotalLaps;
	public const float CountdownSeconds = 3f;

	public RaceState CurrentState { get; private set; } = RaceState.Countdown;
	public int CurrentLap { get; private set; } = 0;
	public double RaceTime { get; private set; } = 0;
	public double CurrentLapTime { get; private set; } = 0;

	private CarController _car;
	private TrackBuilder _track;
	private int _nextCheckpointIndex = 1;
	private double _countdown = CountdownSeconds;
	private int _lastShownCountdown = -1;

	public void Setup(CarController car, TrackBuilder track, IEnumerable<Checkpoint> checkpoints)
	{
		_car = car;
		_track = track;
		car.ControlsEnabled = false;

		foreach (var cp in checkpoints)
			cp.CarPassed += OnCheckpointPassed;
	}

	public override void _Process(double delta)
	{
		switch (CurrentState)
		{
			case RaceState.Countdown:
				_countdown -= delta;
				int shown = Mathf.CeilToInt((float)_countdown);
				if (shown != _lastShownCountdown)
				{
					_lastShownCountdown = shown;
					if (shown > 0)
					{
						EmitSignal(SignalName.CountdownTick, shown);
						AudioManager.Instance?.PlayBeep();
					}
				}
				if (_countdown <= 0)
				{
					CurrentState = RaceState.Racing;
					_car.ControlsEnabled = true;
					AudioManager.Instance?.PlayGo();
					AudioManager.Instance?.StartEngine();
					EmitSignal(SignalName.RaceStarted);
				}
				break;

			case RaceState.Racing:
				RaceTime += delta;
				CurrentLapTime += delta;
				break;
		}
	}

	private void OnCheckpointPassed(int index)
	{
		if (CurrentState != RaceState.Racing)
			return;

		if (index != _nextCheckpointIndex)
			return;

		if (index == 0)
		{
			CompleteLap();
			_nextCheckpointIndex = 1;
		}
		else
		{
			AudioManager.Instance?.PlayCheckpoint();
			_car.SetSpawn(_track.CheckpointTransforms[index]);
			_nextCheckpointIndex = (index + 1) % _track.CheckpointCount;
		}

		EmitSignal(SignalName.CheckpointPassed, index);
	}

	private void CompleteLap()
	{
		float lapTime = (float)CurrentLapTime;
		CurrentLap++;
		bool isNewBest = SaveSystem.Instance.SubmitLapTime(lapTime);
		AudioManager.Instance?.PlayLap();
		_car.SetSpawn(_track.StartTransform);
		EmitSignal(SignalName.LapCompleted, CurrentLap, lapTime, isNewBest);
		CurrentLapTime = 0;

		if (CurrentLap >= TotalLaps)
			FinishRace();
	}

	private void FinishRace()
	{
		CurrentState = RaceState.Finished;
		_car.ControlsEnabled = false;
		AudioManager.Instance?.StopEngine();
		float total = (float)RaceTime;
		bool isNewBest = SaveSystem.Instance.SubmitTotalTime(total);
		EmitSignal(SignalName.RaceFinished, total, isNewBest);
	}
}
