using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using photocon.ViewModels;
using photocon.Views;
using System.IO;
using System;
using Avalonia.Styling;

namespace photocon;

public partial class App : Application
{
    public static bool IsDarkThemed => (string?)(Current?.ActualThemeVariant.Key) == (string)ThemeVariant.Dark.Key;
    
    public Settings Configuration { get; private set; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (File.Exists(SettingsFileName))
        {
            try
            {
                Configuration = Serializer.Deserialize<Settings>(File.ReadAllText(SettingsFileName));   
            }
            catch (Exception ex)
            {
                Program.LogExceptionWithMessage(ex, "Failed to load configuration file, using defaults");
            }
        }
        Program.LogInfo("Starting up.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(Configuration)
            };
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void SaveSettings()
    {
        try
        {
            File.WriteAllText(SettingsFileName, Serializer.Serialize(Configuration));   
        }
        catch (Exception ex)
        {
            Program.LogExceptionWithMessage(ex, "Failed to save configuration file");
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        SaveSettings();
        Program.LogInfo("Exiting.");
    }

    private const string SettingsFileName = "settings.yaml";
}