using Godot;
using SerpentinaRacer.Car;
using SerpentinaRacer.Core;
using SerpentinaRacer.Race;

namespace SerpentinaRacer.UI;

public partial class RaceHud : Control
{
	private RaceManager _raceManager;
	private CarController _car;

	private Label _speedLabel;
	private Label _timeLabel;
	private Label _lapLabel;
	private Label _bestLabel;
	private Label _countdownLabel;
	private Label _messageLabel;
	private Timer _messageTimer;
	private Timer _countdownHideTimer;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		Theme = UiTheme.Build();

		BuildTopBar();
		BuildCenterLabels();

		_messageTimer = new Timer { OneShot = true, WaitTime = 1.6 };
		_messageTimer.Timeout += () => _messageLabel.Visible = false;
		AddChild(_messageTimer);

		_countdownHideTimer = new Timer { OneShot = true, WaitTime = 0.9 };
		_countdownHideTimer.Timeout += () => _countdownLabel.Visible = false;
		AddChild(_countdownHideTimer);
	}

	private void BuildTopBar()
	{
		var panel = new PanelContainer();
		panel.SetAnchorsPreset(LayoutPreset.TopWide);
		panel.Position = new Vector2(0, 16);
		panel.GrowHorizontal = GrowDirection.Both;
		AddChild(panel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 40);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		panel.AddChild(row);

		_lapLabel = UiTheme.MakeTitleLabel("Volta 1/3", 24, Colors.White);
		_timeLabel = UiTheme.MakeTitleLabel("00:00.000", 24, UiTheme.Accent);
		_bestLabel = UiTheme.MakeTitleLabel("Melhor: --:--.---", 20, new Color(0.8f, 0.9f, 1f));

		row.AddChild(_lapLabel);
		row.AddChild(_timeLabel);
		row.AddChild(_bestLabel);

		var speedPanel = new PanelContainer();
		speedPanel.SetAnchorsPreset(LayoutPreset.BottomRight);
		speedPanel.Position = new Vector2(-190, -110);
		AddChild(speedPanel);

		_speedLabel = UiTheme.MakeTitleLabel("0 km/h", 34, Colors.White);
		speedPanel.AddChild(_speedLabel);
	}

	private void BuildCenterLabels()
	{
		_countdownLabel = UiTheme.MakeTitleLabel("", 96, UiTheme.Accent);
		_countdownLabel.SetAnchorsPreset(LayoutPreset.Center);
		_countdownLabel.Position = new Vector2(-80, -220);
		_countdownLabel.Visible = false;
		AddChild(_countdownLabel);

		_messageLabel = UiTheme.MakeTitleLabel("", 32, Colors.White);
		_messageLabel.SetAnchorsPreset(LayoutPreset.Center);
		_messageLabel.Position = new Vector2(-250, -150);
		_messageLabel.CustomMinimumSize = new Vector2(500, 0);
		_messageLabel.Visible = false;
		AddChild(_messageLabel);
	}

	public void Setup(RaceManager raceManager, CarController car)
	{
		_raceManager = raceManager;
		_car = car;

		raceManager.CountdownTick += OnCountdownTick;
		raceManager.RaceStarted += OnRaceStarted;
		raceManager.LapCompleted += OnLapCompleted;
		raceManager.CheckpointPassed += OnCheckpointPassed;

		float best = SaveSystem.Instance?.BestLapTime ?? -1f;
		_bestLabel.Text = $"Melhor: {TimeFormat.Format(best)}";
	}

	public override void _Process(double delta)
	{
		if (_car != null)
			_speedLabel.Text = $"{Mathf.RoundToInt(_car.SpeedKmh)} km/h";

		if (_raceManager != null)
		{
			_timeLabel.Text = TimeFormat.Format((float)_raceManager.RaceTime);
			int lap = Mathf.Min(_raceManager.CurrentLap + 1, RaceManager.TotalLaps);
			_lapLabel.Text = $"Volta {lap}/{RaceManager.TotalLaps}";
		}
	}

	private void OnCountdownTick(int secondsLeft)
	{
		_countdownLabel.Visible = true;
		_countdownLabel.Text = secondsLeft.ToString();
	}

	private void OnRaceStarted()
	{
		_countdownLabel.Text = "VAI!";
		_countdownLabel.Visible = true;
		_countdownHideTimer.Start();
	}

	private void OnCheckpointPassed(int index)
	{
		if (index != 0)
			ShowMessage("Checkpoint!");
	}

	private void OnLapCompleted(int lapNumber, float lapTime, bool isNewBest)
	{
		float best = SaveSystem.Instance?.BestLapTime ?? -1f;
		_bestLabel.Text = $"Melhor: {TimeFormat.Format(best)}";
		string suffix = isNewBest ? " - NOVO RECORDE!" : "";
		ShowMessage($"Volta {lapNumber}: {TimeFormat.Format(lapTime)}{suffix}");
	}

	private void ShowMessage(string text)
	{
		_messageLabel.Text = text;
		_messageLabel.Visible = true;
		_messageTimer.Start();
	}
}
