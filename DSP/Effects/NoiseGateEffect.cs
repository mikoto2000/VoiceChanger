using System;

namespace VoiceChanger.DSP.Effects
{
    public class NoiseGateEffect : IEffect
    {
        private volatile float _threshold = 0.003f;
        private volatile float _release = 0.1f;
        private volatile float _floor = 0.2f; // attenuation when below threshold
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
                    // Soft gate: attenuate instead of hard mute to avoid choppiness
                    buffer[i] = x * _floor;
                }
            }
        }
    }
}
