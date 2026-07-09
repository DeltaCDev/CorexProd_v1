using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace CorexProd.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int NotificationPermissionRequestCode = 8101;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestNotificationPermission();
        StartNotificationPollingService();
    }

    private void RequestNotificationPermission()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return;

        if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted)
            return;

        RequestPermissions([Android.Manifest.Permission.PostNotifications], NotificationPermissionRequestCode);
    }

    private void StartNotificationPollingService()
    {
        Intent intent = new(this, typeof(NotificationPollingService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(intent);
        else
            StartService(intent);
    }
}
