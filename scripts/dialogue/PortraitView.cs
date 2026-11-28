using Godot;
using Ragna.Core.Dialogue;

namespace Ragna.Dialogue;

/// <summary>
/// One character's bust portrait on the dialogue stage. Loads
/// <c>res://assets/portraits/&lt;character&gt;/&lt;expression&gt;.png</c>, falling back to the character's
/// <c>neutral.png</c> and then to a drawn placeholder bust. Slides and fades in and out, mirrors itself to face the
/// middle of the screen, and dims and shrinks a little while someone else is speaking.
/// </summary>
public partial class PortraitView : Control
{
    public const string PortraitRoot = "res://assets/portraits";

    private const float SlideDistance = 90f;
    private const float FadeTime = 0.25f;
    private const float HighlightTime = 0.15f;
    private const float DimmedScale = 0.94f;
    private static readonly Color DimmedColor = new(0.45f, 0.45f, 0.5f);

    private Control? _art;
    private TextureRect? _texture;
    private PlaceholderPortrait? _placeholder;
    private Tween? _highlightTween;
    private bool _flip;

    public string Character { get; private set; } = "";

    public string Expression { get; private set; } = Expressions.Neutral;

    public StageSlot Slot { get; private set; }

    /// <summary>Sets up the portrait. Call before adding it to the tree.</summary>
    public void Setup(string character, StageSlot slot, string expression, bool flip, Vector2 size)
    {
        Character = character;
        Slot = slot;
        _flip = flip;
        MouseFilter = MouseFilterEnum.Ignore;

        // Stand on the bottom edge, centred on the slot's anchor.
        var anchor = StageSlots.Anchor(slot);
        AnchorLeft = anchor;
        AnchorRight = anchor;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = -size.X / 2;
        OffsetRight = size.X / 2;
        OffsetTop = -size.Y;
        OffsetBottom = 0;
        PivotOffset = new Vector2(size.X / 2, size.Y);

        _art = new Control { MouseFilter = MouseFilterEnum.Ignore, Size = size, PivotOffset = size / 2 };
        _art.Scale = new Vector2(StageSlots.FacesLeft(slot, flip) ? -1 : 1, 1);
        AddChild(_art);

        _texture = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = size,
        };
        _art.AddChild(_texture);

        _placeholder = new PlaceholderPortrait { MouseFilter = MouseFilterEnum.Ignore, Size = size, Character = character };
        _art.AddChild(_placeholder);

