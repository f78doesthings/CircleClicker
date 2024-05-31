using CircleClicker.UI.Windows;
using CircleClicker.Utils.Audio;
using System.Collections.ObjectModel;
using System.Windows;
using BC = BCrypt.Net.BCrypt;

namespace CircleClicker.Models.Database;

public partial class User
{
    private float _musicVolume = Settings.MusicVolume.DefaultValue;
    private float _soundVolume = Settings.SoundVolume.DefaultValue;

    public int Id { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The hashed password of the user.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Whether this user can access administrator-only features, such as the admin panel.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// The time until the user is banned. If <see langword="null"/>, the user is not banned.
    /// </summary>
    public DateTime? BannedUntil { get; set; }

    /// <summary>
    /// The reason why the user is banned.
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Whether the user is currently banned.
    /// </summary>
    public bool IsBanned => BannedUntil != null && DateTime.Now < BannedUntil;

    /// <summary>
    /// The user's setting for how many purchases to buy at once.
    /// </summary>
    public int BulkBuy { get; set; } = 1;

    /// <summary>
    /// The user's setting for the music volume.
    /// </summary>
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = value;
            AudioPlaybackEngine.Instance.MusicVolume = value;
        }
    }

    /// <summary>
    /// The user's setting for the sound effect volume.
    /// </summary>
    public float SoundVolume
    {
        get => _soundVolume;
        set
        {
            _soundVolume = value;
            AudioPlaybackEngine.Instance.SoundVolume = value;
        }
    }

    /// <summary>
    /// A list of <see cref="Save"/>s that belong to this user.
    /// </summary>
    public virtual ObservableCollection<Save> Saves { get; set; } = [];

    /// <summary>
    /// Attempts to create a user and save it to the database.
    /// </summary>
    public static User? CreateUser(string username, string password, out string errorMessage)
    {
        errorMessage = "";

        if (!Main.Instance.IsDBAvailable)
        {
            errorMessage = "Cannot create a user because the database is unavailable.";
        }
        else if (username == "")
        {
            errorMessage = "The username must not be empty.";
        }
        else if (password == "")
        {
            errorMessage = "The password must not be empty.";
        }
        else if (Main.Instance.DB.Users.Any(user => user.Name == username))
        {
            errorMessage = "That username is taken.";
        }
        else
        {
            try
            {
                User user = new()
                {
                    Name = username,
                    Password = BC.HashPassword(password),
                    IsAdmin = username.Equals("admin", StringComparison.InvariantCultureIgnoreCase),
                };

                Main.Instance.DB.Users.Add(user);
                Main.Instance.DB.SaveChanges();
                return user;
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show("Something went wrong while trying to create a user.", MessageBoxButton.OK, MessageBoxImage.Error, ex);
                errorMessage = "Something went wrong. Please try again later.";
            }
        }

        return null;
    }
}
