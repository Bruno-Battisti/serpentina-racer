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

	/// <summary>Places a control at a screen edge/corner using size-flags inside a
	/// full-rect MarginContainer, instead of hand-computed anchor+pixel offsets
	/// (which previously produced badly misplaced, clipped UI).</summary>
	private Control Pin(Control child, Control.SizeFlags h, Control.SizeFlags v, int margin = 18)
	{
		var wrap = new MarginContainer();
		wrap.SetAnchorsPreset(LayoutPreset.FullRect);
		wrap.MouseFilter = MouseFilterEnum.Ignore;
		wrap.AddThemeConstantOverride("margin_left", margin);
		wrap.AddThemeConstantOverride("margin_right", margin);
		wrap.AddThemeConstantOverride("margin_top", margin);
		wrap.AddThemeConstantOverride("margin_bottom", margin);
		child.SizeFlagsHorizontal = h;
		child.SizeFlagsVertical = v;
		wrap.AddChild(child);
		AddChild(wrap);
		return wrap;
	}

	private void BuildTopBar()
	{
		var panel = new PanelContainer();
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

		Pin(panel, Control.SizeFlags.ShrinkCenter, Control.SizeFlags.ShrinkBegin);

		var speedPanel = new PanelContainer();
		_speedLabel = UiTheme.MakeTitleLabel("0 km/h", 34, Colors.White);
		speedPanel.AddChild(_speedLabel);
		Pin(speedPanel, Control.SizeFlags.ShrinkEnd, Control.SizeFlags.ShrinkEnd, 26);
	}

	private void BuildCenterLabels()
	{
		_countdownLabel = UiTheme.MakeTitleLabel("", 96, UiTheme.Accent);
		_countdownLabel.Visible = false;
		Pin(_countdownLabel, Control.SizeFlags.ShrinkCenter, Control.SizeFlags.ShrinkCenter);

		_messageLabel = UiTheme.MakeTitleLabel("", 32, Colors.White);
		_messageLabel.CustomMinimumSize = new Vector2(500, 0);
		_messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_messageLabel.Visible = false;
		var messageWrap = Pin(_messageLabel, Control.SizeFlags.ShrinkCenter, Control.SizeFlags.ShrinkCenter);
		messageWrap.AddThemeConstantOverride("margin_top", 500);
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
