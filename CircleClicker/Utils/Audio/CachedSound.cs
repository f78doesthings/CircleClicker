using System.IO;
using NAudio.Wave;

namespace CircleClicker.Utils.Audio
{
    /// <summary>
    /// Holds wave audio data.
    /// </summary>
    public class CachedSound
    {
        /// <summary>
        /// The cached wave audio data.
        /// </summary>
        public byte[] AudioData { get; }

        /// <summary>
        /// The wave format of the audio data. Should always be <see cref="AudioPlaybackEngine.TargetWaveFormat"/>.
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Creates a new <see cref="CachedSound"/> from a file.<br />
        /// The file must be supported by <see cref="MediaFoundationReader"/>.
        /// </summary>
        public CachedSound(string file)
            : this(new MediaFoundationReader(file)) { }

        /// <summary>
        /// Creates a new <see cref="CachedSound"/> from a stream.<br />
        /// The stream must be supported by <see cref="StreamMediaFoundationReader"/>.
        /// </summary>
        public CachedSound(Stream sourceStream)
            : this(new StreamMediaFoundationReader(sourceStream)) { }

        private CachedSound(WaveStream inputStream)
        {
            using ResamplerDmoStream reader =
                new(inputStream, AudioPlaybackEngine.TargetWaveFormat); // Converts the input into 44.1 kHz, stereo, 32-bit float in order to work with AudioPlaybackEngine
            WaveFormat = reader.WaveFormat;

            List<byte> wholeFile = new((int)(reader.Length / 4));
            byte[] readBuffer = new byte[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];

            int samplesRead;
            while ((samplesRead = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }

            AudioData = [.. wholeFile];
        }
    }
}
