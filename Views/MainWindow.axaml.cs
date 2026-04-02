using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using photocon.ViewModels;

namespace photocon.Views;

public partial class MainWindow : Window
{
    private enum LastUsedFolderType
    {
        LoadParams,
        SaveParams,
        SaveSpectrum
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindow_DataContextChanged;
        MainWindow_DataContextChanged(this, new EventArgs());
    }

    private MainWindowViewModel? ViewModel { get; set; }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        ViewModel = (MainWindowViewModel?)DataContext;
    }
    private async Task<IStorageFolder?> TryGetLastUsedFolder(LastUsedFolderType t)
    {
        if (ViewModel == null) return null;

        string? initialPath = null;
        initialPath = t switch
        {
            LastUsedFolderType.LoadParams => ViewModel.Configuration.LoadLocation,
            LastUsedFolderType.SaveParams => ViewModel.Configuration.SaveLocation,
            LastUsedFolderType.SaveSpectrum => ViewModel.Configuration.SpectrumSaveLocation,
            _ => throw new ArgumentException(),
        };
        IStorageFolder? initialFolder;
        if (initialPath != null) initialFolder = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
        else initialFolder = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);

        return initialFolder;
    }
    private void SetLastUsedFolder(LastUsedFolderType t, IStorageFile fileInFolder)
    {
        if (ViewModel == null) return;

        string? path = Path.GetDirectoryName(Uri.UnescapeDataString(fileInFolder.Path.AbsolutePath));
        switch (t)
        {
            case LastUsedFolderType.LoadParams:
                ViewModel.Configuration.LoadLocation = path;
                break;
            case LastUsedFolderType.SaveParams:
                ViewModel.Configuration.SaveLocation = path;
                break;
            case LastUsedFolderType.SaveSpectrum:
                ViewModel.Configuration.SpectrumSaveLocation = path;
                break;
            default: throw new ArgumentException();
        }
    }

    public async void LoadMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) throw new NullReferenceException();

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
            AllowMultiple = false,
            SuggestedStartLocation = await TryGetLastUsedFolder(LastUsedFolderType.LoadParams),
            FileTypeFilter = FileTypeFilter,
            Title = "Select file..."
        });
        if ((files?.Count ?? 0) == 0) return;
        var file = files[0];
        SetLastUsedFolder(LastUsedFolderType.LoadParams, file);

        string yaml = await File.ReadAllTextAsync(Uri.UnescapeDataString(file.Path.AbsolutePath));
        try
        {
            Models.ScanParams scanParams = Serializer.Deserialize<Models.ScanParams>(yaml);
            ViewModel.SetScanParams(scanParams);
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Photocon", $"Unable to load scan parameters. Error: {ex}").ShowAsPopupAsync(this);
            Program.LogException(ex, "Failed to load scan parameters from file");
        }
    }
    public async void SaveMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) throw new NullReferenceException();

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions() {
            DefaultExtension = "yaml",
            SuggestedStartLocation = await TryGetLastUsedFolder(LastUsedFolderType.SaveParams),
            Title = "Select save location..."
        });
        if (file == null) return;
        SetLastUsedFolder(LastUsedFolderType.SaveParams, file);

        try
        {
            string yaml = Serializer.Serialize(ViewModel.ScanParamsContext);
            await File.WriteAllTextAsync(Uri.UnescapeDataString(file.Path.AbsolutePath), yaml);
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Photocon", $"Unable to save scan parameters. Error: {ex}").ShowAsPopupAsync(this);
            Program.LogException(ex, "Failed to save scan parameters to file");
        }
    }
    public async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var aboutBox = new AboutBox() { DataContext = new AboutBoxViewModel() };
        await aboutBox.ShowDialog(this);
    }
    public async void AdvanceStateMachine_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.AdvanceStateMachine();
    }
    public async void SaveSpectrum_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) throw new NullReferenceException();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions() {
            DefaultExtension = "csv",
            SuggestedStartLocation = await TryGetLastUsedFolder(LastUsedFolderType.SaveSpectrum),
            Title = "Select save location..."
        });
        if (file == null) return;
        SetLastUsedFolder(LastUsedFolderType.SaveSpectrum, file);
        await ViewModel.SaveSpectrum(Uri.UnescapeDataString(file.Path.AbsolutePath));
    }

    private static readonly FilePickerFileType[] FileTypeFilter = new FilePickerFileType[] { 
        new("yaml") { Patterns = new string[] { "*.yaml" } } 
    };
}