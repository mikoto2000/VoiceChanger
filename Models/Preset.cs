namespace VoiceChanger.Models
{
    public class Preset
    {
        public float InputGain { get; set; } = 1.0f;
        public float PitchSemitones { get; set; } = 0f;
        public float FormantShift { get; set; } = 0f;
        public float EqLowDb { get; set; } = 0f;
        public float EqMidDb { get; set; } = 0f;
        public float EqHighDb { get; set; } = 0f;
        public int EchoMs { get; set; } = 150;
        public float EchoFeedback { get; set; } = 0.3f;
        public float EchoMix { get; set; } = 0.2f;
        public float GateThreshold { get; set; } = 0.01f;
        public float GateRelease { get; set; } = 0.05f;
        public float OutputGain { get; set; } = 1.0f;
    }
}

