namespace Ragna.Core.Items;

/// <summary><paramref name="Quantity"/> of <paramref name="Item"/> in one inventory slot.</summary>
public readonly record struct ItemStack(ItemDefinition Item, int Quantity);

/// <summary>
/// A grid of <see cref="Columns"/> × <see cref="Rows"/> slots. Each slot is empty or holds one
/// stack of a single item type, up to that item's <see cref="ItemDefinition.MaxStack"/>. Slots are
/// numbered row by row from the top left, starting at 0.
/// </summary>
/// <remarks>
/// Adding fills stacks of the same item first, then empty slots in order. Items are the same type
/// when their ids match, ignoring case (like <see cref="ItemDatabase"/>).
/// </remarks>
public sealed class Inventory
{
    private readonly ItemStack?[] _slots;

    public Inventory(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        Columns = columns;
        Rows = rows;
        _slots = new ItemStack?[columns * rows];
    }

    /// <summary>Raised after any change to the slots.</summary>
    public event Action? Changed;

    public int Columns { get; }

    public int Rows { get; }

    /// <summary>Number of slots.</summary>
    public int Capacity => _slots.Length;

    /// <summary>Every slot in order; <see langword="null"/> is an empty slot.</summary>
    public IReadOnlyList<ItemStack?> Slots => _slots;

    /// <summary>Number of empty slots.</summary>
    public int EmptySlots => _slots.Count(s => s is null);

    /// <summary>Whether every slot holds something. A full inventory can still take more of an item whose stack isn't full.</summary>
    public bool IsFull => EmptySlots == 0;

    /// <summary>The stack in <paramref name="slot"/>, or <see langword="null"/> if it is empty.</summary>
    public ItemStack? this[int slot] => _slots[CheckSlot(slot)];

    /// <summary>The stack at <paramref name="column"/>, <paramref name="row"/>.</summary>
    public ItemStack? this[int column, int row] => _slots[SlotAt(column, row)];

    /// <summary>The slot number of <paramref name="column"/>, <paramref name="row"/>.</summary>
    public int SlotAt(int column, int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        return (row * Columns) + column;
    }

    /// <summary>How many of <paramref name="itemId"/> the inventory holds, over all slots.</summary>
    public int Count(string itemId) => _slots.Where(s => s is { } stack && Same(stack.Item, itemId)).Sum(s => s!.Value.Quantity);

    /// <summary>How many more of <paramref name="item"/> fit: room in its stacks plus empty slots.</summary>
    public int SpaceFor(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var space = 0L;
        foreach (var slot in _slots)
        {
            if (slot is null)
            {
                space += item.MaxStack;
            }
            else if (Same(slot.Value.Item, item.Id))
            {
                space += Math.Max(0, item.MaxStack - slot.Value.Quantity);
            }
        }

        return (int)Math.Min(space, int.MaxValue);
    }

    /// <summary>
    /// Adds as many of <paramref name="quantity"/> × <paramref name="item"/> as fit: first onto
    /// stacks of the same item, then into empty slots in order.
    /// </summary>
    /// <returns>How many didn't fit. 0 when all were added.</returns>
    public int Add(ItemDefinition item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        var left = quantity;

        for (var i = 0; i < _slots.Length && left > 0; i++)
        {
            if (_slots[i] is { } stack && Same(stack.Item, item.Id) && stack.Quantity < item.MaxStack)
            {
                var moved = Math.Min(left, item.MaxStack - stack.Quantity);
                _slots[i] = stack with { Quantity = stack.Quantity + moved };
                left -= moved;
            }
        }

        for (var i = 0; i < _slots.Length && left > 0; i++)
        {
            if (_slots[i] is null)
            {
                var moved = Math.Min(left, item.MaxStack);
                _slots[i] = new ItemStack(item, moved);
                left -= moved;
            }
        }

        if (left != quantity)
        {
            Changed?.Invoke();
        }

        return left;
    }

    /// <summary>Adds all of them if they all fit, otherwise nothing.</summary>
    /// <returns>Whether they were added.</returns>
    public bool TryAddAll(ItemDefinition item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        if (SpaceFor(item) < quantity)
        {
            return false;
        }

        Add(item, quantity);
        return true;
    }

    /// <summary>
    /// Removes up to <paramref name="quantity"/> of <paramref name="itemId"/>, taking from the last
    /// slots first so the first stacks stay full.
    /// </summary>
    /// <returns>How many were removed (fewer than asked if the inventory held fewer).</returns>
    public int Remove(string itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        var removed = 0;
        for (var i = _slots.Length - 1; i >= 0 && removed < quantity; i--)
        {
            if (_slots[i] is { } stack && Same(stack.Item, itemId))
            {
                removed += Take(i, stack, quantity - removed);
            }
        }

        if (removed > 0)
        {
            Changed?.Invoke();
        }

        return removed;
    }

    /// <summary>Removes up to <paramref name="quantity"/> from <paramref name="slot"/>.</summary>
    /// <returns>How many were removed. 0 if the slot was empty.</returns>
    public int RemoveAt(int slot, int quantity)
    {
        CheckSlot(slot);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        if (_slots[slot] is not { } stack)
        {
            return 0;
        }

        var removed = Take(slot, stack, quantity);
        Changed?.Invoke();
        return removed;
    }

    /// <summary>
    /// Moves the stack in <paramref name="from"/> onto <paramref name="to"/>, as when dragging in
    /// the inventory window. Onto an empty slot it moves; onto the same item it merges as much as
    /// fits and leaves the rest behind; onto a different item the two swap.
    /// </summary>
    /// <returns>Whether anything changed.</returns>
    public bool Move(int from, int to)
    {
        CheckSlot(from);
        CheckSlot(to);
        if (from == to || _slots[from] is not { } moving)
        {
            return false;
        }

        if (_slots[to] is { } target && Same(target.Item, moving.Item.Id))
        {
            var moved = Math.Min(moving.Quantity, target.Item.MaxStack - target.Quantity);
            if (moved <= 0)
            {
                return false;
            }

            _slots[to] = target with { Quantity = target.Quantity + moved };
            _slots[from] = moving.Quantity == moved ? null : moving with { Quantity = moving.Quantity - moved };
        }
        else
        {
            (_slots[from], _slots[to]) = (_slots[to], _slots[from]);
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Empties every slot.</summary>
    public void Clear()
    {
        Array.Clear(_slots);
        Changed?.Invoke();
    }

    private int Take(int slot, ItemStack stack, int quantity)
    {
        var taken = Math.Min(quantity, stack.Quantity);
        _slots[slot] = stack.Quantity == taken ? null : stack with { Quantity = stack.Quantity - taken };
        return taken;
    }

    private int CheckSlot(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _slots.Length);
        return slot;
    }

    private static bool Same(ItemDefinition item, string id) => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);
}
