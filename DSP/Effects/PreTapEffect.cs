using System;

namespace VoiceChanger.DSP.Effects
{
    public class PreTapEffect : IEffect
    {
        private float[] _buffer = Array.Empty<float>();
        private int _length;
        public void Process(float[] buffer, int offset, int count, int channels)
        {
            Ensure(count);
            Array.Copy(buffer, offset, _buffer, 0, count);
            _length = count;
        }
        private void Ensure(int count)
        {
            if (_buffer.Length < count) _buffer = new float[count];
        }
        public ReadOnlySpan<float> GetCapturedSpan() => new ReadOnlySpan<float>(_buffer, 0, _length);
    }
}

