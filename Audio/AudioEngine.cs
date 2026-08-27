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
        private WasapiRecorder? _capture;
        private WasapiPlayer? _output;
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
        private readonly SoftClipEffect _softClip = new();

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
            // Use event-driven WASAPI to reduce glitches
            _capture = new WasapiRecorderBuilder()
                .WithDevice(input)
                .WithSharedMode()
                .WithEventSync()
                .WithBufferLength(latencyMs)
                .Build();
            _bufferedInput = new BufferedWaveProvider(_capture.WaveFormat, TimeSpan.FromMilliseconds(200))
            {
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            _capture.DataAvailable += (data, _, _, _) =>
            {
                _bufferedInput.AddSamples(data);
            };

            ISampleProvider inputSamples = _bufferedInput.ToSampleProvider();

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
            _softClip.Amount = 0.6f; // moderate default soft clipping

            _effectChain.SetEffects(new IEffect[]
            {
                _gate,
                _gainIn,
                _pitch,
                _formant,
                _eq,
                _echo,
                _gainOut,
                _softClip,
                new TapEffect(_waveTap)
            });

            // Adapt to output device mix format (rate/channels) to avoid resample mismatches that cause crackling
            using var outputAudioClient = output.CreateAudioClient();
            var mix = outputAudioClient.MixFormat;
            if (_sampleChain.WaveFormat.SampleRate != mix.SampleRate)
            {
                _sampleChain = new WdlResamplingSampleProvider(_sampleChain, mix.SampleRate);
            }
            if (_sampleChain.WaveFormat.Channels != mix.Channels)
            {
                if (_sampleChain.WaveFormat.Channels == 1 && mix.Channels == 2)
                {
                    _sampleChain = new MonoToStereoSampleProvider(_sampleChain);
                }
                else if (_sampleChain.WaveFormat.Channels == 2 && mix.Channels == 1)
                {
                    var st2m = new StereoToMonoSampleProvider(_sampleChain) { LeftVolume = 0.5f, RightVolume = 0.5f };
                    _sampleChain = st2m;
                }
                // Other channel counts are uncommon for voice; if encountered, WASAPI may still accept downmixing via OS.
            }

            IWaveProvider outProvider = _sampleChain.ToWaveProvider();
            _output = new WasapiPlayerBuilder()
                .WithDevice(output)
                .WithSharedMode()
                .WithEventSync()
                .WithLatency(latencyMs)
                .Build();
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
