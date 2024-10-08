using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CircleClicker.Utils;

namespace CircleClicker.Models.Database;

/// <summary>
/// The base class for something that can be purchased.
/// </summary>
public abstract partial class Purchase : Observable, IDependency
{
    #region Model
    private string _name = "";
    private double _requiredAmount;
    private double _requirementScaling;
    private bool _requirementAdditive;
    private double _baseCost;
    private double _costScaling;
    private int _maxAmount;

    public int Id { get; set; }

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

    /// <summary>
    /// The maximum amount of this purchase you can have.
    /// </summary>
    public int MaxAmount
    {
        get => _maxAmount;
        set
        {
            _maxAmount = value;
            OnPropertyChanged([nameof(AmountText)]);

            // FIXME: This causes purchases to get duplicated when calling LoadSampleData(false)
            //if (value > 0)
            //{
            //    Amount = Math.Min(Amount, value);
            //}
        }
    }
    #endregion

    #region Dependency references
    private IReadOnlyDependency? _requires;

    [MaybeNull]
    private IDependency _currency;

    /// <summary>
    /// The dependency that needs to be above <see cref="BaseRequirement"/> to be able to purchase this upgrade.<br />
    /// Can be <see langword="null"/>, in which case this purchase will have no requirement.
    /// </summary>
    [NotMapped, JsonIgnore]
    public IReadOnlyDependency? Requires
    {
        get => _requires;
        set
        {
            _requires = value;
            OnPropertyChanged([nameof(Requires), nameof(IsUnlocked), nameof(CanAfford)]);
        }
    }

    /// <summary>
    /// The <see cref="IDependency"/> used to purchase this upgrade.<br />
    /// May return <see langword="null"/> if the currency no longer exists.
    /// </summary>
    [NotMapped, JsonIgnore, MaybeNull]
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

    public string CostText =>
        $"Buy x{ClampedBulkBuy.ToString("N0", App.Culture)} for {Currency?.Format(Cost, "R-")}";
    #endregion

    #region Values for CurrentSave
    /// <summary>
    /// The description of the purchase. Supports rich text (see <see cref="Helpers.ParseInlines"/>).
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Returns the current amount needed to unlock this purchase.
    /// </summary>
    public double Requirement => GetRequirement(Amount);

    /// <summary>
    /// Whether this purchase is currently available for purchase.
    /// </summary>
    public virtual bool IsUnlocked =>
        Currency != null
        && (
            (
                (MaxAmount <= 0 || Amount < MaxAmount)
                && (Requires == null || Requires.Value >= Requirement)
            )
            || ClampedBulkBuy < 0
        );

    /// <summary>
    /// Whether this purchase can currently be afforded.
    /// </summary>
    public bool CanAfford =>
        IsUnlocked && ((Currency?.Value >= Cost && ClampedBulkBuy != 0) || ClampedBulkBuy < 0);

    /// <summary>
    /// Clamps <see cref="User.BulkBuy"/> for this purchase.
    /// </summary>
    public int ClampedBulkBuy
    {
        get
        {
            int amount = GetCost(Amount, Main.Instance.CurrentUser?.BulkBuy ?? 1).amount;
            for (int i = 0; i < amount; i++)
            {
                if (Requires?.Value < GetRequirement(Amount + i))
                {
                    return i;
                }
            }
            return amount;
        }
    }

    /// <summary>
    /// The current cost of this purchase.
    /// </summary>
    public double Cost => GetCost(Amount, ClampedBulkBuy).cost;

    public double GetRequirement(int level)
    {
        return RequirementAdditive
            ? BaseRequirement + RequirementScaling * level
            : BaseRequirement * Math.Pow(RequirementScaling, level);
    }

    /// <summary>
    /// Returns the cost of this purchase for the given level and bulk buy amount.
    /// </summary>
    public (double cost, int amount) GetCost(int level, int bulkAmount = 1)
    {
        if (bulkAmount == 1)
        {
            return (BaseCost * Math.Pow(CostScaling, level), 1);
        }
        else if (bulkAmount > 1)
        {
            double cost = 0;
            int i = 0;
            for (; i < bulkAmount && (MaxAmount <= 0 || i < level - MaxAmount); i++)
            {
                cost += BaseCost * Math.Pow(CostScaling, level + i);
            }

            return (cost, i);
        }
        else if (bulkAmount == 0)
        {
            double cost = 0;
            double nextCost = BaseCost * Math.Pow(CostScaling, level);
            int i = 0;

            while (cost <= Currency?.Value)
            {
                cost += nextCost;
                i++;

                nextCost = BaseCost * Math.Pow(CostScaling, level + i);
                if (cost + nextCost > Currency?.Value)
                {
                    break;
                }
            }

            return (cost, i);
        }
        else
        {
            double cost = 0;
            int i = 0;
            for (; i < -bulkAmount && i < level; i++)
            {
                cost -= BaseCost * Math.Pow(CostScaling, level - i - 1);
            }

            return (cost, -i);
        }
    }

    /// <summary>
    /// The current amount of this purchase owned.
    /// </summary>
    [NotMapped]
    public virtual int Amount
    {
        get =>
            Main.Instance.CurrentSave?.OwnedPurchases?.FirstOrDefault(v =>
                v.Purchase == this || v.PurchaseId == Id
            )?.Amount ?? 0;
        set
        {
            if (Main.Instance.CurrentSave == null)
            {
                return;
            }

            OwnedPurchase? owned = Main.Instance.CurrentSave.OwnedPurchases.FirstOrDefault(v =>
                v.Purchase == this || v.PurchaseId == Id
            );
            if (owned == null)
            {
                owned = new()
                {
                    Save = Main.Instance.CurrentSave,
                    Purchase = this,
                    PurchaseId = Id
                };
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
                    nameof(CanAfford),
                    nameof(Description),
                ]
            );

            foreach (Building building in Main.Instance.Buildings)
            {
                if (building != this)
                {
                    building.NotifyPropertyChanged(nameof(Description));
                }
            }
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
