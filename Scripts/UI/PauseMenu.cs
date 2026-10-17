using Godot;
using SerpentinaRacer.Audio;
using SerpentinaRacer.Core;

namespace SerpentinaRacer.UI;

public partial class PauseMenu : Control
{
	[Signal] public delegate void ResumeRequestedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsPreset(LayoutPreset.FullRect);
		Theme = UiTheme.Build();
		Visible = false;

		AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.55f), AnchorRight = 1, AnchorBottom = 1 });

		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(360, 0) };
		center.AddChild(panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 16);
		panel.AddChild(box);

		box.AddChild(UiTheme.MakeTitleLabel("PAUSADO", 34, UiTheme.Accent));

		var resume = new Button { Text = "CONTINUAR", CustomMinimumSize = new Vector2(320, 50) };
		resume.Pressed += () => { AudioManager.Instance?.PlayClick(); EmitSignal(SignalName.ResumeRequested); };
		box.AddChild(resume);

		var restart = new Button { Text = "REINICIAR CORRIDA", CustomMinimumSize = new Vector2(320, 50) };
		restart.Pressed += () =>
		{
			AudioManager.Instance?.PlayClick();
			GetTree().Paused = false;
			GameManager.Instance.GoToRace();
		};
		box.AddChild(restart);

		var menu = new Button { Text = "MENU PRINCIPAL", CustomMinimumSize = new Vector2(320, 50) };
		menu.Pressed += () =>
		{
			AudioManager.Instance?.PlayClick();
			GetTree().Paused = false;
			GameManager.Instance.GoToMainMenu();
		};
		box.AddChild(menu);
	}

	public void Open()
	{
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
	}
}
