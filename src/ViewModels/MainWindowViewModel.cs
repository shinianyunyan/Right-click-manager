using RightClickManager.Base;
using RightClickManager.Helpers;
using RightClickManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using static RightClickManager.Helpers.PackagedComHelper;

namespace RightClickManager.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private ComPackage[]? comPackages;
        private BlockedClsid[]? blockedClsids;

        private IReadOnlyList<PackagedAppModel>? apps;
        private IReadOnlyList<PackagedAppModel>? blockedApps;
        private IReadOnlyList<PackagedAppModel>? interceptedApps;
        private string searchingText = "";
        private AsyncRelayCommand<string>? searchCommand;

        public IReadOnlyList<PackagedAppModel>? Apps
        {
            get => apps;
            private set => SetProperty(ref apps, value);
        }

        public IReadOnlyList<PackagedAppModel>? BlockedApps
        {
            get => blockedApps;
            private set => SetProperty(ref blockedApps, value);
        }

        public IReadOnlyList<PackagedAppModel>? InterceptedApps
        {
            get => interceptedApps;
            private set => SetProperty(ref interceptedApps, value);
        }

        public string SearchingText
        {
            get => searchingText;
            set => SetProperty(ref searchingText, value);
        }

        public bool IsAutoStartEnabled
        {
            get
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("RightClickManager") != null;
            }
        }

        public RightClickManager.Base.RelayCommand ToggleAutoStartCommand => new RightClickManager.Base.RelayCommand(() =>
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (IsAutoStartEnabled)
                    {
                        key.DeleteValue("RightClickManager", false);
                    }
                    else
                    {
                        var path = System.Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(path))
                        {
                            key.SetValue("RightClickManager", $"\"{path}\" --hidden");
                        }
                    }
                    OnPropertyChanged(nameof(IsAutoStartEnabled));
                }
            }
            catch { }
        });

        public AsyncRelayCommand<string> SearchCommand => searchCommand ??= new AsyncRelayCommand<string>(async search =>
        {
            search = search?.Trim();

            Apps = await Task.Run(async () =>
            {
                comPackages = PackagedComHelper.GetAllComPackages();
                blockedClsids = PackagedComHelper.GetBlockedClsids();

                var dict = blockedClsids
                    .DistinctBy(c => c.Clsid)
                    .ToDictionary(c => c.Clsid, c => c);

                var list = new List<PackagedAppModel>(comPackages.Length);
                var blockedList = new List<PackagedAppModel>();
                var interceptedList = new List<PackagedAppModel>();

                for (int i = 0; i < comPackages.Length; i++)
                {
                    if (comPackages[i].Clsids.Length > 0)
                    {
                        var packageInfo = PackageManager.GetPackageInfoByFullName(comPackages[i].PackageFullName);
                        if (packageInfo != null)
                        {
                            var appInfo = await PackageManager.GetPackageAppInfoAsync(packageInfo);
                            if (appInfo != null && appInfo.ContextMenuItems.Count > 0)
                            {
                                var matchingItems = appInfo.ContextMenuItems.AsEnumerable();
                                if (!string.IsNullOrEmpty(search))
                                {
                                    if (!appInfo.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchingItems = matchingItems.Where(c => GuidContains(c.Clsid, search)
                                                         || c.Type.Contains(search, StringComparison.OrdinalIgnoreCase)
                                                         || c.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
                                                         || c.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) is true);
                                    }
                                }

                                var mList = matchingItems.ToList();
                                if (mList.Count > 0)
                                {
                                    var allowed = mList.Where(c => !dict.ContainsKey(c.Clsid)).ToList();
                                    var blockedManually = mList.Where(c => dict.TryGetValue(c.Clsid, out var b) && !b.IsPending).ToList();
                                    var autoIntercepted = mList.Where(c => dict.TryGetValue(c.Clsid, out var b) && b.IsPending).ToList();

                                    if (allowed.Count > 0)
                                    {
                                        list.Add(new PackagedAppModel(appInfo, packageInfo, allowed, dict));
                                    }
                                    if (blockedManually.Count > 0)
                                    {
                                        blockedList.Add(new PackagedAppModel(appInfo, packageInfo, blockedManually, dict));
                                    }
                                    if (autoIntercepted.Count > 0)
                                    {
                                        interceptedList.Add(new PackagedAppModel(appInfo, packageInfo, autoIntercepted, dict));
                                    }
                                }
                            }
                        }
                    }
                }

                Dispatcher.UIThread.Post(() => 
                {
                    BlockedApps = blockedList;
                    InterceptedApps = interceptedList;
                });
                return list;

                static bool GuidContains(Guid guid, string text) =>
                    guid.ToString("B").Contains(text, StringComparison.OrdinalIgnoreCase)
                    || guid.ToString("N").Contains(text, StringComparison.OrdinalIgnoreCase);
            });
        });
    }
}
