using Android.App;
using Android.Content;
using Android.Service.Notification;
using Android.Widget;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;

[Service(Label = "MainNotificationListenerService", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Enabled = true, Exported = true)]
[IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
public class MainNotificationListenerService : NotificationListenerService
{
    private static List<string> _runningApps = new List<string>();

    public override void OnCreate()
    {
        base.OnCreate();
    }

    public override void OnNotificationPosted(StatusBarNotification sbn)
    {
        LaunchAppList();
    }

    public override void OnNotificationRemoved(StatusBarNotification sbn)
    {

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

        foreach (var app in appList)
        {
            if(!_runningApps.Contains(app))
            {
                LaunchTargetApp(app);
                System.Threading.Tasks.Task.Delay(2500).Wait();
            }
        }
    }

    private void LaunchTargetApp(string targetPackageName)
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
            Intent launchIntent = PackageManager.GetLaunchIntentForPackage(targetPackageName);
            if (launchIntent != null)
            {
                _runningApps.Add(targetPackageName);
                launchIntent.AddFlags(ActivityFlags.NewTask);
                StartActivity(launchIntent);
            }
        }
        catch { }
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
}