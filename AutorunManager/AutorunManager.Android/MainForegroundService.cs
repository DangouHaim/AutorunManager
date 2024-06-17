using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace AutorunManager.Droid
{
    
    [Service]
    public class MainForegroundService : Service
    {
        public const int ServiceRunningNotificationId = 10001;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var notification = new NotificationCompat.Builder(this, "boot_channel")
                .SetContentTitle("Persistent Notification")
                .SetContentText("This notification persists across reboots.")
                .SetSmallIcon(Resource.Drawable.navigation_empty_icon)
                .SetOngoing(true)
                .Build();

            StartForeground(ServiceRunningNotificationId, notification);

            return StartCommandResult.Sticky;
        }
    }
}