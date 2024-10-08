﻿using System.Text.Json.Serialization;

namespace CircleClicker.Models.Database;

/// <summary>
/// A purchase that produces circles over time.
/// </summary>
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
            Models.Currency.Circles.NotifyPropertyChanged(
                nameof(Models.Currency.Production),
                nameof(Models.Currency.IsProduced)
            );
        }
    }
    #endregion

    #region Values for CurrentSave
    public override string Description =>
        $"Base: <font color=\"res:ForegroundBrush\">{Models.Currency.Circles.Format(Value, "R+")}/s</font> | Total: <font color=\"res:ForegroundBrush\">{Models.Currency.Circles.Format(Production, "R+")}/s</font> ({(ProductionFraction).ToString("P2", App.Culture)})";

    [JsonIgnore]
    public override int Amount
    {
        get => base.Amount;
        set
        {
            base.Amount = value;
            NotifyPropertyChanged(nameof(Production));
            Models.Currency.Circles.NotifyPropertyChanged(
                nameof(Models.Currency.Production),
                nameof(Models.Currency.IsProduced)
            );
        }
    }

    /// <summary>
    /// The current production of the building.
    /// </summary>
    public double Production => Value * Amount;

    /// <summary>
    /// The fraction of the total production this building is currently producting.
    /// </summary>
    public double ProductionFraction =>
        Models.Currency.Circles.Production == 0
            ? 0
            : Production / Models.Currency.Circles.Production;
    #endregion

    #region IStat implementation
    public double DefaultValue => IStat.DefaultValue(this, 1);

    string IStat.Name => $"{Name} production multiplier";

    string IStat.StatId => DependencyId;

    string IStat.Description =>
        $"<font color=\"res:MidForegroundBrush\">{Name}</font> <font color=\"res:AccentBrush\">production</font> is increased by x{{0}}.";

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
