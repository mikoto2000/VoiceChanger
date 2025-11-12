using System;

namespace VoiceChanger.DSP.Effects
{
    public class EchoEffect : IEffect
    {
        private float[] _delayBuffer = Array.Empty<float>();
        private int _writeIndex;
        private volatile int _delaySamples = 2400;
        private volatile float _feedback = 0.3f;
        private volatile float _mix = 0.2f;
        private volatile int _channels = 2;

        public void Configure(float sampleRate, int channels, int delayMs, float feedback, float mix)
        {
            _channels = Math.Max(1, channels);
            _delaySamples = Math.Max(1, (int)(sampleRate * delayMs / 1000.0f)) * _channels;
            _feedback = Math.Clamp(feedback, 0f, 0.95f);
            _mix = Math.Clamp(mix, 0f, 1f);
            int size = _delaySamples * 4;
            if (_delayBuffer.Length != size)
            {
                _delayBuffer = new float[size];
                _writeIndex = 0;
            }
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (_delayBuffer.Length == 0) return;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                int readIndex = _writeIndex - _delaySamples;
                if (readIndex < 0) readIndex += _delayBuffer.Length;
                float delayed = _delayBuffer[readIndex];
                float input = buffer[i];
                float wet = input + delayed * _mix;
                _delayBuffer[_writeIndex] = input + delayed * _feedback;
                _writeIndex++;
                if (_writeIndex >= _delayBuffer.Length) _writeIndex = 0;
                buffer[i] = wet;
            }
        }
    }
}

