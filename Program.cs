using Avalonia;
using Avalonia.ReactiveUI;
using System;
using LLibrary;
using MsBox.Avalonia;
using System.Threading.Tasks;

namespace photocon;

static class Program
{
    public static L Logger { get; } = new(directory: "logs");
    public static L TerminalLogger { get; } = new(directory: "terminal_logs");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) 
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogFatalExceptionWithWindow(ex);            
        }
    } 

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    public static void LogFatalExceptionWithWindow(Exception ex)
    {
        Logger.Fatal(ex);
        ShowErrorWindow(ex).Wait();
    }

    public static async Task LogExceptionWithWindow(Exception ex, string? message = null)
    {
        LogException(ex, message);
        await ShowErrorWindow(ex);
    }

    public static async Task ShowErrorWindow(Exception ex, bool fatal = false)
    {
        await MessageBoxManager.GetMessageBoxStandard($"Photocon: {(fatal ? "Fatal " : "")}Error", $"{(fatal ? "Fatal e" : "E")}rror ocurred: {ex.Message}")
                .ShowWindowAsync();
    }

    public static void LogInfoWithMessage(string s)
    {
        Logger.Info(s);
        MessageBoxManager.GetMessageBoxStandard("Photocon: Message", s);
    }
    public static void LogInfo(string s)
    {
        Logger.Info(s);
    }
    public static void LogExceptionWithMessage(Exception? ex, string? msg)
    {
        if (ex != null)
        {
            msg = $"{msg ?? "Exception occurred"} :{Environment.NewLine}{ex}";
        }
        Logger.Error(msg);
        MessageBoxManager.GetMessageBoxStandard("Photocon: Error", msg ?? "N/A");
    }
    public static void LogException(Exception? ex, string? msg)
    {
        msg = $"{msg ?? "Exception occurred"} :{Environment.NewLine}{ex}";
        Logger.Error(msg);
    }
    public static void LogTerminal(string s)
    {
        TerminalLogger.Info(s);
    }
}
