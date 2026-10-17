using Godot;
using SerpentinaRacer.Audio;
using SerpentinaRacer.Core;

namespace SerpentinaRacer.UI;

public partial class MainMenuUI : Control
{
	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		Theme = UiTheme.Build();

		var background = new MenuBackground();
		AddChild(background);

		var title = UiTheme.MakeTitleLabel("SERPENTINA RACER", 56, Colors.White);
		title.SetAnchorsPreset(LayoutPreset.TopWide);
		title.Position = new Vector2(0, 60);
		title.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.6f));
		title.AddThemeConstantOverride("outline_size", 8);
		AddChild(title);

		var subtitle = UiTheme.MakeTitleLabel("Corrida arcade nas serras", 22, new Color(1f, 0.92f, 0.78f));
		subtitle.SetAnchorsPreset(LayoutPreset.TopWide);
		subtitle.Position = new Vector2(0, 130);
		AddChild(subtitle);

		var buttons = new VBoxContainer
		{
			Position = new Vector2(0, 0),
			CustomMinimumSize = new Vector2(320, 0)
		};
		buttons.SetAnchorsPreset(LayoutPreset.CenterLeft);
		buttons.Position = new Vector2(90, -40);
		buttons.AddThemeConstantOverride("separation", 18);
		AddChild(buttons);

		var startButton = new Button { Text = "INICIAR CORRIDA", CustomMinimumSize = new Vector2(300, 56) };
		startButton.Pressed += OnStartPressed;
		buttons.AddChild(startButton);

		var quitButton = new Button { Text = "SAIR", CustomMinimumSize = new Vector2(300, 56) };
		quitButton.Pressed += OnQuitPressed;
		buttons.AddChild(quitButton);

		BuildInfoPanel();
	}

	private void BuildInfoPanel()
	{
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
		panel.SetAnchorsPreset(LayoutPreset.CenterRight);
		panel.Position = new Vector2(-420, -160);
		AddChild(panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 6);
		panel.AddChild(box);

		box.AddChild(UiTheme.MakeTitleLabel("CONTROLES", 26, UiTheme.Accent));
		AddControlLine(box, "W / seta cima", "Acelerar");
		AddControlLine(box, "S / seta baixo", "Frear / Ré");
		AddControlLine(box, "A D / setas", "Virar");
		AddControlLine(box, "Espaço", "Freio de mão");
		AddControlLine(box, "R", "Reposicionar carro");
		AddControlLine(box, "ESC", "Pausar");

		var sep = new HSeparator();
		box.AddChild(sep);

		float best = SaveSystem.Instance?.BestLapTime ?? -1f;
		string bestText = best >= 0 ? TimeFormat.Format(best) : "--:--.---";
		box.AddChild(UiTheme.MakeTitleLabel($"Melhor volta: {bestText}", 20, Colors.White));
	}

	private static void AddControlLine(VBoxContainer box, string key, string action)
	{
		var row = new HBoxContainer();
		var keyLabel = new Label { Text = key, CustomMinimumSize = new Vector2(150, 0) };
		keyLabel.AddThemeColorOverride("font_color", UiTheme.Accent);
		var actionLabel = new Label { Text = action };
		row.AddChild(keyLabel);
		row.AddChild(actionLabel);
		box.AddChild(row);
	}

	private void OnStartPressed()
	{
		AudioManager.Instance?.PlayClick();
		GameManager.Instance.GoToRace();
	}

	private void OnQuitPressed()
	{
		GameManager.Instance.QuitGame();
	}
}
