using System;
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

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            if (read <= 0) return read;
            foreach (var effect in _effects)
            {
                effect.Process(buffer, offset, read, WaveFormat.Channels);
            }
            return read;
        }
    }
}

