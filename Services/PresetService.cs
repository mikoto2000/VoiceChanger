using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceChanger.Models;

namespace VoiceChanger.Services
{
    public static class PresetService
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static async Task SaveAsync(string path, Preset preset)
        {
            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, preset, Options);
        }

        public static async Task<Preset?> LoadAsync(string path)
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Preset>(fs, Options);
        }
    }
}

