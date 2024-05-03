using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CircleClicker.Utils;

namespace CircleClicker.Models.Database;

/// <summary>
/// The base class for something that can be purchased.
/// </summary>
public abstract partial class Purchase : NotifyPropertyChanged, IDependency
{
    #region Model
    public int Id { get; set; }

    private string _name = "";

    /// <summary>
    /// The display name of the purchase.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The <see cref="IReadOnlyDependency.DependencyId"/> of <see cref="Requires"/>. This is what is saved to the database.
    /// </summary>
    public string? RequiredId
    {
        get => Requires?.DependencyId;
        set => Requires = IReadOnlyDependency.Instances.Find(v => v.DependencyId == value);
    }

    private double _requiredAmount;

    /// <summary>
    /// The base amount needed to unlock this purchase.
    /// </summary>
    public double BaseRequirement
    {
        get => _requiredAmount;
        set
        {
            _requiredAmount = value;
            OnPropertyChanged([nameof(IsUnlocked), nameof(CanAfford)]);
        }
    }

    private double _requirementScaling;

    /// <summary>
    /// If <see cref="RequirementAdditive"/> is <see langword="true"/>, how much is added to the requirement with each level.<br />
    /// Otherwise, how much the requirement is multiplied by with each level.
    /// </summary>
    public double RequirementScaling
    {
        get => _requirementScaling;
        set
        {
            _requirementScaling = value;
            OnPropertyChanged([nameof(IsUnlocked), nameof(CanAfford)]);
        }
    }

    private bool _requirementAdditive;

    /// <summary>
    /// Whether the requirement scales linearly or exponentially.
    /// </summary>
    public bool RequirementAdditive
    {
        get => _requirementAdditive;
        set
        {
            _requirementAdditive = value;
            OnPropertyChanged([nameof(IsUnlocked), nameof(CanAfford)]);
        }
    }

    /// <summary>
    /// The <see cref="IReadOnlyDependency.DependencyId"/> of <see cref="Currency"/>. This is what is saved to the database.
    /// </summary>
    public string CurrencyId
    {
        get => Currency?.DependencyId ?? "";
        set => Currency = IDependency.Instances.Find(v => v.DependencyId == value)!;
    }

    private double _baseCost;

    /// <summary>
    /// The base cost of the purchase.
    /// </summary>
    public double BaseCost
    {
        get => _baseCost;
        set
        {
            _baseCost = value;
            OnPropertyChanged([nameof(Cost), nameof(CostText), nameof(CanAfford)]);
        }
    }

    private double _costScaling;

    /// <summary>
    /// The amount the cost is multiplied by for every single one of this purchase owned.
    /// </summary>
    public double CostScaling
    {
        get => _costScaling;
        set
        {
            _costScaling = value;
            OnPropertyChanged([nameof(Cost), nameof(CostText), nameof(CanAfford)]);
        }
    }

    private int _maxAmount;

    /// <summary>
    /// The maximum amount of this purchase you can have.
    /// </summary>
    public int MaxAmount
    {
        get => _maxAmount;
        set
        {
            _maxAmount = value;
            OnPropertyChanged();

            // This will make sure that Amount does not exceed MaxAmount after changing it
#pragma warning disable CA2245 // Do not assign a property to itself
            Amount = Amount;
#pragma warning restore CA2245 // Do not assign a property to itself
        }
    }
    #endregion

    #region Dependency references
    private IReadOnlyDependency? _requires;

    /// <summary>
    /// The dependency that needs to be above <see cref="BaseRequirement"/> to be able to purchase this upgrade.<br />
    /// Can be <see langword="null"/>, in which case this purchase will have no requirement.
    /// </summary>
    [NotMapped]
    public IReadOnlyDependency? Requires
    {
        get => _requires;
        set
        {
            _requires = value;
            OnPropertyChanged([nameof(Requires), nameof(IsUnlocked), nameof(CanAfford)]);
        }
    }

