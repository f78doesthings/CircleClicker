using System.Numerics;
using CircleClicker.Models.Database;
using CircleClicker.Utils;
using CircleClicker.Utils.Audio;

namespace CircleClicker.Models
{
    public interface ISetting
    {
        /// <summary>
        /// A list of all <see cref="ISetting"/> instances.
        /// </summary>
        public static readonly List<ISetting> Instances = [];

        /// <summary>
        /// The display name of this setting.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The default value of this setting.
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// The current value of this setting.
        /// </summary>
        public object Value { get; set; }
    }

    /// <summary>
    /// The base class for settings.
    /// </summary>
    /// <typeparam name="T">The type of the setting.</typeparam>
    public abstract class Setting<T> : Observable, ISetting
        where T : struct
    {
        public required Func<User, T> Getter { private get; init; }
        public required Action<User, T> Setter { private get; init; }

        public string? Name { get; init; }

        /// <inheritdoc cref="ISetting.DefaultValue" />
        public T DefaultValue { get; init; }

        /// <inheritdoc cref="ISetting.Value" />
        public virtual T Value
        {
            get => Main.Instance.CurrentUser == null ? default : Getter(Main.Instance.CurrentUser);
            set
            {
                if (Main.Instance.CurrentUser != null)
                {
                    Setter(Main.Instance.CurrentUser, value);
                    OnPropertyChanged();
                }
            }
        }

        object ISetting.DefaultValue => DefaultValue;

        object ISetting.Value
        {
            get => Value;
            set => Value = (T)value;
        }

        public Setting()
        {
            ISetting.Instances.Add(this);
        }
    }

    public abstract class NumberSetting<T> : Setting<T>
        where T : struct, INumber<T>
    {
        /// <summary>
        /// The lowest possible value for this setting. Defaults to 0.
        /// </summary>
        public T Minimum { get; init; }

        /// <summary>
        /// The highest possible value for this setting. Defaults to 100.
        /// </summary>
        public T Maximum { get; init; } = T.Parse("100", null);

        public override T Value
        {
            get => base.Value;
            set => base.Value = T.Clamp(value, Minimum, Maximum);
        }
    }

    public class FloatSetting : NumberSetting<float>;

    /// <summary>
    /// Contains serveral <see cref="Setting{T}"/> instances.
    /// </summary>
    public static class Settings
    {
        /// <inheritdoc cref="User.MusicVolume"/>
        public static readonly FloatSetting MusicVolume =
            new()
            {
                Name = "Music Volume (%)",
                DefaultValue = 0.5f,

                Getter = u => u.MusicVolume * 100,
                Setter = (u, v) =>
                {
                    u.MusicVolume = v / 100;
                    AudioPlaybackEngine.Instance.MusicVolume = v / 100;
                },
            };

        /// <inheritdoc cref="User.SoundVolume"/>
        public static readonly FloatSetting SoundVolume =
            new()
            {
                Name = "Sound Effect Volume (%)",
                DefaultValue = 0.5f,

                Getter = u => u.SoundVolume * 100,
                Setter = (u, v) =>
                {
                    u.SoundVolume = v / 100;
                    AudioPlaybackEngine.Instance.SoundVolume = v / 100;
                },
            };
    }
}
