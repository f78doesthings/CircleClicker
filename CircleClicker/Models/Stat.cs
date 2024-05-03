using CircleClicker.Models.Database;
using CircleClicker.Utils;

namespace CircleClicker.Models
{
    // TODO: look into caching stat values to speed up offline production calculation

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
        protected static double DefaultValue(IStat stat)
        {
            double value = stat.BaseValue;
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
    public class Stat : NotifyPropertyChanged, IStat
    {
        #region Instances
        public static readonly List<Stat> Instances = [];

        /// <summary>
        /// The amount of building production to add to the circles gained per click.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat ProductionToCPC =
            new(
                nameof(ProductionToCPC),
                "Clicking grants +{0}% of your circles per second.",
                isAdditive: true,
                customFormula: v => v / 100
            );

        /// <summary>
        /// The base number of circles gained per click.<br />
        /// On this stat, <see cref="Value"/> returns the total number of circles per click.
        /// </summary>
        public static readonly Stat CirclesPerClick =
            new(
                nameof(CirclesPerClick),
                "Gain x{0} base circles from clicking.",
                customFormula: v => v + Main.Instance.TotalProduction * ProductionToCPC.Value
            );

        /// <summary>
        /// The multiplier to the production of all buildings.
        /// </summary>
        public static readonly Stat Production =
            new(nameof(Production), "Increases all building production by x{0}.");

        /// <summary>
        /// The multiplier to triangle gain.
        /// </summary>
        public static readonly Stat TrianglesPerClick =
            new(
                nameof(TrianglesPerClick),
                "Increases the triangles you earn from clicking by x{0}."
            );

        /// <summary>
        /// The chance to earn triangles from clicking the big button.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat TriangleChance =
            new(
                nameof(TriangleChance),
                "Increases the chance to earn triangles from clicking by +{0}%.",
                0.5,
                true,
                v => v / 100
            );

        /// <summary>
        /// The multiplier to square gain.
        /// </summary>
        public static readonly Stat Squares =
            new(nameof(Squares), "Increases the squares you earn from reincarnating by x{0}.");

        /// <summary>
        /// The multiplier to the <b>offline</b> production of all buildings.<br />
        /// On this stat, the returned <see cref="Value"/> is divided by 100.
        /// </summary>
        public static readonly Stat OfflineProduction =
            new(
                nameof(OfflineProduction),
                "Gain +{0}% of your circles per second while offline.",
                10,
                true,
                v => v / 100
            );

        /// <summary>
        /// The maximum duration for offline building production, in hours.
        /// </summary>
        public static readonly Stat MaxOfflineTime =
            new(
                nameof(MaxOfflineTime),
                "Increases how long you can produce circles while offline by {0} hour(s).",
                3,
                true
            );

        #endregion

        #region Model
        public string StatId { get; }

        public string Description { get; }

        public VariableReference BaseValue { get; }

        public bool IsAdditive { get; }

        /// <summary>
        /// Takes the default value of the stat and returns the actual value.
        /// </summary>
        private readonly Func<double, double>? _customFormula;
        #endregion

        #region Implementation
        /// <summary>
        /// The current value for this stat.
        /// </summary>
        public double Value
        {
            get
            {
                double value = IStat.DefaultValue(this);
                return _customFormula != null ? _customFormula(value) : value;
            }
        }

        double IStat.BaseValue => BaseValue.Value;

        public Stat(
            string id = null!,
            string? statText = null,
            double? baseValue = null,
            bool isAdditive = false,
            Func<double, double>? customFormula = null
        )
        {
            StatId = id;
            Description = statText ?? id;
            BaseValue = new($"Stat.{StatId}.BaseValue", baseValue ?? (isAdditive ? 0 : 1));
            IsAdditive = isAdditive;
            _customFormula = customFormula;

            IStat.Instances.Add(this);
            Instances.Add(this);
            _ = BaseValue;
        }

        public override string ToString()
        {
            return StatId;
        }
        #endregion
    }
}
