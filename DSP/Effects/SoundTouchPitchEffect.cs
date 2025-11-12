using System;
using System.Reflection;

namespace VoiceChanger.DSP.Effects
{
    // WSOLA-based pitch shifter via SoundTouch.NET when available.
    // Uses reflection to avoid hard dependency; falls back to SMB pitch when missing.
    public class SoundTouchPitchEffect : IEffect
    {
        private object? _st; // SoundTouch instance
        private MethodInfo? _miSetSampleRate;
        private MethodInfo? _miSetChannels;
        private MethodInfo? _miSetPitchSemiTones;
        private MethodInfo? _miPutSamples;
        private MethodInfo? _miReceiveSamples;
        private MethodInfo? _miClear;

        private readonly PitchShiftEffect _fallback = new();
        private int _channels = 1;
        private float _sampleRate = 48000f;
        private volatile float _semi;

        private float[] _work = Array.Empty<float>();

        public void Configure(float sampleRate, int channels)
        {
            _sampleRate = sampleRate;
            _channels = Math.Max(1, channels);
            TryInitSoundTouch();
            if (_st != null)
            {
                _miSetSampleRate?.Invoke(_st, new object[] { (int)_sampleRate });
                _miSetChannels?.Invoke(_st, new object[] { _channels });
            }
            else
            {
                _fallback.Configure(sampleRate, 4096, 16);
            }
        }

        public void SetSemitones(float semitones)
        {
            _semi = Math.Clamp(semitones, -12f, 12f);
            if (_st != null)
            {
                _miSetPitchSemiTones?.Invoke(_st, new object[] { _semi });
            }
            else
            {
                _fallback.SetSemitones(_semi);
            }
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (_st == null)
            {
                _fallback.Process(buffer, offset, count, channels);
                return;
            }

            Ensure(count);
            Array.Copy(buffer, offset, _work, 0, count);

            // put samples
            _miPutSamples?.Invoke(_st, new object[] { _work, 0, count / _channels });

            // receive processed samples
            int received = 0;
            while (received < count)
            {
                object[] args = new object[] { _work, received, (count - received) / _channels };
                int got = (int)(_miReceiveSamples?.Invoke(_st, args) ?? 0);
                if (got <= 0) break;
                received += got * _channels;
            }

            if (received > 0)
            {
                Array.Copy(_work, 0, buffer, offset, Math.Min(received, count));
            }
        }

        private void TryInitSoundTouch()
        {
            if (_st != null) return;
            var asm = TryLoadAssembly("SoundTouch") ?? TryLoadAssembly("SoundTouch.Net") ?? TryLoadAssembly("SoundTouchNet");
            var type = asm?.GetType("SoundTouch.SoundTouch") ?? asm?.GetType("SoundTouchNet.SoundTouch") ?? asm?.GetType("SoundTouchNetCore.SoundTouch");
            if (type == null) return;
            _st = Activator.CreateInstance(type);
            _miSetSampleRate = type.GetMethod("SetSampleRate") ?? type.GetMethod("set_SampleRate");
            _miSetChannels = type.GetMethod("SetChannels") ?? type.GetMethod("set_Channels");
            _miSetPitchSemiTones = type.GetMethod("SetPitchSemiTones") ?? type.GetMethod("set_PitchSemiTones");
            _miPutSamples = type.GetMethod("PutSamples");
            _miReceiveSamples = type.GetMethod("ReceiveSamples");
            _miClear = type.GetMethod("Clear");
        }

        private static Assembly? TryLoadAssembly(string name)
        {
            try { return Assembly.Load(new AssemblyName(name)); } catch { return null; }
        }

        private void Ensure(int n)
        {
            if (_work.Length < n) _work = new float[n];
        }
    }
}
