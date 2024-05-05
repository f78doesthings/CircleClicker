using NAudio.Wave;

namespace CircleClicker.Utils.Audio
{
    /// <summary>
    /// An <see cref="IWaveProvider"/> for <see cref="CachedSound"/>s.
    /// </summary>
    public class CachedSoundProvider(CachedSound cachedSound) : IWaveProvider
    {
        private readonly CachedSound _cachedSound = cachedSound;
        private long _position;

        public WaveFormat WaveFormat => _cachedSound.WaveFormat;

        public int Read(byte[] buffer, int offset, int count)
        {
            long availableSamples = _cachedSound.AudioData.LongLength - _position;
            long samplesToCopy = Math.Min(availableSamples, count);

            Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;

            return (int)samplesToCopy;
        }
    }
}
