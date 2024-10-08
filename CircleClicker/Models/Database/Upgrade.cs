using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CircleClicker.Utils;

namespace CircleClicker.Models.Database;

/// <summary>
/// A purchase that can boost a single <see cref="IStat"/>.
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

    /// <summary>
    /// The stat this upgrade affects.<br />
    /// May return <see langword="null"/> if the stat no longer exists.
    /// </summary>
    [NotMapped, JsonIgnore, MaybeNull]
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

    [JsonIgnore]
    public string? DesignDescription
    {
        private get;
        [Obsolete("This setter may only be used in design instances.", true)]
        set;
    }

    [NotMapped]
    public override string Description
    {
        get
        {
            if (DesignDescription != null)
                return DesignDescription;

            string effectPart;
            if (Amount == 0)
            {
                effectPart =
                    $"<font color=\"res:ForegroundBrush\">{GetEffect(Amount + ClampedBulkBuy).FormatSuffixes()}</font>";
            }
            else if (
                (MaxAmount > 0 && Amount == MaxAmount && ClampedBulkBuy > 0)
                || ClampedBulkBuy == 0
            )
            {
                effectPart =
                    $"<font color=\"res:ForegroundBrush\">{Effect.FormatSuffixes()}</font>";
            }
            else
            {
                effectPart =
                    $"{Effect.FormatSuffixes()} ➝ <font color=\"res:ForegroundBrush\">{GetEffect(Amount + ClampedBulkBuy).FormatSuffixes()}</font>";
            }

            return string.Format(App.Culture, Affects?.Description ?? "", effectPart);
        }
    }
    #endregion

    #region Values for CurrentSave
    public override bool IsUnlocked => Affects != null && base.IsUnlocked;

    [JsonIgnore]
    public override int Amount
    {
        get => base.Amount;
        set
        {
            base.Amount = value;
            NotifyPropertyChanged(nameof(IsPurchased), nameof(Effect), nameof(Description));
            if (Affects == Stat.Production || Affects is Building)
            {
                Models.Currency.Circles.NotifyPropertyChanged(nameof(Models.Currency.Production));
                foreach (Building building in Main.Instance.Buildings)
                {
                    building.NotifyPropertyChanged(
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
