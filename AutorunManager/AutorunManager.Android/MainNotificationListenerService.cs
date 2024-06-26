using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.OS;
using Android.Service.Notification;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Essentials;

[Service(Label = "MainNotificationListenerService", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Enabled = true, Exported = true)]
[IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
public class MainNotificationListenerService : NotificationListenerService
{
    private static List<string> _runningApps = new List<string>();
    private static List<string> _runnungAppsInNotifications = new List<string>();
    private static DateTime _lastUpdateTime = DateTime.MinValue;

    public override void OnCreate()
    {
        base.OnCreate();
    }

    public override void OnNotificationPosted(StatusBarNotification sbn)
    {
        _runnungAppsInNotifications.Add(sbn.PackageName);

        if (_lastUpdateTime.AddHours(4) <  DateTime.Now)
        {
            _lastUpdateTime = DateTime.Now;
            _runningApps.Clear();
        }

        LaunchAppList();
    }

    public override void OnNotificationRemoved(StatusBarNotification sbn)
    {
        if(_runningApps.Contains(sbn.PackageName))
        {
            _runnungAppsInNotifications.Remove(sbn.PackageName);
        }
    }

    private List<string> LoadAppListFromFile()
    {
        var appList = new List<string>();
        string downloadPath = FileSystem.AppDataDirectory;
        string filePath = Path.Combine(downloadPath, "applist.txt");

        try
        {
            if (File.Exists(filePath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        appList.Add(line.Trim());
                    }
                }
            }
            else
            {
                File.Create(filePath);
                Toast.MakeText(this, "App list file not found in the default download directory.", ToastLength.Short).Show();
            }
        }
        catch (IOException ex)
        {
            Toast.MakeText(this, $"Error reading app list file: {ex.Message}", ToastLength.Short).Show();
        }

        return appList;
    }

    private void LaunchAppList()
    {
        var appList = LoadAppListFromFile(); // Load appList from file
        var lastOpenedApp = GetLatestApp()?.PackageName;

        foreach (var app in appList)
        {
            if(!_runningApps.Contains(app) && !_runnungAppsInNotifications.Contains(app))
            {
                LaunchTargetApp(app, lastOpenedApp);
            }
        }
    }

    private void LaunchTargetApp(string targetPackageName, string lastOpenedApp)
    {
        try
        {
            // Проверяем, запущено ли уже приложение с заданным пакетным именем
            if (IsAppRunningInBackground(targetPackageName))
            {
                // Если приложение уже запущено в фоне, ничего не делаем
                return;
            }

            // Иначе пытаемся запустить приложение
            if (LaunchApp(targetPackageName))
            {
                System.Threading.Tasks.Task.Delay(1000).Wait();
                if (!string.IsNullOrEmpty(lastOpenedApp))
                {
                    if (!LaunchApp(lastOpenedApp))
                    {
                        NavigateHome(Application.Context);
                    }
                }
                else
                {
                    NavigateHome(Application.Context);
                }
            }
        }
        catch { }
    }

    private bool LaunchApp(string targetPackageName)
    {
        Intent launchIntent = PackageManager.GetLaunchIntentForPackage(targetPackageName);
        if (launchIntent != null)
        {
            _runningApps.Add(targetPackageName);
            launchIntent.AddFlags(ActivityFlags.NewTask);
            StartActivity(launchIntent);
            return true;
        }
        
        return false;
    }

    // Метод для проверки, запущено ли приложение с заданным пакетным именем в фоне
    private bool IsAppRunningInBackground(string packageName)
    {
        if (_runningApps.Contains(packageName))
        {
            return true;
        }

        return false; // Приложение не запущено в какой-либо задаче
    }

    private List<UsageEvents.Event> GetPackageUsages()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
        {
            throw new NotSupportedException("This feature is supported on Android Lollipop and above.");
        }

        var usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
        var endTime = Java.Lang.JavaSystem.CurrentTimeMillis();
        var startTime = endTime - (5 * 60 * 1000); // last 5 minutes

        var usageEvents = usageStatsManager.QueryEvents(startTime, endTime);
        var events = new List<UsageEvents.Event>();

        while (usageEvents.HasNextEvent)
        {
            var usageEvent = new UsageEvents.Event();
            usageEvents.GetNextEvent(usageEvent);
            events.Add(usageEvent);
        }

        return events;
    }

    public List<UsageEvents.Event> GetLatestEventsForEachPackage(List<UsageEvents.Event> events, Func<UsageEvents.Event, bool> predicate)
    {
        // Group events by package name
        var groupedEvents = events.Where(predicate).GroupBy(e => e.PackageName);

        // Select the latest event for each package
        var latestEvents = groupedEvents
            .Select(group => group.OrderByDescending(e => e.TimeStamp).First())
            .ToList();

        return latestEvents;
    }

    private UsageEvents.Event GetLatestApp()
    {
        var usages = GetPackageUsages();
        var latestUsages = GetLatestEventsForEachPackage(usages, x => x.EventType == UsageEventType.ActivityResumed);

        latestUsages.Sort((a, b) =>
        {
            return (int)(a.TimeStamp - b.TimeStamp);
        });

        return latestUsages.LastOrDefault();
    }

    private void NavigateHome(Context context)
    {
        Intent intent = new Intent(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryHome);
        intent.SetFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);
    }
}