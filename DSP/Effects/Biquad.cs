using System;

namespace VoiceChanger.DSP.Effects
{
    public class Biquad
    {
        private float a0, a1, a2, b1, b2;
        private float z1L, z2L, z1R, z2R;

        public void SetPeaking(float sampleRate, float centerHz, float q, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (centerHz / sampleRate);
            float alpha = (float)Math.Sin(w0) / (2 * q);
            float cosw0 = (float)Math.Cos(w0);

            float b0 = 1 + alpha * A;
            float b1n = -2 * cosw0;
            float b2 = 1 - alpha * A;
            float a0n = 1 + alpha / A;
            float a1n = -2 * cosw0;
            float a2n = 1 - alpha / A;

            a0 = b0 / a0n;
            a1 = b1n / a0n;
            a2 = b2 / a0n;
            b1 = a1n / a0n;
            b2 = a2n / a0n;
        }

        public void SetLowShelf(float sampleRate, float cutoffHz, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (cutoffHz / sampleRate);
            float cosw0 = (float)Math.Cos(w0);
            float sinw0 = (float)Math.Sin(w0);
            float S = 1f;
            float alpha = sinw0 / 2 * (float)Math.Sqrt((A + 1 / A) * (1 / S - 1) + 2);

            float b0n = A * ((A + 1) - (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha);
            float b1n = 2 * A * ((A - 1) - (A + 1) * cosw0);
            float b2n = A * ((A + 1) - (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha);
            float a0n = (A + 1) + (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha;
            float a1n = -2 * ((A - 1) + (A + 1) * cosw0);
            float a2n = (A + 1) + (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha;

            a0 = b0n / a0n;
            a1 = b1n / a0n;
            a2 = b2n / a0n;
            b1 = a1n / a0n;
            b2 = a2n / a0n;
        }

        public void SetHighShelf(float sampleRate, float cutoffHz, float dbGain)
        {
            float A = (float)Math.Pow(10, dbGain / 40);
            float w0 = 2 * (float)Math.PI * (cutoffHz / sampleRate);
            float cosw0 = (float)Math.Cos(w0);
            float sinw0 = (float)Math.Sin(w0);
            float S = 1f;
            float alpha = sinw0 / 2 * (float)Math.Sqrt((A + 1 / A) * (1 / S - 1) + 2);

            float b0n = A * ((A + 1) + (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha);
            float b1n = -2 * A * ((A - 1) + (A + 1) * cosw0);
            float b2n = A * ((A + 1) + (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha);
            float a0n = (A + 1) - (A - 1) * cosw0 + 2 * (float)Math.Sqrt(A) * alpha;
            float a1n = 2 * ((A - 1) - (A + 1) * cosw0);
            float a2n = (A + 1) - (A - 1) * cosw0 - 2 * (float)Math.Sqrt(A) * alpha;

            a0 = b0n / a0n;
            a1 = b1n / a0n;
            a2 = b2n / a0n;
            b1 = a1n / a0n;
            b2 = a2n / a0n;
        }

        public void Reset()
        {
            z1L = z2L = z1R = z2R = 0f;
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            int end = offset + count;
            if (channels == 1)
            {
                for (int i = offset; i < end; i++)
                {
                    float x = buffer[i];
                    float y = a0 * x + a1 * z1L + a2 * z2L - b1 * z1L - b2 * z2L;
                    z2L = z1L;
                    z1L = y;
                    buffer[i] = y;
                }
            }
            else
            {
                for (int i = offset; i < end; i += 2)
                {
                    float xl = buffer[i];
                    float yl = a0 * xl + a1 * z1L + a2 * z2L - b1 * z1L - b2 * z2L;
                    z2L = z1L;
                    z1L = yl;
                    buffer[i] = yl;

                    float xr = buffer[i + 1];
                    float yr = a0 * xr + a1 * z1R + a2 * z2R - b1 * z1R - b2 * z2R;
                    z2R = z1R;
                    z1R = yr;
                    buffer[i + 1] = yr;
                }
            }
        }
    }
}

