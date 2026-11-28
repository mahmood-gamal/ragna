using Godot;
using Ragna.Core.Hud;

namespace Ragna.Ui;

/// <summary>
/// One floating damage number. It pops in over whoever was hit, rises, drifts to one side, fades
/// and frees itself. Spawned by <see cref="CombatFeedback"/>; the animation lives in
/// <see cref="FloatingNumberMotion"/>.
/// </summary>
public partial class DamageNumber : Label3D
{
    private static readonly Dictionary<PopupStyle, Color> StyleColors = new()
    {
        [PopupStyle.DamageDealt] = new Color(1f, 1f, 1f),
        [PopupStyle.Critical] = new Color(1f, 0.82f, 0.2f),
        [PopupStyle.DamageTaken] = new Color(1f, 0.38f, 0.32f),
        [PopupStyle.Miss] = new Color(0.62f, 0.82f, 1f),
    };

    private FloatingNumberMotion _motion = new();
    private Vector3 _origin;
    private Color _color = Colors.White;
    private float _size = 1f;
    private int _driftSign;
    private float _elapsed;

    /// <summary>
    /// Sets what the number says and where it starts, in world space. Call before adding it to the
    /// tree. <paramref name="driftSign"/> picks the side it drifts to.
    /// </summary>
    public void Setup(DamagePopup popup, Vector3 origin, int driftSign, FloatingNumberMotion? motion = null)
    {
        _motion = motion ?? new FloatingNumberMotion();
        _origin = origin;
        _driftSign = driftSign;
        _size = popup.Scale;
        _color = StyleColors[popup.Style];
        Text = popup.Text;

        // World-space, so it stays where the hit was even if the monster moves or is freed.
        TopLevel = true;
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        NoDepthTest = true;
        Shaded = false;
        DoubleSided = true;
        FixedSize = false;
        PixelSize = 0.008f;
        FontSize = 56;
        OutlineSize = 14;
        OutlineModulate = new Color(0f, 0f, 0f, 0.9f);
        RenderPriority = 10;
        OutlineRenderPriority = 9;
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear;

        // Layer 2 like the character sprites, so blob shadows don't paint on it.
        Layers = 2;
    }

    public override void _Ready() => Apply(_motion.Sample(0f, _driftSign));

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_motion.IsFinished(_elapsed))
        {
            QueueFree();
            return;
        }

        Apply(_motion.Sample(_elapsed, _driftSign));
    }

    private void Apply(FloatingNumberSample sample)
    {
        var camera = GetViewport()?.GetCamera3D();

        // Drift along the screen's horizontal, whichever way the camera faces.
        var right = camera?.GlobalBasis.X ?? Vector3.Right;
        right.Y = 0f;
        right = right.LengthSquared() > 1e-6f ? right.Normalized() : Vector3.Right;

        GlobalPosition = _origin + (right * sample.OffsetX) + (Vector3.Up * sample.OffsetY);
        Scale = Vector3.One * (sample.Scale * _size);
        Modulate = _color with { A = sample.Alpha };
        OutlineModulate = OutlineModulate with { A = 0.9f * sample.Alpha };
    }
}
