using System;

namespace VoiceChanger.DSP.Effects
{
    public class NoiseGateEffect : IEffect
    {
        private volatile float _threshold = 0.01f;
        private volatile float _release = 0.05f;
        private float _env;

        public void Configure(float threshold, float release)
        {
            _threshold = Math.Clamp(threshold, 0f, 0.2f);
            _release = Math.Clamp(release, 0.001f, 0.5f);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                float x = buffer[i];
                _env = Math.Max(Math.Abs(x), _env - _release);
                if (_env < _threshold)
                {
                    buffer[i] = 0f;
                }
            }
        }
    }
}

