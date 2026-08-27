using System;
using System.Collections.Generic;
using System.Reflection;

namespace VoiceChanger.DSP.Effects
{
    // SoundTouch returns a variable number of frames and has an internal delay.
    // Buffer its output so processed and dry audio are never spliced together.
    public class SoundTouchPitchEffect : IEffect
    {
        private object? _soundTouch;
        private Type? _arrayPtrType;
        private MethodInfo? _setPitchSemiTones;
        private MethodInfo? _putSamples;
        private MethodInfo? _receiveSamples;
        private MethodInfo? _arrayToPtr;

        private readonly PitchShiftEffect _fallback = new();
        private readonly Queue<float> _output = new();
        private int _channels = 1;
        private float _targetSemitones;
        private float _currentSemitones;
        private float[] _input = Array.Empty<float>();
        private float[] _received = Array.Empty<float>();

        public void Configure(float sampleRate, int channels)
        {
            _channels = Math.Max(1, channels);
            _output.Clear();

            if (!TryInitSoundTouch())
            {
                _fallback.Configure(sampleRate, 4096, 16);
                _fallback.SetSemitones(_targetSemitones);
                return;
            }

            Invoke("SetSampleRate", (int)sampleRate);
            Invoke("SetChannels", _channels);
            Invoke("Clear");
            SetSoundTouchPitch(_targetSemitones);
            _currentSemitones = _targetSemitones;
        }

        public void SetSemitones(float semitones)
        {
            _targetSemitones = Math.Clamp(semitones, -12f, 12f);
            if (_soundTouch == null) _fallback.SetSemitones(_targetSemitones);
        }

        public void Process(float[] buffer, int offset, int count, int channels)
        {
            if (count <= 0) return;
            if (_soundTouch == null || _arrayToPtr == null || _putSamples == null || _receiveSamples == null)
            {
                _fallback.Process(buffer, offset, count, channels);
                return;
            }

            // Smooth slider changes to avoid zipper/warble noise.
            _currentSemitones += (_targetSemitones - _currentSemitones) * 0.08f;
            SetSoundTouchPitch(_currentSemitones);

            EnsureBuffers(count);
            Array.Copy(buffer, offset, _input, 0, count);
            object inputPtr = _arrayToPtr.Invoke(null, new object[] { _input })!;
            _putSamples.Invoke(_soundTouch, new[] { inputPtr, (object)(count / _channels) });

            int receiveFrames = Math.Max(1, _received.Length / _channels);
            while (true)
            {
                object receivePtr = _arrayToPtr.Invoke(null, new object[] { _received })!;
                int frames = (int)(_receiveSamples.Invoke(_soundTouch,
                    new[] { receivePtr, (object)receiveFrames }) ?? 0);
                if (frames <= 0) break;
                int samples = frames * _channels;
                for (int i = 0; i < samples; i++) _output.Enqueue(_received[i]);
            }

            // Silence is preferable during initial latency. Mixing dry samples into
            // processed output creates comb filtering and the characteristic metallic tone.
            for (int i = 0; i < count; i++)
                buffer[offset + i] = _output.Count > 0 ? _output.Dequeue() : 0f;
        }

        private bool TryInitSoundTouch()
        {
            if (_soundTouch != null) return true;
            try
            {
                Assembly? assembly = TryLoadAssembly("SoundTouch.Net");
                Type? genericSoundTouch = assembly?.GetType("SoundTouch.SoundTouch`2");
                Type? genericArrayPtr = assembly?.GetType("SoundTouch.Utility.ArrayPtr`1");
                if (genericSoundTouch == null || genericArrayPtr == null) return false;

                Type soundTouchType = genericSoundTouch.MakeGenericType(typeof(float), typeof(double));
                _arrayPtrType = genericArrayPtr.MakeGenericType(typeof(float));
                _soundTouch = Activator.CreateInstance(soundTouchType);
                _setPitchSemiTones = soundTouchType.GetMethod("SetPitchSemiTones", new[] { typeof(double) });
                _putSamples = soundTouchType.GetMethod("PutSamples", new[] { _arrayPtrType, typeof(int) });
                _receiveSamples = soundTouchType.GetMethod("ReceiveSamples", new[] { _arrayPtrType, typeof(int) });
                _arrayToPtr = _arrayPtrType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(float[]) }, null);

                if (_soundTouch == null || _setPitchSemiTones == null || _putSamples == null ||
                    _receiveSamples == null || _arrayToPtr == null)
                {
                    _soundTouch = null;
                    return false;
                }
                return true;
            }
            catch
            {
                _soundTouch = null;
                return false;
            }
        }

        private void SetSoundTouchPitch(float semitones)
        {
            _setPitchSemiTones?.Invoke(_soundTouch, new object[] { (double)semitones });
        }

        private void Invoke(string methodName, params object[] args)
        {
            Type[] parameterTypes = Array.ConvertAll(args, x => x.GetType());
            _soundTouch?.GetType().GetMethod(methodName, parameterTypes)?.Invoke(_soundTouch, args);
        }

        private static Assembly? TryLoadAssembly(string name)
        {
            try { return Assembly.Load(new AssemblyName(name)); }
            catch { return null; }
        }

        private void EnsureBuffers(int count)
        {
            if (_input.Length < count) _input = new float[count];
            int receiveSize = Math.Max(count * 2, _channels * 2048);
            if (_received.Length < receiveSize) _received = new float[receiveSize];
        }
    }
}
