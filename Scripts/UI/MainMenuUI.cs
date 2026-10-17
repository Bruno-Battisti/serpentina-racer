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

		// A single CenterContainer keeps the whole block (title + buttons + info)
		// correctly centered on screen regardless of resolution - no manual
		// pixel offsets, which is what caused the previous layout bug.
		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		var content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 22);
		content.Alignment = BoxContainer.AlignmentMode.Center;
		center.AddChild(content);

		var title = UiTheme.MakeTitleLabel("SERPENTINA RACER", 52, Colors.White);
		title.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.6f));
		title.AddThemeConstantOverride("outline_size", 8);
		content.AddChild(title);

		var subtitle = UiTheme.MakeTitleLabel("Corrida arcade nas serras", 20, new Color(1f, 0.92f, 0.78f));
		content.AddChild(subtitle);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 12) };
		content.AddChild(spacer);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 50);
		content.AddChild(row);

		var buttons = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
		buttons.AddThemeConstantOverride("separation", 16);
		row.AddChild(buttons);

		var startButton = new Button { Text = "INICIAR CORRIDA", CustomMinimumSize = new Vector2(300, 56) };
		startButton.Pressed += OnStartPressed;
		buttons.AddChild(startButton);

		var quitButton = new Button { Text = "SAIR", CustomMinimumSize = new Vector2(300, 56) };
		quitButton.Pressed += OnQuitPressed;
		buttons.AddChild(quitButton);

		BuildInfoPanel(row);
	}

	private void BuildInfoPanel(Control parent)
	{
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(360, 0) };
		parent.AddChild(panel);

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

		box.AddChild(new HSeparator());

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
