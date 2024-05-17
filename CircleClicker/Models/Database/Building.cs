namespace CircleClicker.Models.Database;

public class Building : Purchase, IStat
{
    #region Model
    private double _baseProduction;

    /// <summary>
    /// The base production of the building.<br />
    /// Use <see cref="Value"/> to get the base production after all current upgrade boosts.
    /// </summary>
    public double BaseProduction
    {
        get => _baseProduction;
        set
        {
            _baseProduction = value;
            OnPropertyChanged([nameof(Value), nameof(Production)]);
            Models.Currency.Circles.InvokePropertyChanged(
                nameof(Models.Currency.Production),
                nameof(Models.Currency.IsProduced)
            );
        }
    }
    #endregion

    #region Values for CurrentSave
    public override int Amount
    {
        get => base.Amount;
        set
        {
            base.Amount = value;
            InvokePropertyChanged(nameof(Production));
            Models.Currency.Circles.InvokePropertyChanged(
                nameof(Models.Currency.Production),
                nameof(Models.Currency.IsProduced)
            );
        }
    }

    /// <summary>
    /// The current production of the building.
    /// </summary>
    public double Production => Value * Amount;
    #endregion

    #region IStat implementation
    public double DefaultValue => IStat.DefaultValue(this, 1);

    string IStat.Name => $"{Name} production multiplier";

    string IStat.StatId => DependencyId;

    string IStat.Description => $"{Name} production is increased by x{{0}}.";

    double IStat.BaseValue => BaseProduction;

    bool IStat.IsAdditive => false;

    /// <summary>
    /// <inheritdoc cref="IStat.Value"/><br />
    /// For <see cref="Building"/>s, this will be the base production of the building after all current upgrade boosts.
    /// </summary>
    public double Value => IStat.DefaultValue(this) * Stat.Production.Value;

    public Building()
    {
        IStat.Instances.Add(this);
    }
    #endregion

    internal override void CopyTo(Purchase other)
    {
        base.CopyTo(other);

        if (other is Building building)
        {
            building.BaseProduction = BaseProduction;
        }
    }
}
