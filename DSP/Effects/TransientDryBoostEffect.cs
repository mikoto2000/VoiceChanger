using System;

namespace VoiceChanger.DSP.Effects
{
    public class TransientDryBoostEffect : IEffect
    {
        private readonly PreTapEffect _pre;
        private volatile float _mix = 0.25f;       // max dry mix on strong transient
        private volatile float _sensitivity = 0.2f; // lower is more sensitive
        private float[] _prev = Array.Empty<float>();

        public TransientDryBoostEffect(PreTapEffect pre) { _pre = pre; }

        public void Configure(float mix, float sensitivity)
        {
            _mix = Math.Clamp(mix, 0f, 0.7f);
            _sensitivity = Math.Clamp(sensitivity, 0.05f, 1f);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            var dry = _pre.GetCapturedSpan();
            if (dry.Length < count || _mix <= 0f) return;
            Ensure(count);
            int end = offset + count;
            float mixMax = _mix;
            for (int i = 0, bi = offset; bi < end; i++, bi++)
            {
                float x = buffer[bi];
                float d = Math.Abs(x - _prev[i]);
                _prev[i] = x;
                // Simple transient weight: normalize by sensitivity
                float w = d / (_sensitivity + 1e-6f);
                if (w > 1f) w = 1f;
                float m = w * mixMax;
                float v = x * (1 - m) + dry[i] * m;
                buffer[bi] = v;
            }
        }

        private void Ensure(int n)
        {
            if (_prev.Length < n) _prev = new float[n];
        }
    }
}

