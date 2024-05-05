using System.Collections.ObjectModel;
using CircleClicker.Utils;

namespace CircleClicker.Models.Database;

public partial class Save : NotifyPropertyChanged
{
    public int Id { get; set; }

    public int UserId { get; set; }

    private double _circles;

    /// <summary>
    /// The current amount of circles. Circles are the main currency of Circle Clicker.
    /// </summary>
    public double Circles
    {
        get => _circles;
        set
        {
            if (value > _circles)
            {
                TotalCircles += value - _circles;
                LifetimeCircles += value - _circles;
            }

            _circles = value;
            OnPropertyChanged();
            Currency.Circles.InvokePropertyChanged(nameof(Currency.Value));
        }
    }

    private double _totalCircles;

    /// <summary>
    /// The total number of circles earned this incarnation. This is automatically incremented when setting <see cref="Circles"/>.
    /// </summary>
    public double TotalCircles
    {
        get => _totalCircles;
        set
        {
            _totalCircles = value;
            OnPropertyChanged();
            ReadOnlyDependency.TotalCircles.InvokePropertyChanged(nameof(ReadOnlyDependency.Value));
        }
    }

    private double _manualCircles;

    /// <summary>
    /// The total number of circles earned by clicking this incarnation.
    /// </summary>
    public double ManualCircles
    {
        get => _manualCircles;
        set
        {
            if (value > _manualCircles)
            {
                LifetimeManualCircles += value - _manualCircles;
            }

            _manualCircles = value;
            OnPropertyChanged();
            ReadOnlyDependency.ManualCircles.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
        }
    }

    private double _lifetimeCircles;

    /// <summary>
    /// The total number of circles earned. This is automatically increased when setting <see cref="Circles"/>.
    /// </summary>
    public double LifetimeCircles
    {
        get => _lifetimeCircles;
        set
        {
            _lifetimeCircles = value;
            OnPropertyChanged();
            ReadOnlyDependency.LifetimeCircles.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
        }
    }

    private double _lifetimeManualCircles;

    /// <summary>
    /// The total number of circles earned by clicking. This is automatically increased when setting <see cref="ManualCircles"/>
    /// </summary>
    public double LifetimeManualCircles
    {
        get => _lifetimeManualCircles;
        set
        {
            _lifetimeManualCircles = value;
            OnPropertyChanged();
            ReadOnlyDependency.LifetimeManualCircles.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
        }
    }

    private double _triangles;

    /// <summary>
    /// The current amount of triangles. Triangles are a rare currency that can be found by clicking the big button.
    /// </summary>
    public double Triangles
    {
        get => _triangles;
        set
        {
            if (value > _triangles)
            {
                TotalTriangles += value - _triangles;
                LifetimeTriangles += value - _triangles;
            }

            _triangles = value;
            OnPropertyChanged();
            Currency.Triangles.InvokePropertyChanged(nameof(Currency.Value));
        }
    }

    private double _totalTriangles;

    /// <summary>
    /// The total number of triangles earned this incarnation. This is automatically increased when setting <see cref="Triangles"/>.
    /// </summary>
    public double TotalTriangles
    {
        get => _totalTriangles;
        set
        {
            _totalTriangles = value;
            OnPropertyChanged();
            ReadOnlyDependency.TotalTriangles.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
        }
    }

    private double _lifetimeTriangles;

    /// <summary>
    /// The total number of triangles earned. This is automatically increased when setting <see cref="Triangles"/>.
    /// </summary>
    public double LifetimeTriangles
    {
        get => _lifetimeTriangles;
        set
        {
            _lifetimeTriangles = value;
            OnPropertyChanged();
            ReadOnlyDependency.TotalTriangles.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
            Currency.Triangles.InvokePropertyChanged(nameof(Currency.IsUnlocked));
        }
    }

    private double _squares;

    /// <summary>
    /// The current amount of squares. Squares are a special currency earned from performing reincarnations (which resets most stats).
    /// </summary>
    public double Squares
    {
        get => _squares;
        set
        {
            if (value > _squares)
            {
                LifetimeSquares += value - _squares;
            }

            _squares = value;
            OnPropertyChanged();
            Currency.Squares.InvokePropertyChanged(nameof(Currency.Value));
        }
    }

    private double _lifetimeSquares;

    /// <summary>
    /// The total number of squares earned. This is automatically increased when setting <see cref="Squares"/>.
    /// </summary>
    public double LifetimeSquares
    {
        get => _lifetimeSquares;
        set
        {
            _lifetimeSquares = value;
            OnPropertyChanged();
            ReadOnlyDependency.LifetimeSquares.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
            Currency.Squares.InvokePropertyChanged(nameof(Currency.IsUnlocked));
        }
    }

    private int _clicks;

    /// <summary>
    /// The number of times the player has clicked the big button this incarnation.
    /// </summary>
    public int Clicks
    {
        get => _clicks;
        set
        {
            if (value > _clicks)
            {
                LifetimeClicks += value - _clicks;
            }

            _clicks = value;
            OnPropertyChanged();
            ReadOnlyDependency.Clicks.InvokePropertyChanged(nameof(ReadOnlyDependency.Value));
        }
    }

    private int _triangleClicks;

    /// <summary>
    /// The number of times the player got triangles from clicking the big button this incarnation.
    /// </summary>
    public int TriangleClicks
    {
        get => _triangleClicks;
        set
        {
            if (value > _triangleClicks)
            {
                LifetimeTriangleClicks += value - _triangleClicks;
            }

            _triangleClicks = value;
            OnPropertyChanged();
            ReadOnlyDependency.TriangleClicks.InvokePropertyChanged(
                nameof(ReadOnlyDependency.Value)
            );
        }
    }

    private int _lifetimeClicks;

    /// <summary>
    /// The number of times the player has clicked the big button. This is automatically increased when setting <see cref="Clicks"/>.
    /// </summary>
    public int LifetimeClicks
    {
        get => _lifetimeClicks;
        set
        {
            _lifetimeClicks = value;
            OnPropertyChanged();
            ReadOnlyDependency.LifetimeClicks.InvokePropertyChanged(nameof(Currency.Value));
        }
    }

    private int _lifetimeTriangleClicks;

    /// <summary>
    /// The number of times the player got triangles from clicking the big button this incarnation. This is automatically increased when setting <see cref="TriangleClicks"/>.
    /// </summary>
    public int LifetimeTriangleClicks
    {
        get => _lifetimeTriangleClicks;
        set
        {
            _lifetimeTriangleClicks = value;
            OnPropertyChanged();
            ReadOnlyDependency.LifetimeTriangleClicks.InvokePropertyChanged(nameof(Currency.Value));
        }
    }

    /// <summary>
    /// The time at which the save was created.
    /// </summary>
    public DateTime CreationDate { get; set; } = DateTime.Now;

    /// <summary>
    /// The time at which the save was last updated.
    /// </summary>
    public DateTime LastSaveDate { get; set; } = DateTime.Now;

    /// <summary>
    /// The <see cref="Database.User"/> that this save belongs to.
    /// </summary>
    public virtual User User { get; set; }

    public Save()
    {
        User = null!;
    }

    public Save(User user)
    {
        UserId = user.Id;
        User = user;
    }

    public virtual List<OwnedPurchase> OwnedPurchases { get; set; } = [];
}
