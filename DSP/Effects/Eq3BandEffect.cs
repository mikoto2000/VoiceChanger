using System;

namespace VoiceChanger.DSP.Effects
{
    public class Eq3BandEffect : IEffect
    {
        private readonly Biquad _low = new();
        private readonly Biquad _mid = new();
        private readonly Biquad _high = new();
        private volatile float _lowGainDb;
        private volatile float _midGainDb;
        private volatile float _highGainDb;
        private volatile float _sampleRate = 48000f;

        public void Update(float sampleRate, float lowDb, float midDb, float highDb)
        {
            _sampleRate = sampleRate;
            _lowGainDb = Math.Clamp(lowDb, -24f, 24f);
            _midGainDb = Math.Clamp(midDb, -24f, 24f);
            _highGainDb = Math.Clamp(highDb, -24f, 24f);
            Configure();
        }

        private void Configure()
        {
            _low.SetLowShelf(_sampleRate, 200f, _lowGainDb);
            _mid.SetPeaking(_sampleRate, 1000f, 1.0f, _midGainDb);
            _high.SetHighShelf(_sampleRate, 4000f, _highGainDb);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            _low.Process(buffer, offset, count, channels);
            _mid.Process(buffer, offset, count, channels);
            _high.Process(buffer, offset, count, channels);
        }
    }
}

