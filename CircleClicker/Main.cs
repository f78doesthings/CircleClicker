﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.UI.Windows;
using CircleClicker.Utils;
using NAudio.Wave;

namespace CircleClicker
{
    public class Main : NotifyPropertyChanged
    {
        #region Constants
        // TODO: consider finding a way to move some hardcoded variables to the database, so they can be edited by admins
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
        public const int MaxOfflineTicks = 1000;

        /// <summary>
        /// The minimum number of circles needed to perform a reincarnation.
        /// </summary>
        public const double ReincarnationCost = 1_000_000_000;
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
        /// If <see langword="true"/>, <see cref="Tick"/>, <see cref="SaveAsync"/> and <see cref="NotifyPropertyChanged.InvokePropertyChanged"/> can be called.<br/>
        /// If <see langword="null"/>, only <see cref="NotifyPropertyChanged.InvokePropertyChanged"/> can be called.<br />
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

        // TODO: consider moving these to the Currency getters
        /// <summary>
        /// Returns the total production of all buildings.
        /// </summary>
        public double TotalProduction => Buildings.Sum(b => b.Production);

        /// <summary>
        /// The number of squares that can currently be earned by reincarnating.
        /// </summary>
        public double PendingSquares =>
            CurrentSave == null || CurrentSave.TotalCircles < ReincarnationCost
                ? 0
                : Math.Pow(CurrentSave.TotalCircles / ReincarnationCost, 0.5) * Stat.Squares.Value;

        /// <summary>
        /// Whether the player can currently reincarnate.
        /// </summary>
        public bool CanReincarnate => PendingSquares > 0;

        /// <summary>
        /// Used by MainWindow to show a progress bar for the reincarnation button.
        /// </summary>
        public double ReincarnateProgress =>
            CurrentSave == null || CanReincarnate
                ? 0
                : Math.Log(CurrentSave.TotalCircles + 1) / Math.Log(ReincarnationCost + 1) * 100;

        /// <summary>
        /// The text to display on the <see cref="MainWindow.btn_reincarnate"/> button.
        /// </summary>
        public string PendingSquaresText =>
            CanReincarnate
                ? "Reincarnate!"
                : $"Earn {Currency.Circles.Format(ReincarnationCost)} to unlock";
        #endregion

        #region Methods
        /// <summary>
        /// Updates CurrentSave.LastSaveDate and saves all changes to the database asynchronously.
        /// </summary>
        public async Task SaveAsync()
        {
            if (CurrentSave == null || IsAutosavingEnabled != true || IsSaving)
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
        /// Runs all <b>offline</b> timed game logic, and returns the number of circles produced.
        /// </summary>
        public void TickOffline(double deltaTime, out double circlesProduced)
        {
            circlesProduced = 0;
            if (CurrentSave == null)
            {
                return;
            }

            circlesProduced = TotalProduction * Stat.OfflineProduction.Value * deltaTime;
            CurrentSave.Circles += circlesProduced;
        }

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
            CurrentSave.Circles += TotalProduction * deltaTime;

            // This will update various things in MainWindow
            foreach (Building building in Buildings)
            {
                building.InvokePropertyChanged(
                    nameof(building.IsUnlocked),
                    nameof(building.CanAfford)
                );
            }

            foreach (Upgrade upgrade in Upgrades)
            {
                upgrade.InvokePropertyChanged(
                    nameof(upgrade.IsUnlocked),
                    nameof(upgrade.CanAfford)
                );
            }

            foreach (Currency currency in Currency.Instances)
            {
                currency.InvokePropertyChanged(nameof(currency.AffordableUpgrades));
            }

            InvokePropertyChanged(
                nameof(PendingSquares),
                nameof(CanReincarnate),
                nameof(PendingSquaresText),
                nameof(ReincarnateProgress)
            );
            Currency.Squares.InvokePropertyChanged(
                nameof(Currency.Pending),
                nameof(Currency.IsPendingUnlocked),
                nameof(Currency.IsUnlocked)
            );
        }

        /// <summary>
        /// Replaces all purchases with default data.
        /// </summary>
        public void LoadSampleData(bool deleteExistingData = false)
        {
            bool? prevState = IsAutosavingEnabled;
            IsAutosavingEnabled = null;
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
                    RequirementScaling = 3,
                    Currency = Currency.Circles,
                    BaseCost = 75,
                    CostScaling = 3,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Factory Piping",
                    Requires = ReadOnlyDependency.Clicks,
                    BaseRequirement = 100,
                    RequirementScaling = 2.5,
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
                    BaseEffect = 2,
                }
            );
            #endregion

            #region Triangle upgrades
            AddPurchase(
                new Upgrade()
                {
                    Name = "Triangle Hunter",
                    Requires = ReadOnlyDependency.TotalTriangles,
                    BaseRequirement = 1,
                    Currency = Currency.Triangles,
                    BaseCost = 1,
                    CostScaling = 1.5,
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
                    Requires = ReadOnlyDependency.TriangleClicks,
                    BaseRequirement = 15,
                    Currency = Currency.Triangles,
                    BaseCost = 5,
                    CostScaling = 3.6,
                    Affects = Stat.Production,
                    BaseEffect = 1.25,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Triangle Cursor",
                    Requires = ReadOnlyDependency.TriangleClicks,
                    BaseRequirement = 75,
                    Currency = Currency.Triangles,
                    BaseCost = 100,
                    CostScaling = 7,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = Math.Pow(1.25, 2),
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Quad Heaven",
                    Requires = ReadOnlyDependency.LifetimeCircles,
                    BaseRequirement = ReincarnationCost,
                    Currency = Currency.Triangles,
                    BaseCost = 100_000,
                    CostScaling = 10,
                    Affects = Stat.Squares,
                    BaseEffect = 1.5,
                }
            );
            #endregion

            #region Square upgrades
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Factories",
                    Currency = Currency.Squares,
                    BaseCost = Math.Pow(1.5, 0),
                    CostScaling = 3,
                    Affects = Stat.Production,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Triangles",
                    Currency = Currency.Squares,
                    BaseCost = Math.Pow(1.5, 1),
                    CostScaling = 5,
                    Affects = Stat.TrianglesPerClick,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Cursor",
                    Currency = Currency.Squares,
                    BaseCost = Math.Pow(1.5, 2),
                    CostScaling = 4,
                    Affects = Stat.CirclesPerClick,
                    BaseEffect = 2,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Workers",
                    Currency = Currency.Squares,
                    BaseCost = Math.Pow(1.5, 3),
                    CostScaling = 7,
                    Affects = Stat.OfflineProduction,
                    BaseEffect = 5,
                }
            );
            AddPurchase(
                new Upgrade()
                {
                    Name = "Reincarnated Beds",
                    Currency = Currency.Squares,
                    BaseCost = Math.Pow(1.5, 4),
                    CostScaling = 9,
                    Affects = Stat.MaxOfflineTime,
                    BaseEffect = 1,
                }
            );
            #endregion

            if (IsDBAvailable)
            {
                // HACK: This first saves the new purchases to generate IDs for them, then saves again to fix dependency references
                DB.SaveChanges();
                DB.SaveChanges();
            }
            IsAutosavingEnabled = prevState;
        }
        #endregion
    }
}