        SetExpression(expression);
    }

    /// <summary>Swaps the portrait to <paramref name="expression"/>.</summary>
    public void SetExpression(string expression)
    {
        Expression = expression;
        var texture = LoadPortrait(Character, expression);
        if (_texture is not null)
        {
            _texture.Texture = texture;
            _texture.Visible = texture is not null;
        }

        if (_placeholder is not null)
        {
            _placeholder.Visible = texture is null;
            _placeholder.Expression = expression;
        }
    }

    /// <summary>Slides in from the slot's side of the screen while fading in.</summary>
    public void AnimateIn()
    {
        if (_art is null)
        {
            return;
        }

        var home = _art.Position;
        _art.Position = home + new Vector2(StageSlots.EntrySide(Slot) * SlideDistance, 0);
        Modulate = Modulate with { A = 0 };
        var tween = CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_art, "position", home, FadeTime);
        tween.TweenProperty(this, "modulate:a", 1f, FadeTime);
    }

    /// <summary>Slides out towards the slot's side while fading, then frees itself.</summary>
    public void AnimateOutAndFree()
    {
        _highlightTween?.Kill();
        if (_art is null)
        {
            QueueFree();
            return;
        }

        var tween = CreateTween().SetParallel().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_art, "position", _art.Position + new Vector2(StageSlots.EntrySide(Slot) * SlideDistance, 0), FadeTime);
        tween.TweenProperty(this, "modulate:a", 0f, FadeTime);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>Full brightness and size while <paramref name="speaking"/>, otherwise dimmed and slightly smaller.</summary>
    public void SetSpeaking(bool speaking, bool instant = false)
    {
        if (_art is null)
        {
            return;
        }

        // The tint goes on the art and the fade on this node, so the two tweens never fight.
        var color = speaking ? Colors.White : DimmedColor;
        var scale = Vector2.One * (speaking ? 1f : DimmedScale);
        _highlightTween?.Kill();
        if (instant)
        {
            _art.Modulate = color;
            Scale = scale;
            return;
        }

        _highlightTween = CreateTween().SetParallel();
        _highlightTween.TweenProperty(_art, "modulate", color, HighlightTime);
        _highlightTween.TweenProperty(this, "scale", scale, HighlightTime);
    }

    private static Texture2D? LoadPortrait(string character, string expression)
    {
        foreach (var candidate in new[] { expression, Expressions.Neutral })
        {
            var path = $"{PortraitRoot}/{character}/{candidate}.png";
            if (ResourceLoader.Exists(path))
            {
                return GD.Load<Texture2D>(path);
            }
        }

        return null;
    }

    /// <summary>A drawn stand-in bust for characters without portrait art yet: a coloured silhouette with a simple face.</summary>
    private sealed partial class PlaceholderPortrait : Control
    {
        private string _expression = Expressions.Neutral;

        public string Character { get; set; } = "";

        public string Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            var size = Size;
            var hue = (float)((uint)StableHash(Character) % 360) / 360f;
            var body = Color.FromHsv(hue, 0.45f, 0.75f);
            var skin = new Color(0.96f, 0.84f, 0.72f);
            var ink = new Color(0.15f, 0.12f, 0.12f);
            var centre = size.X / 2;
            var headRadius = size.X * 0.2f;
            var head = new Vector2(centre, size.Y * 0.3f);

            // Shoulders and body.
            DrawColoredPolygon(
                [
                    new Vector2(centre - (size.X * 0.42f), size.Y),
                    new Vector2(centre - (size.X * 0.3f), size.Y * 0.55f),
                    new Vector2(centre + (size.X * 0.3f), size.Y * 0.55f),
                    new Vector2(centre + (size.X * 0.42f), size.Y),
                ],
                body);
            DrawCircle(head + new Vector2(0, -headRadius * 0.25f), headRadius * 1.08f, body.Darkened(0.35f));
            DrawCircle(head, headRadius, skin);

            // The art faces right: the nose points that way, so mirroring shows.
            DrawColoredPolygon(
                [head + new Vector2(headRadius * 0.55f, -2), head + new Vector2(headRadius * 0.95f, 10), head + new Vector2(headRadius * 0.55f, 12)],
                skin.Darkened(0.15f));

            var eyeY = head.Y - (headRadius * 0.15f);
            var leftEye = new Vector2(centre - (headRadius * 0.15f), eyeY);
            var rightEye = new Vector2(centre + (headRadius * 0.4f), eyeY);
            var mouth = new Vector2(centre + (headRadius * 0.15f), head.Y + (headRadius * 0.45f));
            var eyeRadius = Expression == Expressions.Surprised ? 7f : 5f;
            DrawCircle(leftEye, eyeRadius, ink);
            DrawCircle(rightEye, eyeRadius, ink);

            switch (Expression)
            {
                case Expressions.Happy:
                    DrawArc(mouth + new Vector2(0, -10), 18, 0.2f, Mathf.Pi - 0.2f, 12, ink, 4);
                    break;
                case Expressions.Sad:
                    DrawArc(mouth + new Vector2(0, 12), 16, Mathf.Pi + 0.4f, (2 * Mathf.Pi) - 0.4f, 12, ink, 4);
                    break;
                case Expressions.Angry:
                    DrawLine(leftEye + new Vector2(-12, -16), leftEye + new Vector2(10, -8), ink, 4);
                    DrawLine(rightEye + new Vector2(12, -16), rightEye + new Vector2(-10, -8), ink, 4);
                    DrawLine(mouth + new Vector2(-14, 2), mouth + new Vector2(14, 2), ink, 4);
                    break;
                case Expressions.Surprised:
                    DrawArc(mouth, 9, 0, Mathf.Tau, 16, ink, 4);
                    break;
                default:
                    DrawLine(mouth + new Vector2(-14, 0), mouth + new Vector2(14, 0), ink, 4);
                    break;
            }
        }

        // string.GetHashCode is randomised per run; this keeps each character's colour the same.
        private static int StableHash(string text)
        {
            var hash = 17;
            foreach (var c in text)
            {
                hash = unchecked((hash * 31) + c);
            }

            return hash & int.MaxValue;
        }
    }
}
