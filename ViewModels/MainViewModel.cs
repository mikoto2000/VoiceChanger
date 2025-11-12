using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using VoiceChanger.Audio;
using VoiceChanger.Models;
using VoiceChanger.Services;
using VoiceChanger.Utils;

namespace VoiceChanger.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly AudioEngine _engine = new();
        public ObservableCollection<MMDevice> InputDevices { get; } = new();
        public ObservableCollection<MMDevice> OutputDevices { get; } = new();

        private MMDevice? _selectedInput;
        public MMDevice? SelectedInput { get => _selectedInput; set => SetProperty(ref _selectedInput, value); }

        private MMDevice? _selectedOutput;
        public MMDevice? SelectedOutput { get => _selectedOutput; set => SetProperty(ref _selectedOutput, value); }

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

        public float InputGain { get => _inputGain; set { SetProperty(ref _inputGain, value); Apply(); } }
        private float _inputGain = 1.0f;
        public float PitchSemitones { get => _pitch; set { SetProperty(ref _pitch, value); Apply(); } }
        private float _pitch = 0f;
        public float FormantShift { get => _formant; set { SetProperty(ref _formant, value); Apply(); } }
        private float _formant = 0f;
        public float EqLowDb { get => _eqLow; set { SetProperty(ref _eqLow, value); Apply(); } }
        private float _eqLow = 0f;
        public float EqMidDb { get => _eqMid; set { SetProperty(ref _eqMid, value); Apply(); } }
        private float _eqMid = 0f;
        public float EqHighDb { get => _eqHigh; set { SetProperty(ref _eqHigh, value); Apply(); } }
        private float _eqHigh = 0f;
        public int EchoMs { get => _echoMs; set { SetProperty(ref _echoMs, value); Apply(); } }
        private int _echoMs = 150;
        public float EchoFeedback { get => _echoFb; set { SetProperty(ref _echoFb, value); Apply(); } }
        private float _echoFb = 0.25f;
        public float EchoMix { get => _echoMix; set { SetProperty(ref _echoMix, value); Apply(); } }
        private float _echoMix = 0.0f; // default off to avoid metallic coloration
        public float GateThreshold { get => _gateTh; set { SetProperty(ref _gateTh, value); Apply(); } }
        private float _gateTh = 0.003f;
        public float GateRelease { get => _gateRel; set { SetProperty(ref _gateRel, value); Apply(); } }
        private float _gateRel = 0.1f;
        public float OutputGain { get => _outGain; set { SetProperty(ref _outGain, value); Apply(); } }
        private float _outGain = 1.0f;
        public float SibilantMix { get => _sibMix; set { SetProperty(ref _sibMix, value); Apply(); } }
        private float _sibMix = 0.15f;
        public float TransientMix { get => _trMix; set { SetProperty(ref _trMix, value); Apply(); } }
        private float _trMix = 0.25f;
        public float TransientSensitivity { get => _trSens; set { SetProperty(ref _trSens, value); Apply(); } }
        private float _trSens = 0.2f;

        public ICommand RefreshDevicesCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public MainViewModel()
        {
            RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices());
            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning && SelectedInput != null && SelectedOutput != null);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            SavePresetCommand = new RelayCommand(async _ =>
            {
                var preset = GetPreset();
                var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "voicechanger_preset.json");
                await PresetService.SaveAsync(path, preset);
            });
            LoadPresetCommand = new RelayCommand(async _ =>
            {
                var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "voicechanger_preset.json");
                var preset = await PresetService.LoadAsync(path);
                if (preset != null) ApplyPreset(preset);
            });

            _engine.Started += () => IsRunning = true;
            _engine.Stopped += () => IsRunning = false;

            RefreshDevices();
        }

        public void RefreshDevices()
        {
            InputDevices.Clear();
            OutputDevices.Clear();
            foreach (var d in _engine.GetInputDevices()) InputDevices.Add(d);
            foreach (var d in _engine.GetOutputDevices()) OutputDevices.Add(d);
            if (SelectedInput == null) SelectedInput = InputDevices.FirstOrDefault();
            if (SelectedOutput == null) SelectedOutput = OutputDevices.FirstOrDefault();
        }

        public void Start()
        {
            if (SelectedInput == null || SelectedOutput == null) return;
            _engine.Start(SelectedInput, SelectedOutput, 60,
                InputGain, PitchSemitones, FormantShift,
                EqLowDb, EqMidDb, EqHighDb,
                EchoMs, EchoFeedback, EchoMix,
                GateThreshold, GateRelease, OutputGain,
                SibilantMix, TransientMix, TransientSensitivity);
        }

        public void Stop() => _engine.Stop();

        private void Apply()
        {
            _engine.UpdateParameters(InputGain, PitchSemitones, FormantShift,
                EqLowDb, EqMidDb, EqHighDb,
                EchoMs, EchoFeedback, EchoMix,
                GateThreshold, GateRelease, OutputGain);
            // Note: sibilant/transient parameters are applied on Start; if needed live updates can be added to engine.
        }

        public bool TryGetWaveform(int max, out float[] samples) => _engine.TryDequeueWaveform(max, out samples);

        private Preset GetPreset() => new Preset
        {
            InputGain = InputGain,
            PitchSemitones = PitchSemitones,
            FormantShift = FormantShift,
            EqLowDb = EqLowDb,
            EqMidDb = EqMidDb,
            EqHighDb = EqHighDb,
            EchoMs = EchoMs,
            EchoFeedback = EchoFeedback,
            EchoMix = EchoMix,
            GateThreshold = GateThreshold,
            GateRelease = GateRelease,
            OutputGain = OutputGain
        };

        private void ApplyPreset(Preset p)
        {
            InputGain = p.InputGain;
            PitchSemitones = p.PitchSemitones;
            FormantShift = p.FormantShift;
            EqLowDb = p.EqLowDb;
            EqMidDb = p.EqMidDb;
            EqHighDb = p.EqHighDb;
            EchoMs = p.EchoMs;
            EchoFeedback = p.EchoFeedback;
            EchoMix = p.EchoMix;
            GateThreshold = p.GateThreshold;
            GateRelease = p.GateRelease;
            OutputGain = p.OutputGain;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
