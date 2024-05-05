using System.Collections.ObjectModel;
using CircleClicker.Utils.Audio;

namespace CircleClicker.Models.Database;

public partial class User(string name, string password)
{
    private float _musicVolume = Settings.MusicVolume.DefaultValue;
    private float _soundVolume = Settings.SoundVolume.DefaultValue;

    public int Id { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The hashed password of this user.
    /// </summary>
    public string Password { get; set; } = password;

    /// <summary>
    /// Whether this user can access administrator-only features, such as the admin panel.
    /// </summary>
    public bool IsAdmin { get; set; }

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
}
