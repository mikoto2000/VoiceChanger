VoiceChanger (.NET 8 / WPF)

- Real-time voice changer for Windows using NAudio.
- Effects: pitch shift, formant shaping, 3‑band EQ, echo, noise gate, gain.
- WASAPI input/output with device selection and waveform monitor.

Build
- Requirements: .NET 8 SDK, Windows 10/11.
- Open `VoiceChanger.sln` in Visual Studio or run: `dotnet build`.
- NuGet: restores `NAudio` automatically.

Run
1) Select microphone input and output device (e.g., VB-Cable).
2) Click Start to begin processing.
3) Adjust sliders (pitch, formant, EQ, echo, gate, gains).
4) Save/Load presets (saved to Documents/voicechanger_preset.json).

Notes
- For virtual microphone, install VB-Audio Cable or VoiceMeeter and select as output.
- Latency is configured ~60 ms. Lower values may crackle depending on hardware.
- Pitch shifting uses NAudio’s SMB algorithm; formant is an approximated vocal filter.

