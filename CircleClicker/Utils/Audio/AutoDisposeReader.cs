using NAudio.Wave;

namespace CircleClicker.Utils.Audio
{
    // Currently unused, may be used later if I add music

    /// <summary>
    /// An <see cref="IWaveProvider"/> that automatically disposes resources when reaching the end of the stream.
    /// </summary>
    /// <param name="provider">The wave data provider. If this inherits <see cref="IDisposable"/>, it will automatically be disposed.</param>
    /// <param name="disposables">Extra objects to automatically dispose.</param>
    public class AutoDisposeReader(IWaveProvider provider, params IDisposable[] disposables)
        : IWaveProvider
    {
        private bool _disposed;

        public WaveFormat WaveFormat { get; } = provider.WaveFormat;

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                return 0;
            }

            int bytesRead = provider.Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                _disposed = true;

                if (provider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }

                foreach (IDisposable disposable in disposables)
                {
                    disposable.Dispose();
                }
            }

            return bytesRead;
        }
    }
}
