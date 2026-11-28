using Godot;
using Ragna.Gameplay.Characters;

namespace Ragna.Gameplay.Npcs;

/// <summary>
/// A character the player can talk to. Left-click it: the player walks over and its dialogue scene,
/// <c>data/dialogue/&lt;DialogueId&gt;.json</c>, plays (see <see cref="PlayerTalk"/>). It sits on the
/// <c>npcs</c> physics layer (layer 5) so the click ray can find it, shows <see cref="DisplayName"/>
/// above its head, and turns to face the player while they talk.
/// </summary>
public partial class Npc : StaticBody3D
{
    /// <summary>Physics layer bit of the <c>npcs</c> layer (layer 5).</summary>
    public const uint NpcLayer = 1u << 4;

    private Label3D? _label;
    private CharacterSprite? _sprite;

    /// <summary>The dialogue script to play, <c>data/dialogue/&lt;DialogueId&gt;.json</c>.</summary>
    [Export]
    public string DialogueId { get; set; } = "";

    /// <summary>Name shown above the NPC.</summary>
    [Export]
    public string DisplayName { get; set; } = "";

    /// <summary>Tint for the placeholder sprite, so NPCs don't look like the player.</summary>
    [Export]
    public Color Tint { get; set; } = Colors.White;

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label3D>("Label");
        _sprite = GetNodeOrNull<CharacterSprite>("Visual/Sprite");
        if (_label is not null)
        {
            _label.Text = DisplayName;
            _label.Visible = !string.IsNullOrEmpty(DisplayName);
        }

        if (_sprite is not null)
        {
            _sprite.Modulate = Tint;
        }

        if (string.IsNullOrEmpty(DialogueId))
        {
            GD.PushWarning($"{Name} has no DialogueId; talking to it does nothing.");
        }
    }

    /// <summary>Turns the NPC towards <paramref name="point"/>, for example the player who talks to it.</summary>
    public void FaceTowards(Vector3 point) => _sprite?.Face(point - GlobalPosition);
}
