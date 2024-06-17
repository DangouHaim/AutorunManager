using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Linq;
using System.IO;
using AutorunManager.Droid;

[assembly: Dependency(typeof(AppInfoService))]
namespace AutorunManager.Droid
{
    public class AppInfoService : IAppInfoService
    {
        public List<AppInfo> GetInstalledApps()
        {
            List<AppInfo> appInfos = new List<AppInfo>();

            try
            {
                PackageManager pm = Android.App.Application.Context.PackageManager;
                List<ApplicationInfo> packages = pm.GetInstalledApplications(PackageInfoFlags.MetaData).ToList();

                foreach (var package in packages)
                {
                    try
                    {
                        Drawable icon = package.LoadIcon(pm);
                        string appName = package.LoadLabel(pm).ToString();
                        string packageName = package.PackageName;

                        appInfos.Add(new AppInfo
                        {
                            AppName = appName,
                            PackageName = packageName,
                            Icon = GetImageSourceFromDrawable(icon) // Convert Drawable to ImageSource
                        });
                    }
                    catch { }
                }

                appInfos = appInfos.OrderBy(x => x.AppName).ToList();
            }
            catch { }

            return appInfos;
        }

        private ImageSource GetImageSourceFromDrawable(Drawable drawable)
        {
            try
            {
                BitmapDrawable bitmapDrawable = drawable as BitmapDrawable;

                if (bitmapDrawable != null)
                {
                    Bitmap bitmap = bitmapDrawable.Bitmap;
                    MemoryStream stream = new MemoryStream();
                    bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    stream.Seek(0L, SeekOrigin.Begin);
                    return ImageSource.FromStream(() => stream);
                }
            }
            catch { }

            return null;
        }

        private Bitmap iconToBitmap(Drawable icon)
        {
            try
            {
                if (icon is BitmapDrawable bitmapDrawable)
                {
                    return bitmapDrawable.Bitmap;
                }

                Bitmap bitmap = Bitmap.CreateBitmap(icon.IntrinsicWidth, icon.IntrinsicHeight, Bitmap.Config.Argb8888);
                Canvas canvas = new Canvas(bitmap);
                icon.SetBounds(0, 0, canvas.Width, canvas.Height);
                icon.Draw(canvas);
                return bitmap;
            }
            catch { }

            return null;
        }
    }
}