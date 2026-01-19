using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MovieMake.Services
{
    public class FilePickerService
    {
        public async Task<StorageFile?> PickSaveFileAsync(string suggestedFileName)
        {
            var window = App.MainWindow;
            if (window == null) return null;

            var picker = new FileSavePicker();
            
            // Get window handle
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON File", new List<string>() { ".json" });
            picker.SuggestedFileName = suggestedFileName;

            return await picker.PickSaveFileAsync();
        }

        public async Task<StorageFile?> PickOpenFileAsync()
        {
            var window = App.MainWindow;
            if (window == null) return null;

            var picker = new FileOpenPicker();
            
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            return await picker.PickSingleFileAsync();
        }

        public async Task<StorageFolder?> PickSingleFolderAsync()
        {
            var window = App.MainWindow;
            if (window == null) return null;

            var picker = new FolderPicker();
            
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            return await picker.PickSingleFolderAsync();
        }
    }
}
