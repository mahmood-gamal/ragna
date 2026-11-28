using Godot;
using Ragna.Core.Hud;

namespace Ragna.Ui;

/// <summary>One coloured HUD bar (HP, SP or EXP) with its caption drawn on top. Built by <see cref="Hud"/>.</summary>
public partial class HudBar : ProgressBar
{
    private readonly Label _caption = new()
    {
        MouseFilter = MouseFilterEnum.Ignore,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>The caption on the bar, for example <c>HP 45 / 60</c>.</summary>
    public string Caption => _caption.Text;

    /// <summary>Sets the fill colour and width. Call once, before adding the bar to the tree.</summary>
    public void Setup(Color fill, int width)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ShowPercentage = false;
        MinValue = 0;
        MaxValue = 1;
        Step = 0;
        CustomMinimumSize = new Vector2(width, 18);

        var background = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.6f) };
        background.SetCornerRadiusAll(3);
        var foreground = new StyleBoxFlat { BgColor = fill };
        foreground.SetCornerRadiusAll(3);
        AddThemeStyleboxOverride("background", background);
        AddThemeStyleboxOverride("fill", foreground);

        _caption.AddThemeFontSizeOverride("font_size", 12);
        _caption.AddThemeConstantOverride("outline_size", 4);
        _caption.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(_caption);
        _caption.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Fills the bar and sets its caption.</summary>
    public void Display(BarReading reading)
    {
        Value = reading.Fraction;
        _caption.Text = reading.Text;
    }
}
