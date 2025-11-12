using System;

namespace VoiceChanger.DSP.Effects
{
    public class Biquad
    {
        // Normalized coefficients (a0 = 1)
        private float _b0, _b1, _b2, _a1, _a2;

        // States per channel
        private float _x1L, _x2L, _y1L, _y2L;
        private float _x1R, _x2R, _y1R, _y2R;

        public void SetPeaking(float sampleRate, float centerHz, float q, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (centerHz / sampleRate);
            float alpha = (float)Math.Sin(w0) / (2 * q);
            float cosw0 = (float)Math.Cos(w0);

            float b0 = 1 + alpha * A;
            float b1 = -2 * cosw0;
            float b2 = 1 - alpha * A;
            float a0 = 1 + alpha / A;
            float a1 = -2 * cosw0;
            float a2 = 1 - alpha / A;

            NormalizeAndSet(b0, b1, b2, a0, a1, a2);
        }

        public void SetLowShelf(float sampleRate, float cutoffHz, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (cutoffHz / sampleRate);
            float cosw0 = (float)Math.Cos(w0);
            float sinw0 = (float)Math.Sin(w0);
            float S = 1f;
            float alpha = sinw0 / 2 * (float)Math.Sqrt((A + 1 / A) * (1 / S - 1) + 2);

            float b0 = A * ((A + 1) - (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha);
            float b1 = 2 * A * ((A - 1) - (A + 1) * cosw0);
            float b2 = A * ((A + 1) - (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha);
            float a0 = (A + 1) + (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha;
            float a1 = -2 * ((A - 1) + (A + 1) * cosw0);
            float a2 = (A + 1) + (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha;

            NormalizeAndSet(b0, b1, b2, a0, a1, a2);
        }

        public void SetHighShelf(float sampleRate, float cutoffHz, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (cutoffHz / sampleRate);
            float cosw0 = (float)Math.Cos(w0);
            float sinw0 = (float)Math.Sin(w0);
            float S = 1f;
            float alpha = sinw0 / 2 * (float)Math.Sqrt((A + 1 / A) * (1 / S - 1) + 2);

            float b0 = A * ((A + 1) + (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha);
            float b1 = -2 * A * ((A - 1) + (A + 1) * cosw0);
            float b2 = A * ((A + 1) + (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha);
            float a0 = (A + 1) - (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha;
            float a1 = 2 * ((A - 1) - (A + 1) * cosw0);
            float a2 = (A + 1) - (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha;

            NormalizeAndSet(b0, b1, b2, a0, a1, a2);
        }

        private void NormalizeAndSet(float b0, float b1, float b2, float a0, float a1, float a2)
        {
            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
            Reset();
        }

        public void Reset()
        {
            _x1L = _x2L = _y1L = _y2L = 0f;
            _x1R = _x2R = _y1R = _y2R = 0f;
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            int end = offset + count;
            if (channels == 1)
            {
                for (int i = offset; i < end; i++)
                {
                    float x = buffer[i];
                    float y = _b0 * x + _b1 * _x1L + _b2 * _x2L - _a1 * _y1L - _a2 * _y2L;
                    _x2L = _x1L; _x1L = x; _y2L = _y1L; _y1L = y;
                    buffer[i] = y;
                }
            }
            else
            {
                for (int i = offset; i < end; i += 2)
                {
                    float xl = buffer[i];
                    float yl = _b0 * xl + _b1 * _x1L + _b2 * _x2L - _a1 * _y1L - _a2 * _y2L;
                    _x2L = _x1L; _x1L = xl; _y2L = _y1L; _y1L = yl;
                    buffer[i] = yl;

                    float xr = buffer[i + 1];
                    float yr = _b0 * xr + _b1 * _x1R + _b2 * _x2R - _a1 * _y1R - _a2 * _y2R;
                    _x2R = _x1R; _x1R = xr; _y2R = _y1R; _y1R = yr;
                    buffer[i + 1] = yr;
                }
            }
        }
    }
}
