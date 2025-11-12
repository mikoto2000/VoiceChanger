using System;

namespace VoiceChanger.DSP.Effects
{
    public class GainEffect : IEffect
    {
        private volatile float _gain = 1.0f;

        public float Gain
        {
            get => _gain;
            set => _gain = Math.Clamp(value, 0f, 10f);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            var g = _gain;
            if (Math.Abs(g - 1f) < 1e-6) return;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                buffer[i] *= g;
            }
        }
    }
}

