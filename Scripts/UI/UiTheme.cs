using Godot;

namespace SerpentinaRacer.UI;

/// <summary>Builds one shared arcade-style Theme used by every UI screen, entirely in code.</summary>
public static class UiTheme
{
	public static readonly Color Accent = new(0.95f, 0.62f, 0.1f);
	public static readonly Color AccentDark = new(0.75f, 0.42f, 0.05f);
	public static readonly Color PanelBg = new(0.07f, 0.08f, 0.12f, 0.82f);
	public static readonly Color TextLight = new(0.95f, 0.96f, 0.98f);

	private static Theme _cached;

	public static Theme Build()
	{
		if (_cached != null)
			return _cached;

		var theme = new Theme();
		var font = ThemeDB.FallbackFont;
		theme.DefaultFontSize = 22;
		theme.SetFont("font", "Label", font);
		theme.SetFont("font", "Button", font);

		var buttonNormal = MakeStyleBox(AccentDark, 10, Accent, 2);
		var buttonHover = MakeStyleBox(Accent, 10, TextLight, 2);
		var buttonPressed = MakeStyleBox(new Color(0.55f, 0.3f, 0.02f), 10, TextLight, 2);

		theme.SetStylebox("normal", "Button", buttonNormal);
		theme.SetStylebox("hover", "Button", buttonHover);
		theme.SetStylebox("pressed", "Button", buttonPressed);
		theme.SetStylebox("focus", "Button", buttonHover);
		theme.SetColor("font_color", "Button", TextLight);
		theme.SetColor("font_hover_color", "Button", new Color(0.1f, 0.08f, 0.02f));
		theme.SetFontSize("font_size", "Button", 26);

		theme.SetColor("font_color", "Label", TextLight);
		theme.SetFontSize("font_size", "Label", 22);

		var panelStyle = MakeStyleBox(PanelBg, 14, Accent, 2);
		theme.SetStylebox("panel", "PanelContainer", panelStyle);
		theme.SetStylebox("panel", "Panel", panelStyle);

		_cached = theme;
		return theme;
	}

	public static StyleBoxFlat MakeStyleBox(Color bg, float radius, Color border, float borderWidth)
	{
		return new StyleBoxFlat
		{
			BgColor = bg,
			CornerRadiusTopLeft = (int)radius,
			CornerRadiusTopRight = (int)radius,
			CornerRadiusBottomLeft = (int)radius,
			CornerRadiusBottomRight = (int)radius,
			BorderColor = border,
			BorderWidthTop = (int)borderWidth,
			BorderWidthBottom = (int)borderWidth,
			BorderWidthLeft = (int)borderWidth,
			BorderWidthRight = (int)borderWidth,
			ContentMarginLeft = 18,
			ContentMarginRight = 18,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		};
	}

	public static Label MakeTitleLabel(string text, int size, Color color)
	{
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}
}
