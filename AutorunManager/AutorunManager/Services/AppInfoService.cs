using System.Collections.Generic;
using AutorunManager.Model;

namespace AutorunManager.Services
{
    public interface IAppInfoService
    {
        List<AppInfo> GetInstalledApps();
    }
}
