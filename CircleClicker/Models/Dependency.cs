using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CircleClicker.Models.Database;
using CircleClicker.Utils;

namespace CircleClicker.Models
{
    /// <summary>
    /// The base interface for something that can unlock something else.
    /// </summary>
    public interface IReadOnlyDependency
    {
        /// <summary>
        /// A list of all <see cref="IReadOnlyDependency"/> instances.
        /// </summary>
        public static readonly List<IReadOnlyDependency> Instances = [];

        protected static void Register(IReadOnlyDependency instance)
        {
            Instances.Add(instance);
        }

        /// <summary>
        /// The internal ID of this dependency.
        /// </summary>
        public string DependencyId { get; }

        /// <summary>
        /// The display name of this dependency.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the current value of this dependency.
        /// </summary>
        public double Value { get; }

        public string Format(
            double number,
            string? format = null,
            IFormatProvider? formatProvider = null
        )
        {
            return DefaultFormat(this, number, format, formatProvider);
        }

        /// <summary>
        /// The default implementation for <see cref="Format"/>.
        /// </summary>
        protected static string DefaultFormat(
            IReadOnlyDependency dependency,
            double number,
            string? format = null,
            IFormatProvider? formatProvider = null
        )
        {
            string text = number.FormatSuffixes(format, formatProvider ?? App.Culture);
            if (dependency.Name != null)
            {
                text += $" {dependency.Name}";
            }
            return text;
        }
    }

    /// <summary>
    /// <inheritdoc cref="IReadOnlyDependency"/><br />
    /// Can also be used to purchase things.
    /// </summary>
    public interface IDependency : IReadOnlyDependency
    {
        /// <summary>
        /// A list of all <see cref="IDependency"/> instances.
        /// </summary>
        public static new readonly List<IDependency> Instances = [];

        protected static void Register(IDependency instance)
        {
            IReadOnlyDependency.Instances.Add(instance);
            Instances.Add(instance);
        }

        /// <summary>
        /// Gets and sets the current value of this dependency.
        /// </summary>
        public new double Value { get; set; }
    }

    /// <summary>
    /// A stat that can unlock something else.
    /// </summary>
    public class ReadOnlyDependency : NotifyPropertyChanged, IReadOnlyDependency
    {
        #region Instances
        /// <inheritdoc cref="Save.TotalCircles"/>
        public static readonly ReadOnlyDependency TotalCircles =
            new() { DependencyId = nameof(TotalCircles), Getter = s => s.TotalCircles };

        /// <inheritdoc cref="Save.ManualCircles"/>
        public static readonly ReadOnlyDependency ManualCircles =
            new() { DependencyId = nameof(ManualCircles), Getter = s => s.ManualCircles };

        /// <inheritdoc cref="Save.TotalTriangles"/>
        public static readonly ReadOnlyDependency TotalTriangles =
            new() { DependencyId = nameof(TotalTriangles), Getter = s => s.TotalTriangles };

        /// <inheritdoc cref="Save.Clicks"/>
        public static readonly ReadOnlyDependency Clicks =
            new() { DependencyId = nameof(Clicks), Getter = s => s.Clicks };

        /// <inheritdoc cref="Save.TriangleClicks"/>
        public static readonly ReadOnlyDependency TriangleClicks =
            new() { DependencyId = nameof(TriangleClicks), Getter = s => s.TriangleClicks };

        /// <inheritdoc cref="Save.LifetimeCircles"/>
        public static readonly ReadOnlyDependency LifetimeCircles =
            new() { DependencyId = nameof(LifetimeCircles), Getter = s => s.LifetimeCircles };

        /// <inheritdoc cref="Save.LifetimeManualCircles"/>
        public static readonly ReadOnlyDependency LifetimeManualCircles =
            new()
            {
                DependencyId = nameof(LifetimeManualCircles),
                Getter = s => s.LifetimeManualCircles
            };

        /// <inheritdoc cref="Save.LifetimeTriangles"/>
        public static readonly ReadOnlyDependency LifetimeTriangles =
            new() { DependencyId = nameof(LifetimeTriangles), Getter = s => s.LifetimeTriangles };

        /// <inheritdoc cref="Save.LifetimeSquares"/>
        public static readonly ReadOnlyDependency LifetimeSquares =
            new() { DependencyId = nameof(LifetimeSquares), Getter = s => s.LifetimeSquares };

        /// <inheritdoc cref="Save.LifetimeClicks"/>
        public static readonly ReadOnlyDependency LifetimeClicks =
            new() { DependencyId = nameof(LifetimeClicks), Getter = s => s.LifetimeClicks };

        /// <inheritdoc cref="Save.LifetimeTriangleClicks"/>
        public static readonly ReadOnlyDependency LifetimeTriangleClicks =
            new()
            {
                DependencyId = nameof(LifetimeTriangleClicks),
                Getter = s => s.LifetimeTriangleClicks
            };
        #endregion

        #region Implementation
        public string? Name { get; init; }

        public required string DependencyId { get; init; }

