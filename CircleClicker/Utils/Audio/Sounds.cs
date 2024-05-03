using System.Windows;
using System.Windows.Resources;

namespace CircleClicker.Utils.Audio
{
    /// <summary>
    /// Contains several <see cref="CachedSound"/> instances.
    /// </summary>
    public static class Sounds
    {
        public static readonly CachedSound Collect = FromResource(nameof(Collect));
        public static readonly CachedSound ClickOn = FromResource(nameof(ClickOn));
        public static readonly CachedSound ClickOff = FromResource(nameof(ClickOff));

        /// <summary>
        /// Creates a new <see cref="CachedSound"/> from a resource. The default location is <c>Resources/Sounds/*.mp3</c>.
        /// </summary>
        private static CachedSound FromResource(
            string name,
            string path = "Resources/Sounds",
            string format = "mp3"
        )
        {
            StreamResourceInfo sri = Application.GetResourceStream(
                new Uri($"pack://application:,,,/{path}/{name}.{format}")
            );
            return new CachedSound(sri.Stream);
        }
    }
}
