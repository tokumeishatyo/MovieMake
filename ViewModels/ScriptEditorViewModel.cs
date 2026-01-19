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
    }
}
