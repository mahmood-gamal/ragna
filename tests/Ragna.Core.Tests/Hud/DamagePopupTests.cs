using Ragna.Core.Combat;
using Ragna.Core.Hud;

namespace Ragna.Core.Tests.Hud;

public sealed class DamagePopupTests
{
    [Fact]
    public void A_hit_on_a_target_shows_the_damage()
    {
        var popup = DamagePopup.OnTarget(new AttackResult(AttackOutcome.Hit, 42));

        Assert.Equal(new DamagePopup(PopupStyle.DamageDealt, "42", 1f), popup);
    }

    [Fact]
    public void A_critical_on_a_target_is_bigger_and_styled_as_critical()
    {
        var popup = DamagePopup.OnTarget(new AttackResult(AttackOutcome.Critical, 1234));

        Assert.Equal(PopupStyle.Critical, popup.Style);
        Assert.Equal("1234", popup.Text);
        Assert.Equal(DamagePopup.CriticalScale, popup.Scale);
        Assert.True(popup.Scale > 1f);
    }

    [Fact]
    public void A_miss_says_miss_either_way()
    {
        Assert.Equal(new DamagePopup(PopupStyle.Miss, "Miss", 1f), DamagePopup.OnTarget(AttackResult.Miss));
        Assert.Equal(new DamagePopup(PopupStyle.Miss, "Miss", 1f), DamagePopup.OnPlayer(AttackResult.Miss));
    }

    [Fact]
    public void Damage_to_the_player_is_styled_as_damage_taken()
    {
        Assert.Equal(new DamagePopup(PopupStyle.DamageTaken, "7", 1f), DamagePopup.OnPlayer(new AttackResult(AttackOutcome.Hit, 7)));

        var critical = DamagePopup.OnPlayer(new AttackResult(AttackOutcome.Critical, 30));
        Assert.Equal(PopupStyle.DamageTaken, critical.Style);
        Assert.Equal(DamagePopup.CriticalScale, critical.Scale);
    }

    [Fact]
    public void Numbers_use_no_thousands_separator()
    {
        Assert.Equal("10000", DamagePopup.OnTarget(new AttackResult(AttackOutcome.Hit, 10000)).Text);
    }
}