        public double Value =>
            Main.Instance.CurrentSave != null ? Getter(Main.Instance.CurrentSave) : 0;

        public required Func<Save, double> Getter { private get; init; }

        public ReadOnlyDependency()
        {
            Register();
        }

        protected virtual void Register()
        {
            IReadOnlyDependency.Register(this);
        }

        public override string ToString()
        {
            return DependencyId;
        }
        #endregion
    }

    /// <summary>
    /// <inheritdoc cref="ReadOnlyDependency"/>
    /// Can also be used to purchase things.
    /// </summary>
    public class Dependency : ReadOnlyDependency, IDependency
    {
        public new double Value
        {
            get => base.Value;
            set
            {
                if (Main.Instance.CurrentSave != null)
                {
                    Setter(Main.Instance.CurrentSave, value);
                }
            }
        }

        public required Action<Save, double> Setter { private get; init; }

        protected override void Register()
        {
            IDependency.Register(this);
        }

        // Exposes IDependency.Format
        public virtual string Format(
            double number,
            string? format = null,
            IFormatProvider? formatProvider = null
        )
        {
            return IReadOnlyDependency.DefaultFormat(this, number, format, formatProvider);
        }
    }

    /// <summary>
    /// A currency that can be displayed with <see cref="UI.Controls.CurrencyDisplay"/>.
    /// </summary>
    public class Currency : Dependency, IDependency
    {
        #region Instances
        public static readonly List<Currency> Instances = [];

        /// <inheritdoc cref="Save.Circles"/>
        public static readonly Currency Circles =
            new()
            {
                DependencyId = nameof(Circles),
                Brush = Application.Current.TryFindResource("AccentBrush") as Brush,
                Name = "Circles",
                Icon = "⚫",
                Getter = s => s.Circles,
                ProductionGetter = m => m.TotalProduction,
                Setter = (s, v) => s.Circles = v
            };

        /// <inheritdoc cref="Save.Triangles"/>
        public static readonly Currency Triangles =
            new()
            {
                DependencyId = nameof(Triangles),
                Brush = new LinearGradientBrush(
                    Color.FromRgb(242, 203, 12),
                    Color.FromRgb(229, 138, 11),
                    90
                ),
                Name = "Triangles",
                Icon = "▼",
                Getter = s => s.Triangles,
                IsUnlockedGetter = s => s.LifetimeTriangles > 0,
                Setter = (s, v) => s.Triangles = v
            };

        public static readonly Currency Squares =
            new()
            {
                DependencyId = nameof(Squares),
                Brush = Application.Current.TryFindResource("SquareBrush") as Brush,
                Name = "Squares",
                Icon = "⬛",
                Getter = s => s.Squares,
                PendingGetter = m => m.PendingSquares,
                IsUnlockedGetter = s => s.LifetimeSquares > 0 || Main.Instance.PendingSquares > 0,
                Setter = (s, v) => s.Squares = v
            };
        #endregion

        #region Model
        /// <summary>
        /// The icon of the currency.
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// The amount of currency being earned per second.
        /// </summary>
        public double Production => ProductionGetter != null ? ProductionGetter(Main.Instance) : 0;

        /// <summary>
        /// The amount of currency that is currently waiting to be claimed.
        /// </summary>
        public double Pending => PendingGetter != null ? PendingGetter(Main.Instance) : 0;

        /// <summary>
        /// Whether this currency has been discovered this session.
        /// </summary>
        public bool IsUnlocked =>
            IsUnlockedGetter == null
            || (Main.Instance.CurrentSave != null && IsUnlockedGetter(Main.Instance.CurrentSave));

        /// <summary>
        /// Whether this <see cref="Currency"/> is currently being produced.<br />
        /// Used by <see cref="UI.Controls.CurrencyDisplay"/> to determine whether to display production.
        /// </summary>
        public bool IsProductionUnlocked => ProductionGetter != null && Production > 0;

        /// <summary>
        /// Whether there is any of this <see cref="Currency"/> currently pending.<br />
        /// Used by <see cref="UI.Controls.CurrencyDisplay"/> to determine whether to display pending currency.
        /// </summary>
        public bool IsPendingUnlocked => PendingGetter != null && Pending > 0;

        public int AffordableUpgrades =>
            Main.Instance.Upgrades.Count(v => v.Currency == this && v.CanAfford);

        /// <summary>
        /// The color of the currency's icon.
        /// </summary>
        public Brush? Brush { get; init; }
        #endregion

        #region Implementation
        public Func<Main, double>? ProductionGetter { private get; init; }

        public Func<Main, double>? PendingGetter { private get; init; }

        public Func<Save, bool>? IsUnlockedGetter { private get; init; }

        protected override void Register()
        {
            Instances.Add(this);
            base.Register();
        }

        public override string Format(
            double number,
            string? format = null,
            IFormatProvider? formatProvider = null
        )
        {
            return Icon != null
                ? $"{Icon} {number.FormatSuffixes(format, formatProvider ?? App.Culture)}"
                : base.Format(number, format, formatProvider);
        }
        #endregion
    }
}
