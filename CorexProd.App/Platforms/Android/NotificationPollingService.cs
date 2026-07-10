using System.Net.Http.Json;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.Storage;

namespace CorexProd.App;

[Service(
    Name = "com.corexprod.app.NotificationPollingService",
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class NotificationPollingService : Service
{
    private const string ServiceChannelId = "corexprod_sync";
    private const string EventsChannelId = "corexprod_events_v2";
    private const int ServiceNotificationId = 9401;
    private const string LastNotificationIdKey = "LastAppNotificationId";
    private const string NotificationsInitializedKey = "AppNotificationsInitialized";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private Timer? _timer;
    private int _isPolling;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannels();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForeground(ServiceNotificationId, BuildServiceNotification());
        _timer ??= new Timer(_ => _ = PollAsync(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(25));
        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        _timer?.Dispose();
        _httpClient.Dispose();
        base.OnDestroy();
    }

    private async Task PollAsync()
    {
        if (Interlocked.Exchange(ref _isPolling, 1) == 1)
            return;

        try
        {
            string baseUrl = NormalizeBaseUrl(Preferences.Get("ApiBaseUrl", string.Empty));
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            long lastId = Preferences.Get(LastNotificationIdKey, 0L);
            bool initialized = Preferences.Get(NotificationsInitializedKey, false);
            string url = $"{baseUrl}/api/notificaciones?desdeId={lastId}";
            using HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return;

            NotificationListResponse? result = await response.Content.ReadFromJsonAsync<NotificationListResponse>(JsonOptions);
            if (result?.Items == null || result.Items.Count == 0)
            {
                if (!initialized)
                    Preferences.Set(NotificationsInitializedKey, true);
                return;
            }

            long maxId = result.Items.Max(x => x.IdNotificacion);
            Preferences.Set(LastNotificationIdKey, maxId);

            if (!initialized)
            {
                Preferences.Set(NotificationsInitializedKey, true);
                return;
            }

            foreach (AppNotificationItem item in result.Items.OrderBy(x => x.IdNotificacion))
                ShowEventNotification(item);
        }
        catch
        {
            // The service retries on the next interval; connection drops are expected in the plant.
        }
        finally
        {
            Interlocked.Exchange(ref _isPolling, 0);
        }
    }

    private void ShowEventNotification(AppNotificationItem item)
    {
        Intent launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty) ?? new Intent(this, typeof(MainActivity));
        PendingIntent pendingIntent = PendingIntent.GetActivity(
            this,
            (int)(item.IdNotificacion % int.MaxValue),
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        Notification notification = new NotificationCompat.Builder(this, EventsChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(string.IsNullOrWhiteSpace(item.Titulo) ? "CorexProd" : item.Titulo)
            .SetContentText(item.Mensaje)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(item.Mensaje))
            .SetContentIntent(pendingIntent)
            .SetAutoCancel(true)
            .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate))
            .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
            .SetPriority((int)NotificationPriority.High)
            .SetCategory(NotificationCompat.CategoryStatus)
            .Build();

        NotificationManagerCompat.From(this).Notify((int)(item.IdNotificacion % int.MaxValue), notification);
    }

    private Notification BuildServiceNotification()
    {
        Intent launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty) ?? new Intent(this, typeof(MainActivity));
        PendingIntent pendingIntent = PendingIntent.GetActivity(
            this,
            ServiceNotificationId,
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ServiceChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle("CorexProd activo")
            .SetContentText("Escuchando novedades de produccion")
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .SetPriority((int)NotificationPriority.Low)
            .SetCategory(NotificationCompat.CategoryService)
            .Build();
    }

    private void CreateNotificationChannels()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        NotificationManager? manager = GetSystemService(NotificationService) as NotificationManager;
        if (manager == null)
            return;

        NotificationChannel serviceChannel = new(
            ServiceChannelId,
            "CorexProd segundo plano",
            NotificationImportance.Low)
        {
            Description = "Mantiene la consulta de notificaciones activa"
        };

        NotificationChannel eventsChannel = new(
            EventsChannelId,
            "CorexProd produccion",
            NotificationImportance.High)
        {
            Description = "OT nuevas y movimientos de produccion"
        };
        eventsChannel.SetSound(
            RingtoneManager.GetDefaultUri(RingtoneType.Notification),
            new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Notification)
                .SetContentType(AudioContentType.Sonification)
                .Build());

        manager.CreateNotificationChannel(serviceChannel);
        manager.CreateNotificationChannel(eventsChannel);
    }

    private static string NormalizeBaseUrl(string? value)
    {
        string url = (value ?? string.Empty).Trim().TrimEnd('/');
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "http://" + url["https://".Length..];
        return url;
    }

    private sealed record NotificationListResponse(int Total, List<AppNotificationItem> Items);

    private sealed record AppNotificationItem(
        long IdNotificacion,
        string Tipo,
        string Titulo,
        string Mensaje,
        int? IdOrdenTrabajo,
        string NumeroOT,
        DateTime FechaRegistro);
}
