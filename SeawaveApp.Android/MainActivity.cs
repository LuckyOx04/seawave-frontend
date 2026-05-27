using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;

namespace SeawaveApp.Android;

[Activity(
    Label = "SeawaveApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (Avalonia.Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseAndroid()
                .WithInterFont();
        }
        
        base.OnCreate(savedInstanceState);
    }
}