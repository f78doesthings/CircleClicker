using CircleClicker.Utils;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CircleClicker.Models.Database;

/// <summary>
/// A purchase that can currently boost a single <see cref="IStat"/>.
/// </summary>
public partial class Upgrade : Purchase
{
    #region Model
    private double _baseEffect;

    public string AffectedId
    {
        get => Affects?.StatId ?? "";
        set => Affects = IStat.Instances.Find(v => v.StatId == value)!;
    }

    /// <summary>
    /// The base effect this upgrade has on the affected stat.
    /// </summary>
    public double BaseEffect
    {
        get => _baseEffect;
        set
        {
            _baseEffect = value;
            OnPropertyChanged([nameof(Effect), nameof(Description)]);
        }
    }
    #endregion

    #region Stat references
    [MaybeNull]
    private IStat _affects;
    private string? _designDescription;

    /// <summary>
    /// The stat this upgrade affects.<br />
    /// May return <see langword="null"/> if the stat no longer exists.
    /// </summary>
    [NotMapped, MaybeNull]
    public IStat Affects
    {
        get => _affects; // IStat.Instances.Find(v => v.StatId == AffectedId);
        set
        {
            _affects = value;
            OnPropertyChanged(
                [nameof(Affects), nameof(Description), nameof(IsUnlocked), nameof(CanAfford)]
            );
        }
    }

    /// <summary>
    /// The description of this upgrade.
    /// </summary>
    [NotMapped]
    public string Description
    {
        get =>
            _designDescription
            ?? string.Format(
                App.Culture,
                Affects?.Description ?? "",
                Amount == 0
                    ? GetEffect(Amount + 1).FormatSuffixes()
                    : MaxAmount > 0 && Amount == MaxAmount
                        ? Effect.FormatSuffixes()
                        : $"{Effect.FormatSuffixes()} ➝ {GetEffect(Amount + 1).FormatSuffixes()}"
            );
        [Obsolete("This setter only exists for use in design instances.", true)]
        set => _designDescription = value;
    }
    #endregion

    #region Values for CurrentSave
    public override bool IsUnlocked => Affects != null && base.IsUnlocked;

    public override int Amount
    {
        get => base.Amount;
        set
        {
            base.Amount = value;
            InvokePropertyChanged(nameof(IsPurchased), nameof(Effect), nameof(Description));
            if (Affects == Stat.Production || Affects is Building)
            {
                Models.Currency.Circles.InvokePropertyChanged(nameof(Models.Currency.Production));
                foreach (Building building in Main.Instance.Buildings)
                {
                    building.InvokePropertyChanged(
                        nameof(Building.Value),
                        nameof(Building.Production)
                    );
                }
            }
        }
    }

    public bool IsPurchased => Amount > 0;

    /// <summary>
    /// The current effect this upgrade has on the affected stat.<br />
    /// Make sure to check if <see cref="Purchase.Amount"/> is greater than 0 before using this.
    /// </summary>
    public double Effect => GetEffect(Amount);

    /// <summary>
    /// Returns the effect at the given level.
    /// </summary>
    private double GetEffect(int level)
    {
        return Affects?.IsAdditive == true ? BaseEffect * level : Math.Pow(BaseEffect, level);
    }
    #endregion

    internal override void CopyTo(Purchase other)
    {
        base.CopyTo(other);

        if (other is Upgrade upgrade)
        {
            upgrade.Affects = Affects!;
            upgrade.BaseEffect = BaseEffect;
        }
    }
}
