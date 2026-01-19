using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieMake.Services;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace MovieMake.ViewModels
{
    public partial class SetupViewModel : ObservableObject
    {
        private readonly PythonService _pythonService;
        
        // Event to request navigation
        public event EventHandler? ConnectionSuccessful;

        [ObservableProperty]
        private string _statusMessage = "Not Connected";

        [ObservableProperty]
        private string _apiKey = "";

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private bool _isBusy = false;

        public SetupViewModel()
        {
            _pythonService = App.PythonService;
            InitializeBackend();
        }

        private async void InitializeBackend()
        {
            IsBusy = true;
            StatusMessage = "Starting Python Backend...";
            try
            {
                await _pythonService.StartBackendAsync();
                StatusMessage = "Backend Running. Waiting for API Key...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting backend: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                StatusMessage = "Please enter an API Key.";
                return;
            }

            IsBusy = true;
            try
            {
                await _pythonService.SetApiKeyAsync(ApiKey);
                
                var isHealthy = await _pythonService.CheckHealthAsync();
                if (isHealthy)
                {
                    StatusMessage = "Connected & Authenticated!";
                    IsConnected = true;
                    ApiKey = ""; 
                    
                    // Trigger navigation
                    ConnectionSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = "Connected but Health Check Failed.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection Failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
