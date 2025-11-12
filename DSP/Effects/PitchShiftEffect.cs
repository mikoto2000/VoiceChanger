using System;
using NAudio.Dsp;

namespace VoiceChanger.DSP.Effects
{
    public class PitchShiftEffect : IEffect
    {
        private volatile float _semitones = 0f;
        private volatile int _fftSize = 2048;
        private volatile int _osamp = 8;
        private volatile float _sampleRate = 48000f;

        private float[] _frameBufferL;
        private float[] _frameBufferR;
        private readonly SmbPitchShifter _shifter = new();

        public PitchShiftEffect()
        {
            _frameBufferL = new float[4096];
            _frameBufferR = new float[4096];
        }

        public void Configure(float sampleRate, int fftSize, int oversampling)
        {
            _sampleRate = sampleRate;
            _fftSize = fftSize;
            _osamp = oversampling;
        }

        public void SetSemitones(float semitones)
        {
            _semitones = Math.Clamp(semitones, -12f, 12f);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (Math.Abs(_semitones) < 0.001f) return;
            float pitch = (float)Math.Pow(2.0, _semitones / 12.0);
            EnsureCapacity(count / Math.Max(1, channels) + 16);
            if (channels == 1)
            {
                Array.Copy(buffer, offset, _frameBufferL, 0, count);
                _shifter.PitchShift(pitch, count, _fftSize, _osamp, _sampleRate, _frameBufferL);
                Array.Copy(_frameBufferL, 0, buffer, offset, count);
            }
            else
            {
                int samples = count / 2;
                for (int i = 0; i < samples; i++)
                {
                    _frameBufferL[i] = buffer[offset + 2 * i];
                    _frameBufferR[i] = buffer[offset + 2 * i + 1];
                }
                _shifter.PitchShift(pitch, samples, _fftSize, _osamp, _sampleRate, _frameBufferL);
                _shifter.PitchShift(pitch, samples, _fftSize, _osamp, _sampleRate, _frameBufferR);
                for (int i = 0; i < samples; i++)
                {
                    buffer[offset + 2 * i] = _frameBufferL[i];
                    buffer[offset + 2 * i + 1] = _frameBufferR[i];
                }
            }
        }

        private void EnsureCapacity(int needed)
        {
            if (_frameBufferL.Length < needed)
            {
                _frameBufferL = new float[needed];
            }
            if (_frameBufferR.Length < needed)
            {
                _frameBufferR = new float[needed];
            }
        }
    }
}
