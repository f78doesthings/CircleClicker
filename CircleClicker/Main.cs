using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.Utils;
using System.Collections.ObjectModel;

namespace CircleClicker
{
    public class Main : Observable
    {
        #region Constants
        private Main() { }

        public static readonly Main Instance = new();

        /// <summary>
        /// The time between <see cref="Tick"/> calls, in seconds.
        /// </summary>
        public const double TickInterval = 1 / 60.0;

        /// <summary>
        /// The time between <see cref="Save"/> calls, in seconds.
        /// </summary>
        public const double AutosaveInterval = 60;

        /// <summary>
        /// The maximum number of ticks to run for offline production.
        /// </summary>
        public const int MaxOfflineTicks = 10000;

        /// <summary>
        /// The minimum number of circles needed to perform a reincarnation.
        /// </summary>
        public static readonly VariableReference ReincarnationCost =
            new("ReincarnationCost", 1_000_000_000);

        public static readonly VariableReference SquarePower = new("SquarePower", 0.5);
        #endregion

        #region Properties / Fields
        /// <summary>
        /// The last time <see cref="Tick"/> was called.
        /// </summary>
        private DateTime? _lastTick;

        /// <summary>
        /// The last time <see cref="Save"/> was called this session.
        /// </summary>
        private DateTime? _lastSave;

        private bool _isSaving = false;

        /// <summary>
        /// Whether database changes are currently being saved.
        /// </summary>
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The exception that occured during the last save operation, if any.
        /// </summary>
        public Exception? LastSaveException { get; set; }

        /// <summary>
        /// Whether the database is currently available. This is checked once during startup.
        /// </summary>
        public bool IsDBAvailable { get; set; } = false;

        /// <summary>
        /// Disables some methods. Can be used to temporarily improve performance or to prevent saving during critical operations.<br />
        /// <br />
        /// If <see langword="true"/>, <see cref="Tick"/>, <see cref="SaveAsync"/> and <see cref="Observable.NotifyPropertyChanged"/> can be called.<br/>
        /// If <see langword="null"/>, only <see cref="Observable.NotifyPropertyChanged"/> can be called.<br />
        /// If <see langword="false"/>, none of the above can be called.
        /// </summary>
        public bool? IsAutosavingEnabled { get; set; } = true;

        /// <summary>
        /// A global random number generator.
        /// </summary>
        public Random RNG { get; } = new();

        /// <summary>
        /// The global database context.<br />
        /// Make sure to check <see cref="IsDBAvailable"/> before using this, as not doing so will result in an error if the database is unavailable.
        /// </summary>
        public CircleClickerContext DB { get; } = new();

        /// <summary>
        /// The user that is currently logged in.
        /// </summary>
        public User? CurrentUser { get; set; }

        /// <summary>
        /// The save that is currently loaded.
        /// </summary>
        public Save? CurrentSave { get; set; }

        /// <summary>
        /// A list of all buildings.
        /// </summary>
        public ObservableCollection<Building> Buildings { get; set; } = null!; // Set during App.Application_Startup

        /// <summary>
        /// A list of all upgrades.
        /// </summary>
        public ObservableCollection<Upgrade> Upgrades { get; set; } = null!; // Set during App.Application_Startup

        /// <summary>
        /// A list of all game variables.
        /// </summary>
        public ObservableCollection<Variable> Variables { get; set; } = null!; // Set during App.Application_Startup
        #endregion

        #region Methods
        /// <summary>
        /// Runs all timed game logic, such as building production.
        /// </summary>
        public void Tick()
        {
            if (CurrentSave == null || IsAutosavingEnabled != true)
            {
                return;
            }

            // Calculate the time since the last Tick call
            DateTime now = DateTime.Now;
            TimeSpan deltaTimeSpan = now - (_lastTick ?? now);
            double deltaTime = deltaTimeSpan.TotalSeconds;
            _lastTick = now;

            // Calculate the time since the last save
            _lastSave ??= now;
            TimeSpan? timeSinceLastSave = now - _lastSave;
            if (timeSinceLastSave?.TotalSeconds >= AutosaveInterval)
            {
                _lastSave = now;
                _ = SaveAsync(); // Exceptions will be stored in LastSaveException
            }

            // Calculate the number of circles produced this tick
            CurrentSave.Circles += Currency.Circles.Production * deltaTime;

            // This will update various things in MainWindow
            foreach (Building building in Buildings)
            {
                building.NotifyPropertyChanged(
                    nameof(building.IsUnlocked),
                    nameof(building.CanAfford),
                    nameof(building.CostText)
                );
            }

            foreach (Upgrade upgrade in Upgrades)
            {
                upgrade.NotifyPropertyChanged(
                    nameof(upgrade.IsUnlocked),
                    nameof(upgrade.CanAfford),
                    nameof(upgrade.CostText),
                    nameof(upgrade.Description)
                );
            }

            foreach (Currency currency in Currency.Instances)
            {
                currency.NotifyPropertyChanged(nameof(currency.AffordableUpgrades));
            }

            /*InvokePropertyChanged(
                nameof(PendingSquares),
                nameof(CanReincarnate),
                nameof(PendingSquaresText),
                nameof(ReincarnateProgress)
            );*/
            Currency.Squares.NotifyPropertyChanged(
                nameof(Currency.Pending),
                nameof(Currency.IsPending),
                nameof(Currency.IsUnlocked)
            );
        }

