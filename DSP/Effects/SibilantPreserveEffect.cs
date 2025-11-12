using System;

namespace VoiceChanger.DSP.Effects
{
    public class SibilantPreserveEffect : IEffect
    {
        private readonly PreTapEffect _pre;
        private readonly Biquad _hp = new();
        private readonly Biquad _lp = new();
        private float _sr = 48000f;
        private volatile float _mix = 0.15f;
        private float[] _work = Array.Empty<float>();

        public SibilantPreserveEffect(PreTapEffect pre) { _pre = pre; }

        public void Configure(float sampleRate, float hpHz = 5000f, float lpHz = 12000f)
        {
            _sr = sampleRate;
            _hp.SetHighpass(_sr, hpHz, 0.707f);
            _lp.SetLowpass(_sr, lpHz, 0.707f);
        }

        public void SetMix(float mix) { _mix = Math.Clamp(mix, 0f, 0.5f); }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (_mix <= 0f) return;
            var dry = _pre.GetCapturedSpan();
            if (dry.Length < count) return;
            Ensure(count);
            dry.Slice(0, count).CopyTo(_work);
            _hp.Process(_work, 0, count, channels);
            _lp.Process(_work, 0, count, channels);
            int end = offset + count; int j = 0; float m = _mix;
            for (int i = offset; i < end; i++, j++)
            {
                float v = buffer[i] + _work[j] * m;
                if (v > 1f) v = 1f; else if (v < -1f) v = -1f;
                buffer[i] = v;
            }
        }

        private void Ensure(int n) { if (_work.Length < n) _work = new float[n]; }
    }
}

