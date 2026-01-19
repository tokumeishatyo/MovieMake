using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MovieMake.Models;

namespace MovieMake.Services
{
    public class ScriptManager
    {
        public async Task SaveScriptAsync(Script script, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, script, options);
        }

        public async Task<Script?> LoadScriptAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<Script>(stream);
        }
    }
}
