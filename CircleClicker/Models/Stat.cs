using CircleClicker.Models.Database;
using CircleClicker.Utils;

namespace CircleClicker.Models
{
    // TODO: Look into caching stat values to speed up calculations

    /// <summary>
    /// The base interface for something that can be boosted by <see cref="Upgrade"/>s.
    /// </summary>
    public interface IStat
    {
        /// <summary>
        /// A list of all <see cref="IStat"/> instances.
        /// </summary>
        public static readonly List<IStat> Instances = [];

        /// <summary>
        /// The internal ID of the stat.
        /// </summary>
        public string StatId { get; }

        /// <summary>
        /// The display name of the stat.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description for upgrades that affect this stat.<br />
        /// This will be passed to <see cref="string.Format(IFormatProvider?, string, object?)"/> with <see cref="Upgrade.Effect"/> as the parameter, i.e. <c>{0}</c> will be replaced with <see cref="Upgrade.Effect"/>.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The base value of this stat.
        /// </summary>
        public double BaseValue { get; }

        /// <summary>
        /// Whether boosts to this stats are additive (<see langword="true"/>) or multiplicative (<see langword="false"/>, default).
        /// </summary>
        public bool IsAdditive { get; }

        /// <summary>
        /// The current value of this stat.
        /// </summary>
        public double Value => DefaultValue(this);

        /// <summary>
        /// The default implementation of <see cref="Value"/>.
        /// </summary>
        protected static double DefaultValue(IStat stat, double? overrideBaseValue = null)
        {
            double value = overrideBaseValue ?? stat.BaseValue;
            if (Main.Instance.CurrentSave != null)
            {
                foreach (Upgrade upgrade in Main.Instance.Upgrades)
                {
                    if (upgrade.Affects != stat || upgrade.Amount == 0)
                    {
                        continue;
                    }

                    if (stat.IsAdditive)
                    {
                        value += upgrade.Effect;
                    }
                    else
                    {
                        value *= upgrade.Effect;
                    }
                }
            }
            return value;
        }
    }

    /// <summary>
    /// A game stat that can be affected by <see cref="Upgrade"/>s.
    /// </summary>
    public class Stat : Observable, IStat
    {
        #region Instances
        public static readonly List<Stat> Instances = [];

        /// <summary>
        /// The amount of building production to add to the circles gained per click.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat ProductionToCPC =
            new()
            {
                StatId = nameof(ProductionToCPC),
                Name = "% of circles per second gained per click",
                Description =
                    """<font color="res:BrightAccentBrush">Clicking</font> grants +{0}% of your <font color="res:AccentBrush">circles per second</font>.""",
                IsAdditive = true,
                CustomFormula = v => v / 100
            };

        /// <summary>
        /// The base number of circles gained per click.<br />
        /// On this stat, <see cref="Value"/> returns the total number of circles per click.
        /// </summary>
        public static readonly Stat CirclesPerClick =
            new()
            {
                StatId = nameof(CirclesPerClick),
                Name = "Base circles per click",
                Description =
                    """Gain x{0} base <font color="res:AccentBrush">circles</font> from <font color="res:BrightAccentBrush">clicking</font>.""",
                CustomFormula = v => v + Currency.Circles.Production * ProductionToCPC.Value
            };

        /// <summary>
        /// The multiplier to the production of all buildings.
        /// </summary>
        public static readonly Stat Production =
            new()
            {
                StatId = nameof(Production),
                Name = "Building production multiplier",
                Description =
                    """Increases all <font color="res:AccentBrush">building production</font> by x{0}."""
            };

        /// <summary>
        /// The multiplier to triangle gain.
        /// </summary>
        public static readonly Stat TrianglesPerClick =
            new()
            {
                StatId = nameof(TrianglesPerClick),
                Name = "Triangle multiplier",
                Description =
                    """Increases the <font color="res:TriangleBrush">triangles</font> you earn from <font color="res:BrightAccentBrush">clicking</font> by x{0}."""
            };

        /// <summary>
        /// The chance to earn triangles from clicking the big button.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat TriangleChance =
            new()
            {
                StatId = nameof(TriangleChance),
                Name = "% chance to earn triangles per click",
                Description =
                    """Increases the <font color="res:MidForegroundBrush">chance</font> to earn <font color="res:TriangleBrush">triangles</font> from <font color="res:BrightAccentBrush">clicking</font> by +{0}%.""",
                DefaultBaseValue = 0.5,
                IsAdditive = true,
                CustomFormula = v => v / 100
            };

        /// <summary>
        /// The multiplier to square gain.
        /// </summary>
        public static readonly Stat Squares =
            new()
            {
                StatId = nameof(Squares),
                Name = "Square multiplier",
                Description =
                    """Increases the <font color="res:SquareBrush">squares</font> you earn from <font color="res:BrightSquareBrush">reincarnating</font> by x{0}.""",
            };

        /// <summary>
        /// The multiplier to the <b>offline</b> production of all buildings.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat OfflineProduction =
            new()
            {
                StatId = nameof(OfflineProduction),
                Name = "% of circles per second gained while offline",
                Description =
                    """Gain +{0}% of your <font color="res:AccentBrush">circles per second</font> while <font color="res:SquareBrush">offline</font>.""",
                DefaultBaseValue = 10,
                IsAdditive = true,
                CustomFormula = v => v / 100
            };

        /// <summary>
        /// The maximum duration for offline building production, in hours.
        /// </summary>
        public static readonly Stat MaxOfflineTime =
            new()
            {
                StatId = nameof(MaxOfflineTime),
                Name = "Maximum offline time (in hours)",
                Description =
                    """Increases how long you can produce <font color="res:AccentBrush">circles</font> while <font color="res:SquareBrush">offline</font> by {0} hour(s).""",
                DefaultBaseValue = 3,
                IsAdditive = true,
            };

        #endregion

        #region Model
        public required string StatId { get; init; }

        public string Name { get; private set; } = null!; // Set in Stat.Register

        public string Description { get; private set; } = null!; // Set in Stat.Register

        /// <summary>
        /// The base value of the stat.
        /// </summary>
        public VariableReference BaseValue { get; private set; } = null!; // Set in Stat.Register

        public bool IsAdditive { get; init; }

        /// <summary>
        /// Takes the default value of the stat and returns the actual value.
        /// </summary>
        private Func<double, double>? CustomFormula { get; init; }

        private double? DefaultBaseValue { get; init; }
        #endregion

        #region Implementation
        public double DefaultValue => IStat.DefaultValue(this);

        /// <summary>
        /// The current value for this stat.
        /// </summary>
        public double Value
        {
            get
            {
                double value = IStat.DefaultValue(this);
                return CustomFormula != null ? CustomFormula(value) : value;
            }
        }

        double IStat.BaseValue => BaseValue.Value;

        public Stat()
        {
            IStat.Instances.Add(this);
            Instances.Add(this);
        }

        internal void Register()
        {
            Name ??= StatId;
            Description ??= StatId;
            BaseValue = new($"Stat.{StatId}.BaseValue", DefaultBaseValue ?? (IsAdditive ? 0 : 1));
        }

        public override string ToString()
        {
            return StatId;
        }
        #endregion
    }
}
