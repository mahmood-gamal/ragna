using Godot;
using Ragna.Core.Items;

namespace Ragna.Gameplay.Items;

/// <summary>
/// An item lying on the ground, dropped by a monster. It falls in an arc from where the monster
/// died to its landing spot, shows its name, and can be clicked so the player walks over and picks
/// it up (see <see cref="PlayerPickup"/>). It sits on the <c>items</c> physics layer (layer 4) so the
/// click ray can find it, and disappears by itself after <see cref="Lifetime"/>.
/// </summary>
public partial class GroundItem : StaticBody3D
{
    /// <summary>Physics layer bit of the <c>items</c> layer (layer 4).</summary>
    public const uint ItemLayer = 1u << 3;

    private Sprite3D? _sprite;
    private Label3D? _label;
    private double _age;

    /// <summary>Seconds it takes to fall from the monster to the ground.</summary>
    [Export(PropertyHint.Range, "0,2,0.05,suffix:s")]
    public float FallTime { get; set; } = 0.45f;

    /// <summary>How high the arc goes above the start and landing spots.</summary>
    [Export(PropertyHint.Range, "0,3,0.05,suffix:m")]
    public float FallHeight { get; set; } = 0.8f;

    /// <summary>Seconds before an item nobody picks up disappears. 0 keeps it forever.</summary>
    [Export(PropertyHint.Range, "0,600,1,suffix:s")]
    public float Lifetime { get; set; } = 120f;

    public ItemDefinition Item { get; private set; } = new() { Id = "item", Name = "Item" };

    public int Quantity { get; private set; } = 1;

    /// <summary>Where it lies once it has landed. The player walks here to pick it up.</summary>
    public Vector3 RestPosition { get; private set; }

    /// <summary>Whether it has been picked up (it is on its way out of the tree).</summary>
    public bool IsTaken { get; private set; }

    /// <summary>Name shown above the item, for example <c>Slime Gel x2</c>.</summary>
    public string Caption => Quantity > 1 ? $"{Item.Name} x{Quantity}" : Item.Name;

    /// <summary>Sets what this is. Call before adding it to the tree.</summary>
    public void Setup(ItemDefinition item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        Item = item;
        Quantity = quantity;
    }

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite3D>("Sprite");
        _label = GetNodeOrNull<Label3D>("Label");
        RestPosition = GlobalPosition;
        if (_sprite is not null && Ragna.Core.Monsters.MonsterDefinition.TryParseTint(Item.Tint, out var r, out var g, out var b))
        {
            _sprite.Modulate = new Color(r, g, b);
        }

        if (_label is not null)
        {
            _label.Text = Caption;
        }
    }

    public override void _Process(double delta)
    {
        if (Lifetime <= 0f || IsTaken)
        {
            return;
        }

        _age += delta;
        if (_age >= Lifetime)
        {
            IsTaken = true;
            CollisionLayer = 0;
            QueueFree();
        }
    }

    /// <summary>Drops it from <paramref name="from"/> in an arc onto <paramref name="landing"/>. Call after adding it to the tree.</summary>
    public void Fall(Vector3 from, Vector3 landing)
    {
        RestPosition = landing;
        GlobalPosition = from;
        if (FallTime <= 0f)
        {
            GlobalPosition = landing;
            return;
        }

        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                var position = from.Lerp(landing, t);
                position.Y += FallHeight * 4f * t * (1f - t);
                GlobalPosition = position;
            }),
            0f,
            1f,
            FallTime);
    }

    /// <summary>Takes the item off the ground. Returns <c>false</c> if someone already took it.</summary>
    public bool PickUp()
    {
        if (IsTaken)
        {
            return false;
        }

        IsTaken = true;
        CollisionLayer = 0;
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "global_position:y", GlobalPosition.Y + 0.6f, 0.2f);
        if (_sprite is not null)
        {
            tween.TweenProperty(_sprite, "modulate:a", 0f, 0.2f);
        }

        if (_label is not null)
        {
            tween.TweenProperty(_label, "modulate:a", 0f, 0.2f);
        }

        tween.Chain().TweenCallback(Callable.From(QueueFree));
        return true;
    }
}
