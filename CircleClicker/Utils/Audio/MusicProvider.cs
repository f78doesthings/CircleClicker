using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CircleClicker.Utils.Audio
{
    // Adapted from https://markheath.net/post/looped-playback-in-net-with-naudio
    // and https://github.com/naudio/NAudio/blob/master/NAudio.Extras/LoopStream.cs
    public class MusicProvider : NotifyPropertyChanged, ISampleProvider, IDisposable
    {
        /// <summary>
        /// Raised when none of the files of a <see cref="MusicProvider"/> can be opened for playback.
        /// </summary>
        public class NoFilesAvailableException : Exception
        {
            public NoFilesAvailableException(
                IReadOnlyDictionary<string, Exception>? exceptions = null
            )
                : this("Could not open any of the files of the MusicStream.", exceptions) { }

            public NoFilesAvailableException(
                string message,
                IReadOnlyDictionary<string, Exception>? exceptions = null
            )
                : base(message)
            {
                if (exceptions != null)
                {
                    foreach (var kvp in exceptions)
                    {
                        Data.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        private readonly string[] _playlist;
        private int _index = -1;
        private float _volume = 0.5f;
        private MediaFoundationReader _reader;
        private VolumeSampleProvider _provider;

        /// <summary>
        /// Returns the name of the file that is currently being played.
        /// </summary>
        public string CurrentFile => Path.GetFileNameWithoutExtension(_playlist[_index]);

        /// <summary>
        /// Raised when none of the files belonging to the <see cref="MusicProvider"/> are available.
        /// </summary>
        public event Action<Exception>? NoFilesAvailable;

        /// <summary>
        /// Raised when an error occured during <see cref="Read"/>.
        /// </summary>
        public event Action<Exception>? ReadFailed;

        public WaveFormat WaveFormat => _provider.WaveFormat;

        public float Volume
        {
            get => _provider.Volume;
            set
            {
                _volume = value;
                _provider.Volume = value;
            }
        }

        public MusicProvider(string path)
        {
            _playlist = Directory.GetFiles(path);
            if (_playlist.Length == 0)
            {
                throw new DirectoryNotFoundException($"The directory \"{path}\" is empty.");
            }

            Next();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            try
            {
                int totalBytesRead = 0;
                while (totalBytesRead < count)
                {
                    int requiredBytes = count - totalBytesRead;
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = _provider.Read(buffer, offset + totalBytesRead, requiredBytes);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"MusicProvider.Read failed (file {CurrentFile}):\n{ex}",
                            App.ErrorCategory
                        );
                        ReadFailed?.Invoke(ex);
                        Next();
                    }

                    if (bytesRead < requiredBytes)
                    {
                        Next();
                    }

                    totalBytesRead += bytesRead;
                }

                return totalBytesRead;
            }
            catch (Exception ex)
            {
                NoFilesAvailable?.Invoke(ex);
                return 0;
            }
        }

        /// <summary>
        /// Proceeds to the next track in the playlist.
        /// </summary>
        [MemberNotNull(nameof(_provider), nameof(_reader))]
        public void Next()
        {
            Next([]);
        }

        [MemberNotNull(nameof(_provider), nameof(_reader))]
        private void Next(Dictionary<string, Exception> exceptions)
        {
            _reader?.Dispose();

            _index = (_index + 1) % _playlist.Length;
            string file = _playlist[_index];

            if (exceptions.ContainsKey(file))
            {
                Dispose();
                throw new NoFilesAvailableException(exceptions);
            }

            try
            {
                _reader = new MediaFoundationReader(file);
                // Resample to the correct format (note: ResamplerDmoStream doesn't work here due to threading issues)
                WdlResamplingSampleProvider resampler =
                    new(
                        _reader.ToSampleProvider(),
                        AudioPlaybackEngine.TargetWaveFormat.SampleRate
                    );
                _provider = new VolumeSampleProvider(resampler) { Volume = _volume };

                InvokePropertyChanged(nameof(CurrentFile));
            }
            catch (Exception ex)
            {
                _reader = null!;
                _provider = null!;
                Debug.WriteLine(
                    $"MusicProvider.Next failed (file {file}):\n{ex}",
                    App.ErrorCategory
                );
                exceptions.Add(file, ex);
                Next(exceptions);
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null!;
            _provider = null!;
            GC.SuppressFinalize(this);
        }
    }
}
