using System.Collections.Generic;

namespace AutorunManager
{
    public interface IAppInfoService
    {
        List<AppInfo> GetInstalledApps();
    }
}
