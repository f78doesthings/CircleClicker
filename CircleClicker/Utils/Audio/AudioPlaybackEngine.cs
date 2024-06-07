using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundTouch.Net.NAudioSupport;

// Everything under the CircleClicker.Utility.Audio namespace is adapted from https://markheath.net/post/fire-and-forget-audio-playback-with
// as well as https://github.com/owoudenberg/soundtouch.net/blob/master/src/Example/SoundProcessor.cs.
namespace CircleClicker.Utils.Audio
{
    /// <summary>
    /// Handles audio playback.
    /// </summary>
    public class AudioPlaybackEngine : Observable, IDisposable
    {
        /// <summary>
        /// The wave format that <see cref="AudioPlaybackEngine"/> will use.
        /// </summary>
        public static readonly WaveFormat TargetWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
            44100,
            2
        );

        public static readonly AudioPlaybackEngine Instance = new();

        private readonly WasapiOut _output;
        private readonly MixingSampleProvider _mixer;
        private MusicProvider? _musicPlayer;

        public MusicProvider? MusicPlayer
        {
            get => _musicPlayer;
            private set
            {
                _musicPlayer = value;
                OnPropertyChanged();
            }
        }

        private float _musicVolume = 0.5f;

        /// <summary>
        /// The volume of the music.
        /// </summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;

                if (MusicPlayer != null)
                {
                    MusicPlayer.Volume = value;

                    if (value > 0)
                    {
                        PlayMusic();
                    }
                    else
                    {
                        PauseMusic();
                    }
                }
            }
        }

        /// <summary>
        /// The volume of the sound effects.
        /// </summary>
        public float SoundVolume { get; set; } = 0.5f;

        public AudioPlaybackEngine()
        {
            _output = new WasapiOut(AudioClientShareMode.Shared, 100);
            _mixer = new MixingSampleProvider(TargetWaveFormat) { ReadFully = true };

            _output.Init(_mixer);
            _output.Play();
        }

        /// <summary>
        /// Plays a cached sound.
        /// </summary>
        /// <param name="sound">The sound to play. Cached sounds can be found in <see cref="Sounds"/>.</param>
        /// <param name="volume">The volume to play the sound at. Should be between 0 and 1.</param>
        /// <param name="rate">The playback speed of the sound. The normal speed is 1.</param>
        /// <param name="variation">How much to randomize the playback speed by.</param>
        public void PlaySound(
            CachedSound sound,
            float volume = 1f,
            double rate = 1,
            double variation = 0
        )
        {
            if (SoundVolume <= 0)
            {
                return;
            }

            rate *= (1 + Random.Shared.NextDouble() * variation * 2 - variation);

            CachedSoundProvider input = new(sound);
            SoundTouchWaveProvider processor = new(input) { Rate = rate };
            VolumeSampleProvider volumeProvider =
                new(processor.ToSampleProvider()) { Volume = SoundVolume * volume };

            _mixer.AddMixerInput(volumeProvider);
        }

        /// <summary>
        /// Starts or resumes music playback.
        /// </summary>
        public void PlayMusic()
        {
            MusicPlayer ??= new("Resources/Music/") { Volume = MusicVolume };

            if (MusicVolume <= 0)
            {
                return;
            }

            if (!_mixer.MixerInputs.Any(v => v == MusicPlayer))
            {
                _mixer.AddMixerInput(MusicPlayer);
            }
        }

        /// <summary>
        /// Pauses music playback.
        /// </summary>
        public void PauseMusic()
        {
            if (MusicPlayer != null)
            {
                _mixer.RemoveMixerInput(MusicPlayer);
            }
        }

        public void Dispose()
        {
            MusicPlayer?.Dispose();
            _output.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