        /// <summary>
        /// Runs all <b>offline</b> timed game logic, and returns the number of circles produced.
        /// </summary>
        public void TickOffline(double deltaTime, out double circlesProduced)
        {
            circlesProduced = 0;
            if (CurrentSave == null)
            {
                return;
            }

            circlesProduced =
                Currency.Circles.Production * Stat.OfflineProduction.Value * deltaTime;
            CurrentSave.Circles += circlesProduced;
        }

        /// <summary>
        /// Updates CurrentSave.LastSaveDate and saves all changes to the database asynchronously.
        /// </summary>
        public async Task SaveAsync(bool manual = false)
        {
            if (CurrentSave == null || (IsAutosavingEnabled != true && !manual) || IsSaving)
            {
                return;
            }

            IsSaving = true;
            CurrentSave.LastSaveDate = DateTime.Now;
            _lastSave = CurrentSave.LastSaveDate;

            if (IsDBAvailable)
            {
                try
                {
                    await DB.SaveChangesAsync();
                    LastSaveException = null;
                }
                catch (Exception ex)
                {
                    LastSaveException = ex;
                }
            }

            IsSaving = false;
        }

        /// <summary>
        /// Updates CurrentSave.LastSaveDate and saves all changes to the database.
        /// </summary>
        public void Save()
        {
            if (CurrentSave == null || IsSaving)
            {
                return;
            }

            IsSaving = true;
            CurrentSave.LastSaveDate = DateTime.Now;
            _lastSave = CurrentSave.LastSaveDate;

            if (IsDBAvailable)
            {
                try
                {
                    DB.SaveChanges();
                    LastSaveException = null;
                }
                catch (Exception ex)
                {
                    LastSaveException = ex;
                }
            }

            IsSaving = false;
        }

