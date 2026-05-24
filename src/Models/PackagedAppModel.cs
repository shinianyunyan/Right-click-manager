using Avalonia.Media.Imaging;
using RightClickManager.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightClickManager.Models
{
    public partial class PackagedAppModel
    {
        public PackagedAppModel(
            AppInfo appInfo,
            PackageInfo packageInfo,
            IEnumerable<ContextMenuItem> items,
            Dictionary<Guid, PackagedComHelper.BlockedClsid> blockedClsids)
        {
            AppInfo = appInfo;
            PackageInfo = packageInfo;
            ContextMenuItems = items.Select(c =>
            {
                if (blockedClsids.TryGetValue(c.Clsid, out var blockedClsid))
                {
                    return new ContextMenuItemCheckModel(c, false, blockedClsid.Type != PackagedComHelper.BlockedClsidType.LocalMachine, blockedClsid.IsPending);
                }
                return new ContextMenuItemCheckModel(c, true, true, false);
            }).ToArray();

            if (string.IsNullOrEmpty(AppInfo.DisplayName))
            {
                DisplayName = $"{PackageInfo.PackageFamilyName}";
            }
            else
            {
                DisplayName = $"{AppInfo.DisplayName}\n{PackageInfo.PackageFamilyName}";
            }
        }

        public AppInfo AppInfo { get; }

        public PackageInfo PackageInfo { get; }

        public IReadOnlyList<ContextMenuItemCheckModel> ContextMenuItems { get; }

        public string DisplayName { get; }

        public Bitmap? Icon
        {
            get
            {
                if (!string.IsNullOrEmpty(AppInfo.IconPath))
                {
                    var iconPath = System.IO.Path.Combine(PackageInfo.PackageInstallLocation, AppInfo.IconPath);
                    if (System.IO.File.Exists(iconPath))
                    {
                        return new Bitmap(iconPath);
                    }
                }
                return null;
            }
        }

        public RightClickManager.Base.RelayCommand EnableAllCommand => new RightClickManager.Base.RelayCommand(() =>
        {
            foreach (var item in ContextMenuItems)
            {
                if (item.CanModify) item.Enabled = true;
            }
        });

        public RightClickManager.Base.RelayCommand DisableAllCommand => new RightClickManager.Base.RelayCommand(() =>
        {
            foreach (var item in ContextMenuItems)
            {
                if (item.CanModify) item.Enabled = false;
            }
        });
    }
}
