using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android.Text;
using Android.Widget;
using Android.Provider;
using Android;
using Android.App.Usage;

[assembly: UsesPermission(Android.Manifest.Permission.ManageExternalStorage)]
[assembly: UsesPermission(Android.Manifest.Permission.ReadExternalStorage)]
[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission(Android.Manifest.Permission.SystemAlertWindow)]
[assembly: UsesPermission(Android.Manifest.Permission.BindNotificationListenerService)]
[assembly: UsesPermission(Android.Manifest.Permission.RequestIgnoreBatteryOptimizations)]
[assembly: UsesPermission(Android.Manifest.Permission.QueryAllPackages)]
[assembly: UsesPermission(Android.Manifest.Permission.PackageUsageStats)]

namespace AutorunManager.Droid
{
    [Activity(Label = "AutorunManager", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private const int OverlayPermissionRequestCode = 123;
        private const int ReadStoragePermissionRequestCode = 124;
        private const int BatteryOptimizationRequestCode = 125;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            // Request notification listener permission
            RequestNotificationListenerPermission();

            // Request draw over other apps permission
            RequestDrawOverAppsPermission();

            // Request file system permission
            RequestFileSystemPermission();

            // Request battery optimization permission
            RequestBatteryOptimizationExemption();

            // Request package usage stats permission
            RequestUsageStatsPermission();
        }

        public void RequestUsageStatsPermission()
        {
            if (IsUsageStatsPermissionGranted())
            {
                var intent = new Intent(Settings.ActionUsageAccessSettings);
                StartActivity(intent);
            }
        }

        private bool IsUsageStatsPermissionGranted()
        {
            UsageStatsManager usageStatsManager = (UsageStatsManager)GetSystemService(Android.Content.Context.UsageStatsService);
            long endTime = System.DateTime.Now.Millisecond;
            long startTime = endTime - 1000 * 60;
            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            return (stats != null && stats.Count > 0);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private bool IsNotificationServiceEnabled()
        {
            string packageName = PackageName;
            string flat = Android.Provider.Settings.Secure.GetString(ContentResolver, "enabled_notification_listeners");
            if (!string.IsNullOrEmpty(flat))
            {
                string[] names = flat.Split(':');
                foreach (string name in names)
                {
                    ComponentName componentName = ComponentName.UnflattenFromString(name);
                    if (componentName != null && TextUtils.Equals(packageName, componentName.PackageName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Method to request file system permission
        private void RequestFileSystemPermission()
        {
            if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) != Permission.Granted)
            {
                RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage }, ReadStoragePermissionRequestCode);
            }
        }

        // Method to request battery optimization exemption
        private void RequestBatteryOptimizationExemption()
        {
            PowerManager powerManager = (PowerManager)GetSystemService(PowerService);
            if (powerManager != null && !powerManager.IsIgnoringBatteryOptimizations(PackageName))
            {
                Intent intent = new Intent();
                intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(Android.Net.Uri.Parse("package:" + PackageName));
                StartActivityForResult(intent, BatteryOptimizationRequestCode);
            }
        }

        private void RequestNotificationListenerPermission()
        {
            // Check if the notification listener permission is granted
            if (!IsNotificationServiceEnabled())
            {
                // Prompt the user to enable the permission
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Permission Required")
                       .SetMessage("Please enable notification listener access for this app.")
                       .SetPositiveButton("Go to Settings", (sender, args) =>
                       {
                           // Navigate to the notification listener settings
                           Intent intent = new Intent(Android.Provider.Settings.ActionNotificationListenerSettings);
                           StartActivity(intent);
                       })
                       .SetNegativeButton("Cancel", (sender, args) =>
                       {
                           // Handle cancel action
                           Toast.MakeText(this, "Permission not granted. The app will not function properly.", ToastLength.Short).Show();
                       })
                       .Show();
            }
            else
            {
                // Start your notification listener service
                StartService(new Intent(this, typeof(MainNotificationListenerService)));
            }
        }

        private void RequestDrawOverAppsPermission()
        {
            if (!Settings.CanDrawOverlays(this))
            {
                // Prompt the user to grant "Draw over other apps" permission
                var intent = new Intent(Android.Provider.Settings.ActionManageOverlayPermission, Android.Net.Uri.Parse("package:" + PackageName));
                StartActivityForResult(intent, OverlayPermissionRequestCode);
            }
        }
    }
}