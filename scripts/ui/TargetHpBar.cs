using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Hud;

namespace Ragna.Ui;

/// <summary>
/// A small HP bar floating over a monster. <see cref="CombatFeedback"/> puts one over the player's
/// current target. It redraws only on <see cref="Health.Changed"/> and hides on
/// <see cref="Health.Died"/>; it never polls the HP. Add it as a child of the monster so it follows
/// it, and it goes away with the monster.
/// </summary>
public partial class TargetHpBar : Node3D
{
    private static readonly Color BackgroundColor = new(0.05f, 0.05f, 0.05f, 0.85f);
    private static readonly Color FillColor = new(0.3f, 0.85f, 0.3f);
    private static readonly Color LowColor = new(0.9f, 0.25f, 0.2f);

    // Below this much HP the fill turns from green to red.
    private const double LowFraction = 0.25;

    private Health? _health;
    private MeshInstance3D? _fill;
    private QuadMesh? _fillMesh;
    private StandardMaterial3D? _fillMaterial;

    /// <summary>Width of the bar in metres.</summary>
    [Export(PropertyHint.Range, "0.1,4,0.01,suffix:m")]
    public float BarWidth { get; set; } = 0.9f;

    /// <summary>Height of the bar in metres.</summary>
    [Export(PropertyHint.Range, "0.01,1,0.01,suffix:m")]
    public float BarHeight { get; set; } = 0.1f;

    /// <summary>The HP this bar shows.</summary>
    public Health? Health => _health;

    /// <summary>How full the bar is now, from 0 to 1.</summary>
    public double Fraction { get; private set; } = 1;

    /// <summary>Shows <paramref name="health"/>. Call once, before adding the bar to the tree.</summary>
    public void Bind(Health health)
    {
        ArgumentNullException.ThrowIfNull(health);
        _health = health;
    }

    public override void _Ready()
    {
        var border = 0.015f;
        AddChild(Quad(new Vector2(BarWidth + (border * 2f), BarHeight + (border * 2f)), BackgroundColor, 20, out _, out _));
        _fill = Quad(new Vector2(BarWidth, BarHeight), FillColor, 21, out _fillMesh, out _fillMaterial);
        AddChild(_fill);

        if (_health is null)
        {
            GD.PushWarning($"{Name}: no Health bound; the bar stays full.");
            return;
        }

        _health.Changed += Refresh;
        _health.Died += OnDied;
        Refresh();
        Visible = !_health.IsDead;
    }

    public override void _ExitTree()
    {
        if (_health is not null)
        {
            _health.Changed -= Refresh;
            _health.Died -= OnDied;
        }
    }

    private void Refresh()
    {
        if (_health is null || _fillMesh is null || _fill is null)
        {
            return;
        }

        var fill = WorldBarFill.For(_health, BarWidth);
        Fraction = fill.Fraction;
        _fill.Visible = fill.Width > 0f;
        _fillMesh.Size = new Vector2(Mathf.Max(fill.Width, 0.001f), BarHeight);

        // The offset is in the quad's own (billboarded) space, so the bar empties towards the left on screen.
        _fillMesh.CenterOffset = new Vector3(fill.CenterX, 0f, 0f);
        if (_fillMaterial is not null)
        {
            _fillMaterial.AlbedoColor = fill.Fraction <= LowFraction ? LowColor : FillColor;
        }
    }

    private void OnDied() => Visible = false;

    private static MeshInstance3D Quad(Vector2 size, Color color, int priority, out QuadMesh mesh, out StandardMaterial3D material)
    {
        material = new StandardMaterial3D
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            RenderPriority = priority,
        };
        mesh = new QuadMesh { Size = size, Material = material };
        return new MeshInstance3D
        {
            Mesh = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,

            // Layer 2 like the character sprites, so blob shadows don't paint on it.
            Layers = 2,
        };
    }
}
