using System;
using System.Buffers;
using System.Collections.Generic;
using NAudio.Wave;

namespace VoiceChanger.DSP
{
    public class EffectChain : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly List<IEffect> _effects = new();

        public EffectChain(ISampleProvider source)
        {
            _source = source;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public void SetEffects(IEnumerable<IEffect> effects)
        {
            _effects.Clear();
            _effects.AddRange(effects);
        }

        public int Read(Span<float> buffer)
        {
            int read = _source.Read(buffer);
            if (read <= 0) return read;

            float[] rented = ArrayPool<float>.Shared.Rent(read);
            try
            {
                buffer[..read].CopyTo(rented);
                foreach (var effect in _effects)
                {
                    effect.Process(rented, 0, read, WaveFormat.Channels);
                }
                rented.AsSpan(0, read).CopyTo(buffer);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(rented);
            }
            return read;
        }
    }
}
