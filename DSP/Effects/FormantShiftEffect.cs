using System;

namespace VoiceChanger.DSP.Effects
{
    public class FormantShiftEffect : IEffect
    {
        private volatile float _shift = 0f;
        private readonly Biquad _f1 = new();
        private readonly Biquad _f2 = new();
        private readonly Biquad _f3 = new();
        private volatile float _sampleRate = 48000f;

        public void Configure(float sampleRate)
        {
            _sampleRate = sampleRate;
            UpdateFilters();
        }

        public void SetShift(float shift)
        {
            _shift = Math.Clamp(shift, -1f, 1f);
            UpdateFilters();
        }

        private void UpdateFilters()
        {
            float scale = (float)Math.Pow(2.0, _shift * 0.5); // ~±1: ~±1/2 octave
            _f1.SetPeaking(_sampleRate, 700f * scale, 1.2f, 4f);
            _f2.SetPeaking(_sampleRate, 1200f * scale, 1.2f, 3f);
            _f3.SetPeaking(_sampleRate, 3000f * scale, 1.5f, 2f);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (Math.Abs(_shift) < 1e-4) return;
            _f1.Process(buffer, offset, count, channels);
            _f2.Process(buffer, offset, count, channels);
            _f3.Process(buffer, offset, count, channels);
        }
    }
}

