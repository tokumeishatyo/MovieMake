using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieMake.Services
{
    public class PythonService : IDisposable
    {
        private Process? _pythonProcess;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://127.0.0.1:8000";
        private string? _apiKey; // In-memory API key
        public bool IsRunning => _pythonProcess != null && !_pythonProcess.HasExited;

        public PythonService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task StartBackendAsync()
        {
            if (IsRunning) return;

            string pythonScript = Path.GetFullPath("backend/main.py");
            
            var psi = new ProcessStartInfo
            {
                FileName = "python", // Standard on Windows
                Arguments = $"-u \"{pythonScript}\"", // -u for unbuffered stdout
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetFullPath(".") 
            };

            try 
            {
                _pythonProcess = Process.Start(psi);
                if (_pythonProcess == null) throw new Exception("Failed to start python process.");
                
                // Monitor output for debugging
                _pythonProcess.OutputDataReceived += (s, e) => Debug.WriteLine($"[Python] {e.Data}");
                _pythonProcess.ErrorDataReceived += (s, e) => Debug.WriteLine($"[Python ERR] {e.Data}");
                _pythonProcess.BeginOutputReadLine();
                _pythonProcess.BeginErrorReadLine();

                await WaitForHealthAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting python: {ex.Message}");
                throw;
            }
        }

        private async Task WaitForHealthAsync(int maxRetries = 10)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync("/health");
                    if (response.IsSuccessStatusCode) return;
                }
                catch
                {
                    // Ignore connection errors while starting
                }
                await Task.Delay(500);
            }
            throw new Exception("Timed out waiting for backend to start.");
        }

        public async Task SetApiKeyAsync(string apiKey)
        {
            _apiKey = apiKey; // Keep in memory
            
            var payload = new { api_key = apiKey };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/config/api-key", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> CheckHealthAsync()
        {
            try 
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch 
            {
                return false;
            }
        }

        public void StopBackend()
        {
            try
            {
                if (_pythonProcess != null && !_pythonProcess.HasExited)
                {
                    _pythonProcess.Kill();
                    _pythonProcess.WaitForExit();
                }
                _pythonProcess = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping backend: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopBackend();
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
