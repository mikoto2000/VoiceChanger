namespace VoiceChanger.DSP
{
    public interface IEffect
    {
        void Process(float[] buffer, int offset, int count, int channels);
    }
}

