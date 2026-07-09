using Android.App;
using Android.Content;
using Android.OS;

namespace CorexProd.App;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
public sealed class NotificationStartupReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
            return;

        string? action = intent?.Action;
        if (action != Intent.ActionBootCompleted && action != Intent.ActionMyPackageReplaced)
            return;

        Intent serviceIntent = new(context, typeof(NotificationPollingService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(serviceIntent);
        else
            context.StartService(serviceIntent);
    }
}
