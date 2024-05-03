using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace CircleClicker.Utils.Audio
{
    /// <summary>
    /// Holds wave audio data.
    /// </summary>
    public class CachedSound
    {
        public byte[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

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
