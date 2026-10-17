using Godot;
using SerpentinaRacer.Audio;
using SerpentinaRacer.Core;

namespace SerpentinaRacer.UI;

public partial class ResultsScreen : Control
{
	private Label _finalTimeLabel;
	private Label _bestTimeLabel;
	private Label _recordLabel;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsPreset(LayoutPreset.FullRect);
		Theme = UiTheme.Build();
		Visible = false;

		AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.65f), AnchorRight = 1, AnchorBottom = 1 });

		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(400, 0) };
		center.AddChild(panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 14);
		panel.AddChild(box);

		box.AddChild(UiTheme.MakeTitleLabel("CORRIDA CONCLUIDA", 30, UiTheme.Accent));
		_recordLabel = UiTheme.MakeTitleLabel("", 22, new Color(0.3f, 1f, 0.4f));
		box.AddChild(_recordLabel);

		_finalTimeLabel = UiTheme.MakeTitleLabel("Tempo: --:--.---", 24, Colors.White);
		box.AddChild(_finalTimeLabel);

		_bestTimeLabel = UiTheme.MakeTitleLabel("Melhor tempo total: --:--.---", 20, new Color(0.8f, 0.9f, 1f));
		box.AddChild(_bestTimeLabel);

		var again = new Button { Text = "JOGAR DE NOVO", CustomMinimumSize = new Vector2(360, 50) };
		again.Pressed += () =>
		{
			AudioManager.Instance?.PlayClick();
			GetTree().Paused = false;
			GameManager.Instance.GoToRace();
		};
		box.AddChild(again);

		var menu = new Button { Text = "MENU PRINCIPAL", CustomMinimumSize = new Vector2(360, 50) };
		menu.Pressed += () =>
		{
			AudioManager.Instance?.PlayClick();
			GetTree().Paused = false;
			GameManager.Instance.GoToMainMenu();
		};
		box.AddChild(menu);
	}

	public void Show(float totalTime, bool isNewBest)
	{
		_finalTimeLabel.Text = $"Tempo: {TimeFormat.Format(totalTime)}";
		float best = SaveSystem.Instance?.BestTotalTime ?? -1f;
		_bestTimeLabel.Text = $"Melhor tempo total: {TimeFormat.Format(best)}";
		_recordLabel.Text = isNewBest ? "NOVO RECORDE!" : "";
		Visible = true;
	}
}
