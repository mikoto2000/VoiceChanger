using System;
using System.Collections.Concurrent;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceChanger.DSP;
using VoiceChanger.DSP.Effects;

namespace VoiceChanger.Audio
{
    public class AudioEngine : IDisposable
    {
        private WasapiCapture? _capture;
        private WasapiOut? _output;
        private BufferedWaveProvider? _bufferedInput;
        private ISampleProvider? _sampleChain;
        private EffectChain? _effectChain;
        private readonly GainEffect _gainIn = new();
        private readonly SoundTouchPitchEffect _pitch = new();
        private readonly FormantShiftEffect _formant = new();
        private readonly Eq3BandEffect _eq = new();
        private readonly EchoEffect _echo = new();
        private readonly NoiseGateEffect _gate = new();
        private readonly GainEffect _gainOut = new();

        private readonly ConcurrentQueue<float> _waveTap = new();

        public event Action? Started;
        public event Action? Stopped;

        public MMDevice[] GetInputDevices()
        {
            using var en = new MMDeviceEnumerator();
            return en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
        }

        public MMDevice[] GetOutputDevices()
        {
            using var en = new MMDeviceEnumerator();
            return en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArray();
        }

        public void Start(MMDevice input, MMDevice output, int latencyMs,
                          float gainIn, float semitones, float formant, float eqLow, float eqMid, float eqHigh,
                          int echoMs, float echoFb, float echoMix, float gateThresh, float gateRelease, float gainOut)
        {
            Stop();
            _capture = new WasapiCapture(input, false, latencyMs);
            _bufferedInput = new BufferedWaveProvider(_capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true
            };
            _capture.DataAvailable += (s, e) =>
            {
                _bufferedInput.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            ISampleProvider inputSamples = _capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat
                ? new WaveBufferToSampleProvider(_bufferedInput)
                : new WaveToSampleProvider(_bufferedInput);

            _effectChain = new EffectChain(inputSamples);
            _sampleChain = _effectChain;

            _gainIn.Gain = gainIn;
            _pitch.Configure(_capture.WaveFormat.SampleRate, _capture.WaveFormat.Channels);
            _pitch.SetSemitones(semitones);
            _formant.Configure(_capture.WaveFormat.SampleRate);
            _formant.SetShift(formant);
            _eq.Update(_capture.WaveFormat.SampleRate, eqLow, eqMid, eqHigh);
            _echo.Configure(_capture.WaveFormat.SampleRate, _capture.WaveFormat.Channels, echoMs, echoFb, echoMix);
            _gate.Configure(gateThresh, gateRelease);
            _gainOut.Gain = gainOut;

            _effectChain.SetEffects(new IEffect[]
            {
                _gate,
                _gainIn,
                _pitch,
                _formant,
                _eq,
                _echo,
                _gainOut,
                new TapEffect(_waveTap)
            });

            IWaveProvider outProvider = _sampleChain.ToWaveProvider();
            _output = new WasapiOut(output, AudioClientShareMode.Shared, false, latencyMs);
            _output.Init(outProvider);
            _capture.StartRecording();
            _output.Play();
            Started?.Invoke();
        }

        public void UpdateParameters(float? gainIn = null, float? semitones = null, float? formant = null,
                                      float? eqLow = null, float? eqMid = null, float? eqHigh = null,
                                      int? echoMs = null, float? echoFb = null, float? echoMix = null,
                                      float? gateThresh = null, float? gateRelease = null, float? gainOut = null)
        {
            if (_capture == null) return;
            if (gainIn.HasValue) _gainIn.Gain = gainIn.Value;
            if (semitones.HasValue) _pitch.SetSemitones(semitones.Value);
            if (formant.HasValue) _formant.SetShift(formant.Value);
            if (eqLow.HasValue || eqMid.HasValue || eqHigh.HasValue)
            {
                _eq.Update(_capture.WaveFormat.SampleRate,
                    eqLow ?? 0f,
                    eqMid ?? 0f,
                    eqHigh ?? 0f);
            }
            if (echoMs.HasValue || echoFb.HasValue || echoMix.HasValue)
            {
                _echo.Configure(_capture.WaveFormat.SampleRate, _capture.WaveFormat.Channels,
                    echoMs ?? 200, echoFb ?? 0.3f, echoMix ?? 0.2f);
            }
            if (gateThresh.HasValue || gateRelease.HasValue)
            {
                _gate.Configure(gateThresh ?? 0.01f, gateRelease ?? 0.05f);
            }
            if (gainOut.HasValue) _gainOut.Gain = gainOut.Value;
        }

        public void Stop()
        {
            try { _output?.Stop(); } catch { }
            try { _capture?.StopRecording(); } catch { }
            _output?.Dispose();
            _capture?.Dispose();
            _output = null;
            _capture = null;
            _bufferedInput = null;
            _effectChain = null;
            _sampleChain = null;
            Stopped?.Invoke();
        }

        public bool TryDequeueWaveform(int maxSamples, out float[] samples)
        {
            int n = Math.Min(maxSamples, _waveTap.Count);
            samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                _waveTap.TryDequeue(out samples[i]);
            }
            return n > 0;
        }

        public void Dispose()
        {
            Stop();
        }

        private sealed class WaveBufferToSampleProvider : ISampleProvider
        {
            private readonly BufferedWaveProvider _provider;
            public WaveFormat WaveFormat { get; }

            public WaveBufferToSampleProvider(BufferedWaveProvider provider)
            {
                _provider = provider;
                WaveFormat = provider.WaveFormat;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                var bytesNeeded = count * sizeof(float);
                var temp = new byte[bytesNeeded];
                int bytes = _provider.Read(temp, 0, bytesNeeded);
                int samples = bytes / sizeof(float);
                var wb = new WaveBuffer(temp);
                for (int i = 0; i < samples; i++) buffer[offset + i] = wb.FloatBuffer[i];
                return samples;
            }
        }

        private sealed class TapEffect : IEffect
        {
            private readonly ConcurrentQueue<float> _tap;
            public TapEffect(ConcurrentQueue<float> tap) { _tap = tap; }
            public void Process(float[] buffer, int offset, int count, int channels)
            {
                int end = offset + Math.Min(count, 2048);
                for (int i = offset; i < end; i += channels)
                {
                    _tap.Enqueue(buffer[i]);
                    while (_tap.Count > 4096) _tap.TryDequeue(out _);
                }
            }
        }
    }
}