    [MaybeNull]
    private IDependency _currency;

    /// <summary>
    /// The <see cref="IDependency"/> used to purchase this upgrade.<br />
    /// May return <see langword="null"/> if the currency no longer exists.
    /// </summary>
    [NotMapped, MaybeNull]
    public IDependency Currency
    {
        get => _currency;
        set
        {
            _currency = value;
            OnPropertyChanged(
                [nameof(Currency), nameof(CostText), nameof(IsUnlocked), nameof(CanAfford)]
            );
        }
    }

    public string? CostText => Currency?.Format(Cost);
    #endregion

    #region Values for CurrentSave
    /// <summary>
    /// Returns the current amount needed to unlock this purchase.
    /// </summary>
    public double Requirement =>
        RequirementAdditive
            ? BaseRequirement + RequirementScaling * Amount
            : BaseRequirement * Math.Pow(RequirementScaling, Amount);

    /// <summary>
    /// Whether this purchase is currently available for purchase.
    /// </summary>
    public virtual bool IsUnlocked =>
        Currency != null
        && (MaxAmount <= 0 || Amount < MaxAmount)
        && (Requires == null || Requires.Value >= Requirement);

    /// <summary>
    /// Whether this purchase can currently be afforded.
    /// </summary>
    public bool CanAfford => IsUnlocked && Currency?.Value >= Cost;

    /// <summary>
    /// The current cost of this purchase.
    /// </summary>
    public double Cost => BaseCost * Math.Pow(CostScaling, Amount);

    /// <summary>
    /// The current amount of this purchase owned.
    /// </summary>
    [NotMapped]
    public virtual int Amount
    {
        get =>
            Main.Instance.CurrentSave?.OwnedPurchases?.FirstOrDefault(v =>
                v.Purchase == this
            )?.Amount ?? 0;
        set
        {
            if (Main.Instance.CurrentSave == null)
            {
                return;
            }

            OwnedPurchase? owned = Main.Instance.CurrentSave.OwnedPurchases.FirstOrDefault(v =>
                v.Purchase == this
            );
            if (owned == null)
            {
                owned = new() { Save = Main.Instance.CurrentSave, Purchase = this, };
                Main.Instance.CurrentSave.OwnedPurchases.Add(owned);
            }

            if (MaxAmount > 0)
            {
                value = Math.Min(value, MaxAmount);
            }
            owned.Amount = Math.Max(value, 0);
            OnPropertyChanged(
                [
                    nameof(AmountText),
                    nameof(Cost),
                    nameof(CostText),
                    nameof(IsUnlocked),
                    nameof(CanAfford)
                ]
            );
        }
    }

    public string AmountText =>
        MaxAmount > 1
            ? $"Level {Amount:N0}/{MaxAmount:N0}"
            : MaxAmount == 0
                ? $"Level {Amount:N0}"
                : Amount == 1
                    ? "Purchased"
                    : "";
    #endregion

    /// <summary>
    /// Copies the properties of this purchase, excluding <see cref="Id"/>, to another purchase.
    /// </summary>
    internal virtual void CopyTo(Purchase other)
    {
        other.Name = Name;
        other.Requires = Requires;
        other.BaseRequirement = BaseRequirement;
        other.RequirementScaling = RequirementScaling;
        other.RequirementAdditive = RequirementAdditive;
        other.Currency = Currency!;
        other.BaseCost = BaseCost;
        other.CostScaling = CostScaling;
        other.MaxAmount = MaxAmount;
    }

    #region IDependency implementation
    public string DependencyId => $"{GetType().Name};{Id}";

    double IReadOnlyDependency.Value => Amount;

    [NotMapped]
    double IDependency.Value
    {
        get => Amount;
        set => Amount = (int)value;
    }

    public override string ToString()
    {
        return $"{DependencyId} ({Name})";
    }

    public Purchase()
    {
        IDependency.Register(this);
    }
    #endregion
}
