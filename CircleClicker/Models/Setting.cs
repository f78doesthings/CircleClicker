using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CircleClicker.Models.Database;
using CircleClicker.Utils;

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
    public abstract class Setting<T> : NotifyPropertyChanged, ISetting
        where T : struct
    {
        public required Func<User, T> Getter { private get; init; }
        public required Action<User, T> Setter { private get; init; }

        public string? Name { get; init; }

        /// <inheritdoc cref="ISetting.DefaultValue" />
        public T DefaultValue { get; init; }

        /// <inheritdoc cref="ISetting.Value" />
        public T Value
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

    public class FloatSetting : Setting<float>;

    /// <summary>
    /// Contains serveral <see cref="Setting{T}"/> instances.
    /// </summary>
    public static class Settings
    {
        public static readonly FloatSetting MusicVolume =
            new()
            {
                Name = "Music Volume (%)",
                DefaultValue = 0.5f,
                Getter = u => u.MusicVolume * 100,
                Setter = (u, v) => u.MusicVolume = v / 100,
            };

        public static readonly FloatSetting SoundVolume =
            new()
            {
                Name = "Sound Effect Volume (%)",
                DefaultValue = 0.5f,
                Getter = u => u.SoundVolume * 100,
                Setter = (u, v) => u.SoundVolume = v / 100,
            };
    }
}
