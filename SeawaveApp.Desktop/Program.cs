using System;
using System.Runtime.InteropServices;
using Avalonia;
using LibVLCSharp.Shared;

namespace SeawaveApp.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var currentFolder = AppDomain.CurrentDomain.BaseDirectory;
            Core.Initialize(currentFolder);
        }
        else
        {
            Core.Initialize();
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}