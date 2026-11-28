using System.Numerics;
using Ragna.Core.Items;

namespace Ragna.Core.Tests.Items;

public sealed class PickupOrderTests
{
    private static readonly Vector3 Item = new(10f, 0f, 0f);

    [Fact]
    public void Does_nothing_without_an_order()
    {
        var order = new PickupOrder();

        Assert.Equal(new PickupStep(PickupAction.None), order.Tick(Vector3.Zero, Item, walking: false));
    }

    [Fact]
    public void Asks_for_a_path_once_then_walks()
    {
        var order = new PickupOrder();
        order.Start();

        Assert.Equal(new PickupStep(PickupAction.Walk, Repath: true), order.Tick(Vector3.Zero, Item, walking: false));
        Assert.Equal(new PickupStep(PickupAction.Walk), order.Tick(new Vector3(2f, 0f, 0f), Item, walking: true));
    }

    [Fact]
    public void Picks_up_in_reach_and_ends_the_order()
    {
        var order = new PickupOrder(reach: 1f);
        order.Start();
        order.Tick(Vector3.Zero, Item, walking: false);

        Assert.Equal(PickupAction.PickUp, order.Tick(new Vector3(9.1f, 0f, 0f), Item, walking: true).Action);
        Assert.False(order.IsActive);
        Assert.Equal(PickupAction.None, order.Tick(new Vector3(9.1f, 0f, 0f), Item, walking: true).Action);
    }

    [Fact]
    public void Picks_up_straight_away_when_already_in_reach()
    {
        var order = new PickupOrder(reach: 1f);
        order.Start();

        Assert.Equal(PickupAction.PickUp, order.Tick(new Vector3(9.5f, 0f, 0f), Item, walking: false).Action);
    }

    [Fact]
    public void Height_does_not_count()
    {
        var order = new PickupOrder(reach: 1f);
        order.Start();

        Assert.Equal(PickupAction.PickUp, order.Tick(new Vector3(10f, 3f, 0.5f), Item, walking: false).Action);
    }

    [Fact]
    public void Gives_up_when_the_walk_ends_out_of_reach()
    {
        var order = new PickupOrder(reach: 1f);
        order.Start();
        order.Tick(Vector3.Zero, Item, walking: false);

        Assert.Equal(PickupAction.GiveUp, order.Tick(new Vector3(7f, 0f, 0f), Item, walking: false).Action);
        Assert.False(order.IsActive);
    }

    [Fact]
    public void Cancel_ends_the_order()
    {
        var order = new PickupOrder();
        order.Start();
        order.Cancel();

        Assert.False(order.IsActive);
        Assert.Equal(PickupAction.None, order.Tick(Vector3.Zero, Item, walking: false).Action);
    }

    [Fact]
    public void A_new_order_asks_for_a_new_path()
    {
        var order = new PickupOrder();
        order.Start();
        order.Tick(Vector3.Zero, Item, walking: false);
        order.Start();

        Assert.True(order.Tick(Vector3.Zero, new Vector3(0f, 0f, 5f), walking: true).Repath);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void Rejects_bad_reach(float reach)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PickupOrder(reach));
    }
}
