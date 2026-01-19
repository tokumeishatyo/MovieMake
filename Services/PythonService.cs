using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieMake.Services
{
    public class PythonService : IDisposable
    {
        private Process? _pythonProcess;
        private readonly HttpClient _httpClient;
        private string? _apiKey; // In-memory API key
        private int _port;

        public bool IsRunning => _pythonProcess != null && !_pythonProcess.HasExited;

        public PythonService()
        {
            _httpClient = new HttpClient();
        }

        public async Task StartBackendAsync()
        {
            if (IsRunning) return;

            // Find free port
            _port = GetFreeTcpPort();
            string baseUrl = $"http://127.0.0.1:{_port}";
            _httpClient.BaseAddress = new Uri(baseUrl);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string backendDir = Path.Combine(baseDir, "backend");
            string pythonScript = Path.Combine(backendDir, "main.py");
            
            if (!File.Exists(pythonScript))
            {
                throw new FileNotFoundException($"Backend script not found at: {pythonScript}");
            }

            var psi = new ProcessStartInfo
            {
                FileName = "python", 
                Arguments = $"-u \"{pythonScript}\"", 
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = backendDir
            };
            
            // Pass PORT to python
            psi.EnvironmentVariables["PORT"] = _port.ToString();

            try 
            {
                _pythonProcess = Process.Start(psi);
                if (_pythonProcess == null) throw new Exception("Failed to start python process.");
                
                // Monitor output
                _pythonProcess.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) Debug.WriteLine($"[Python] {e.Data}");
                };
                _pythonProcess.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) Debug.WriteLine($"[Python ERR] {e.Data}");
                };
                _pythonProcess.BeginOutputReadLine();
                _pythonProcess.BeginErrorReadLine();

                await WaitForHealthAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting python: {ex.Message}");
                StopBackend();
                throw;
            }
        }

        private int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private async Task WaitForHealthAsync(int maxRetries = 10)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (_pythonProcess != null && _pythonProcess.HasExited)
                {
                     throw new Exception($"Python process exited prematurely with code {_pythonProcess.ExitCode}. check debug output.");
                }

                try
                {
                    var response = await _httpClient.GetAsync("/health");
                    if (response.IsSuccessStatusCode) return;
                }
                catch
                {
                    // Ignore
                }
                await Task.Delay(500);
            }
            throw new Exception("Timed out waiting for backend to start.");
        }

        public async Task SetApiKeyAsync(string apiKey)
        {
            _apiKey = apiKey; 
            
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
