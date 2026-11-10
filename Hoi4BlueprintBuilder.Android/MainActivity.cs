using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Avalonia.Android;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace Hoi4BlueprintBuilder.Androi;

[Activity(
    Label = "paradox.art.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        RequestStoragePermission();
        base.OnCreate(savedInstanceState);
    }

    private void RequestStoragePermission()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            if (!Environment.IsExternalStorageManager)
            {
                var uri = Uri.FromParts("package", PackageName, null);
                var intent = new Intent();
                intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
                intent.SetData(uri);
                StartActivity(intent);
            }
        }
    }
}
