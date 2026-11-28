using Godot;
using Ragna.Core.Dialogue;
using Ragna.Core.Save;
using Ragna.Dialogue;
using Ragna.Gameplay.Movement;
using Ragna.Gameplay.Save;

namespace Ragna.Gameplay.Npcs;

/// <summary>
/// An area that plays a dialogue scene, <c>data/dialogue/&lt;DialogueId&gt;.json</c>, when the player walks in.
/// By default it plays once: it sets the dialogue flag <c>trigger.&lt;DialogueId&gt;</c> (or <see cref="OnceFlag"/>),
/// which is saved with the game, so it doesn't play again after a load. With <see cref="RequiredFlag"/> it waits
/// until that flag equals <see cref="RequiredValue"/>. The rules live in <see cref="StoryTriggerRule"/>.
/// </summary>
/// <remarks>Give it a <see cref="CollisionShape3D"/> child. It watches the player layer (layer 2).</remarks>
public partial class StoryTrigger : Area3D
{
    /// <summary>The dialogue script to play.</summary>
    [Export]
    public string DialogueId { get; set; } = "";

    /// <summary>Whether it plays only once per game (remembered in the save).</summary>
    [Export]
    public bool Once { get; set; } = true;

    /// <summary>Flag that remembers it has played. Empty uses <c>trigger.&lt;DialogueId&gt;</c>.</summary>
    [Export]
    public string OnceFlag { get; set; } = "";

    /// <summary>Flag that must equal <see cref="RequiredValue"/> before it plays. Empty means none.</summary>
    [Export]
    public string RequiredFlag { get; set; } = "";

    [Export]
    public bool RequiredValue { get; set; } = true;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1u << 1;
        Monitorable = false;
        BodyEntered += OnBodyEntered;
        if (string.IsNullOrEmpty(DialogueId))
        {
            GD.PushWarning($"{Name} has no DialogueId; it does nothing.");
        }
    }

    /// <summary>Plays the dialogue now if the rule allows it. Returns whether it started.</summary>
    public bool TryPlay(ClickToMoveController? player = null)
    {
        if (string.IsNullOrEmpty(DialogueId) || DialogueScene.Instance is not { IsPlaying: false } scene)
        {
            return false;
        }

        var flags = SaveService.Instance?.Flags ?? new DialogueFlags();
        var rule = new StoryTriggerRule(DialogueId, Once, OnceFlag, RequiredFlag, RequiredValue);
        if (!rule.ShouldPlay(flags))
        {
            return false;
        }

        player?.StopMoving();
        if (!scene.Play(DialogueId))
        {
            return false;
        }

        rule.MarkPlayed(flags);
        return true;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is ClickToMoveController player)
        {
            TryPlay(player);
        }
    }
}