        /// <summary>
        /// Replaces all purchases with default data.
        /// </summary>
        public void LoadSampleData(bool deleteExistingData = false)
        {
            bool? prevState = IsAutosavingEnabled;
            IsAutosavingEnabled = false;
            int buildingIndex = 0;
            int upgradeIndex = 0;

            T AddPurchase<T>(T purchase)
                where T : Purchase
            {
                if (purchase is Building building)
                {
                    if (Buildings.Count > buildingIndex && !deleteExistingData)
                    {
                        building.CopyTo(Buildings[buildingIndex]);
                        purchase = (Buildings[buildingIndex] as T)!;
                    }
                    else
                    {
                        Buildings.Add(building);
                    }

                    buildingIndex++;
                }
                else if (purchase is Upgrade upgrade)
                {
                    if (Upgrades.Count > upgradeIndex && !deleteExistingData)
                    {
                        upgrade.CopyTo(Upgrades[upgradeIndex]);
                        purchase = (Upgrades[upgradeIndex] as T)!;
                    }
                    else
                    {
                        Upgrades.Add(upgrade);
                    }

                    upgradeIndex++;
                }
                else
                {
                    throw new NotSupportedException();
                }

                return purchase;
            }

            if (deleteExistingData)
            {
                Buildings.Clear();
                Upgrades.Clear();
            }

            #region Buildings and building upgrades
            Building? prevBuilding = null;
            for (int i = 0; i < 10; i++)
            {
                double costScaling = 1.125 + (i * 0.001);
                Building building = AddPurchase(
                    new Building()
                    {
                        Name = $"Circle Factory {i + 1}.0",
                        Requires = prevBuilding,
                        BaseRequirement = 1,
                        Currency = Currency.Circles,
                        BaseCost = 10 * Math.Pow(10 + ((i - 1) * costScaling), i),
                        CostScaling = costScaling,
                        BaseProduction = Math.Pow(6 + ((i - 1) * costScaling / 3), i),
                    }
                );
                prevBuilding = building;

                double upgradeScaling = Math.Pow(costScaling, 10) * (1.02 + i * 0.002);
                AddPurchase(
                    new Upgrade()
                    {
                        Name = $"Better {building.Name}",
                        Requires = building,
                        BaseRequirement = 10,
                        RequirementScaling = 10,
                        RequirementAdditive = true,
                        Currency = Currency.Circles,
                        BaseCost = building.BaseCost * upgradeScaling * 4 * Math.Pow(1.075, i),
                        CostScaling = upgradeScaling,
                        Affects = building,
                        BaseEffect = 2,
                    }
                );
            }
            #endregion

            #region Circle upgrades
            AddPurchase(
                new Upgrade()
                {
                    Name = "Enhanced Cursor",
                    Requires = ReadOnlyDependency.ManualCircles,
                    BaseRequirement = 50,
                    RequirementScaling = 2.96,
                    Currency = Currency.Circles,
                    BaseCost = 75,
                    CostScaling = 2.95,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Factory Piping",
                    Requires = ReadOnlyDependency.LifetimeClicks,
                    BaseRequirement = 100,
                    RequirementScaling = 2.75,
                    Currency = Currency.Circles,
                    BaseCost = 250,
                    CostScaling = 100,
                    Affects = Stat.ProductionToCPC,
                    BaseEffect = 1,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Square Heaven",
                    Requires = ReadOnlyDependency.LifetimeCircles,
                    BaseRequirement = ReincarnationCost,
                    Currency = Currency.Circles,
                    BaseCost = ReincarnationCost * 200,
                    CostScaling = 200,
                    Affects = Stat.Squares,
                    BaseEffect = 1.4,
                }
            );
            #endregion

            #region Triangle upgrades
            AddPurchase(
                new Upgrade()
                {
                    Name = "Triangle Detector",
                    Requires = ReadOnlyDependency.TotalTriangles,
                    BaseRequirement = 1,
                    Currency = Currency.Triangles,
                    BaseCost = 1,
                    CostScaling = 1.5,
                    MaxAmount = 30,
                    Affects = Stat.TriangleChance,
                    BaseEffect = 0.1,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "More Triangles",
                    Requires = ReadOnlyDependency.TotalTriangles,
                    BaseRequirement = 5,
                    Currency = Currency.Triangles,
                    BaseCost = 2.5,
                    CostScaling = 1.8,
                    Affects = Stat.TrianglesPerClick,
                    BaseEffect = 1.5,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Triangle Factories",
                    Requires = ReadOnlyDependency.TotalTriangles,
                    BaseRequirement = Math.Pow(6, 2),
                    Currency = Currency.Triangles,
                    BaseCost = 5,
                    CostScaling = 4,
                    Affects = Stat.Production,
                    BaseEffect = 1.25,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Triangle Cursor",
                    Requires = ReadOnlyDependency.TotalTriangles,
                    BaseRequirement = Math.Pow(7, 3),
                    Currency = Currency.Triangles,
                    BaseCost = 100,
                    CostScaling = 6,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = 1.625,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Quad Heaven",
                    Requires = ReadOnlyDependency.LifetimeCircles,
                    BaseRequirement = ReincarnationCost,
                    Currency = Currency.Triangles,
                    BaseCost = 15_000,
                    CostScaling = 15,
                    Affects = Stat.Squares,
                    BaseEffect = 1.4,
                }
            );
            #endregion

            #region Square upgrades
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Factories",
                    Currency = Currency.Squares,
                    BaseCost = 0.5,
                    CostScaling = 4,
                    Affects = Stat.Production,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Triangles",
                    Currency = Currency.Squares,
                    BaseCost = 0.75,
                    CostScaling = 6,
                    Affects = Stat.TrianglesPerClick,
                    BaseEffect = 1.75,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Cursor",
                    Currency = Currency.Squares,
                    BaseCost = 1,
                    CostScaling = 5,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = 2.25,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Triangle Detector",
                    Currency = Currency.Squares,
                    BaseCost = 1.25,
                    CostScaling = 8,
                    MaxAmount = 5,
                    Affects = Stat.TriangleChance,
                    BaseEffect = 0.3,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Beds",
                    Currency = Currency.Squares,
                    BaseCost = 5,
                    CostScaling = 9,
                    Affects = Stat.MaxOfflineTime,
                    BaseEffect = 1,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Workers",
                    Currency = Currency.Squares,
                    BaseCost = 6,
                    CostScaling = 10,
                    MaxAmount = 5,
                    Affects = Stat.OfflineProduction,
                    BaseEffect = 4,
                }
            );
            #endregion

            #region Variables
            foreach (VariableReference variable in VariableReference.Instances)
            {
                variable.Value = variable.DefaultValue;
            }
            #endregion

            if (IsDBAvailable)
            {
                // HACK: This needs to be done to save I(ReadOnly)Dependency references properly. Is there a better way to do this?
                DB.SaveChanges();
                foreach (Purchase purchase in DB.Purchases)
                {
                    DB.Update(purchase);
                }
                DB.SaveChanges();
            }
            IsAutosavingEnabled = prevState;
        }
        #endregion
    }
}
