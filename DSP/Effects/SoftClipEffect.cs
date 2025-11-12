using System;

namespace VoiceChanger.DSP.Effects
{
    public class SoftClipEffect : IEffect
    {
        // 0 = bypass, 1 = full soft clip
        private volatile float _amount = 0.6f;

        public float Amount
        {
            get => _amount;
            set => _amount = Math.Clamp(value, 0f, 1f);
        }

        // Cubic soft clipper blended with dry: y = (1-a)*x + a*softclip(x)
        // softclip(x):
        //   if |x| >= 2/3 -> sign(x)
        //   else -> x - (x^3)/3
        public void Process(float[] buffer, int offset, int count, int channels)
        {
            var amt = _amount;
            if (amt <= 0f) return;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                float x = buffer[i];
                float y;
                if (x >= 0.6666667f)
                    y = 1f;
                else if (x <= -0.6666667f)
                    y = -1f;
                else
                    y = x - (x * x * x) / 3f;

                buffer[i] = (1f - amt) * x + amt * y;
            }
        }
    }
}

