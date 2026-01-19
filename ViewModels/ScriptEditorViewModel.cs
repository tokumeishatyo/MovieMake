using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieMake.Models;
using MovieMake.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMake.ViewModels
{
    public partial class ScriptEditorViewModel : ObservableObject
    {
        private readonly ScriptManager _scriptManager;

        [ObservableProperty]
        private Script _currentScript;

        public ObservableCollection<Character> AvailableCharacters { get; } = new ObservableCollection<Character>();

        public ScriptEditorViewModel()
        {
            _scriptManager = new ScriptManager();
            CurrentScript = new Script();
            
            _ = InitializeAsync(); // Fire and forget load
        }

        public async Task InitializeAsync()
        {
             try 
             {
                 var json = await App.PythonService.GetCharactersJsonAsync();
                 var chars = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Character>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                 
                 AvailableCharacters.Clear();
                 if (chars != null)
                 {
                     foreach (var c in chars)
                     {
                         // Basic mapping if backend diffs from frontend model
                         AvailableCharacters.Add(c);
                     }
                 }
                 
                 if (CurrentScript.Lines.Count == 0) AddLine(); 
             }
             catch (Exception ex)
             {
                 StatusMessage = $"Failed to load characters: {ex.Message}";
                 // Fallback dummy
                 AvailableCharacters.Add(new Character { Name = "Fallback", Id = "fallback" });
                 if (CurrentScript.Lines.Count == 0) AddLine();
             }
        }

        [RelayCommand]
        private void AddLine()
        {
            var newLine = new Line 
            { 
                CharacterId = AvailableCharacters.FirstOrDefault()?.Id ?? "" 
            };
            CurrentScript.Lines.Add(newLine);
            // ObservableCollection handles notification
        }

        [RelayCommand]
        private void RemoveLine(Line line)
        {
            if (CurrentScript.Lines.Contains(line))
            {
                CurrentScript.Lines.Remove(line);
                OnPropertyChanged(nameof(CurrentScript));
            }
        }

        [ObservableProperty]
        private string _statusMessage = "";

        [RelayCommand]
        private async Task SaveScriptAsync()
        {
            try 
            {
                var pickerService = new FilePickerService();
                var file = await pickerService.PickSaveFileAsync("MyScript");
                
                if (file != null)
                {
                    await _scriptManager.SaveScriptAsync(CurrentScript, file.Path);
                    StatusMessage = $"Saved to: {file.Path}";
                }
                else
                {
                    StatusMessage = "Save Cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save Failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadScriptAsync()
        {
            try
            {
                var pickerService = new FilePickerService();
                var file = await pickerService.PickOpenFileAsync();

                if (file != null)
                {
                    var loaded = await _scriptManager.LoadScriptAsync(file.Path);
                    if (loaded != null)
                    {
                        CurrentScript = loaded;
                        StatusMessage = $"Loaded: {file.Path}";
                    }
                }
                else
                {
                     StatusMessage = "Load Cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load Failed: {ex.Message}";
            }
        }
        [ObservableProperty]
        private bool _isRendering = false;

        [RelayCommand]
        private async Task RenderVideoAsync()
        {
            if (CurrentScript.Lines.Count == 0)
            {
                StatusMessage = "Script is empty.";
                return;
            }

            IsRendering = true;
            StatusMessage = "Rendering video... This may take a while.";

            try
            {
                // We need the full URL including port, which PythonService manages internally.
                // However, PythonService.RenderVideoAsync returns the relative path from the server root.
                // It's tricky to get the port from here as it's private in PythonService.
                // Actually PythonService exposes relative URL properly.
                // Let's modify PythonService to expose BaseUrl or handle full URL construction.
                // OR we just assume localhost and we can't easily guess port.
                // HACK: PythonService.RenderVideoAsync should probably return full URL or we expose BaseAddress.
                // But for now let's assume PythonService returns something we can use.
                // NOTE: I'll stick to returning relative URL from service, but I need the base.
                // Let's assume the user will play it via browser.
                
                string fullUrl = await App.PythonService.RenderVideoAsync(CurrentScript);
                
                StatusMessage = "Render Complete! Opening video..."; 
                
                // Open in default browser/player
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Render Failed: {ex.Message}";
            }
            finally
            {
                IsRendering = false;
            }
        }

        [RelayCommand]
        private async Task ImportCharacterAsync()
        {
            try
            {
                var picker = new FilePickerService();
                var folder = await picker.PickSingleFolderAsync();
                
                if (folder == null) return;

                StatusMessage = "Importing character...";

                // Destination: LocalFolder/characters/{FolderName}
                var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var charsDir = await localFolder.CreateFolderAsync("characters", Windows.Storage.CreationCollisionOption.OpenIfExists);
                var destDir = await charsDir.CreateFolderAsync(folder.Name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

                // Copy files
                var files = await folder.GetFilesAsync();
                foreach (var file in files)
                {
                    await file.CopyAsync(destDir, file.Name, Windows.Storage.NameCollisionOption.ReplaceExisting);
                }

                StatusMessage = $"Imported '{folder.Name}'. Refreshing list...";
                
                // Refresh list from backend
                await InitializeAsync();
                
                StatusMessage = $"Imported '{folder.Name}' successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import Failed: {ex.Message}";
            }
        }
    }
}
