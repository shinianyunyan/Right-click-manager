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

        private IReadOnlyList<Models.SystemShellGroup>? systemItems;
        private IReadOnlyList<Models.SystemShellGroup>? blockedSystemItems;
        private IReadOnlyList<Models.SystemShellGroup>? interceptedSystemItems;

        public IReadOnlyList<Models.SystemShellGroup>? SystemItems
        {
            get => systemItems;
            private set => SetProperty(ref systemItems, value);
        }

        public IReadOnlyList<Models.SystemShellGroup>? BlockedSystemItems
        {
            get => blockedSystemItems;
            private set => SetProperty(ref blockedSystemItems, value);
        }

        public IReadOnlyList<Models.SystemShellGroup>? InterceptedSystemItems
        {
            get => interceptedSystemItems;
            private set => SetProperty(ref interceptedSystemItems, value);
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
        public string CurrentLanguage
        {
            get
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RightClickManager", false);
                    return key?.GetValue("Language")?.ToString() ?? "system";
                }
                catch { return "system"; }
            }
            set
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\RightClickManager");
                    key.SetValue("Language", value);
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnPropertyChanged(nameof(IsSystemLanguage));
                    OnPropertyChanged(nameof(IsChineseLanguage));
                    OnPropertyChanged(nameof(IsEnglishLanguage));
                    
                    App.ApplyLanguage(value);
                }
                catch { }
            }
        }

        public bool IsSystemLanguage => CurrentLanguage == "system";
        public bool IsChineseLanguage => CurrentLanguage == "zh-CN";
        public bool IsEnglishLanguage => CurrentLanguage == "en-US";

        public RightClickManager.Base.RelayCommand<string> SetLanguageCommand => new RightClickManager.Base.RelayCommand<string>(lang =>
        {
            if (!string.IsNullOrEmpty(lang))
            {
                CurrentLanguage = lang;
            }
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

                // --- System-level item enumeration (non-PackagedCom extensions + shell verbs) ---
                var sysAllowed = new List<Models.SystemShellItem>();
                var sysBlocked = new List<Models.SystemShellItem>();
                var sysIntercepted = new List<Models.SystemShellItem>();

                // Filter already-seen PackagedCom CLSIDs to avoid duplicates
                var packagedComClsids = new HashSet<Guid>();
                foreach (var pkg in comPackages)
                    foreach (var info in pkg.Clsids)
                        packagedComClsids.Add(info.Clsid);

                // 1. Shell extensions from non-PackagedCom shellex paths
                foreach (var root in ShellMenuScanner.ExtensionRoots)
                {
                    try
                    {
                        using var rootKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(root, false);
                        if (rootKey == null) continue;
                        foreach (var name in rootKey.GetSubKeyNames())
                        {
                            if (!Guid.TryParse(name, out var clsid)) continue;
                            if (packagedComClsids.Contains(clsid)) continue;

                            if (!string.IsNullOrEmpty(search))
                            {
                                if (!clsid.ToString("B").Contains(search, StringComparison.OrdinalIgnoreCase)
                                    && !clsid.ToString("N").Contains(search, StringComparison.OrdinalIgnoreCase)
                                    && !root.Contains(search, StringComparison.OrdinalIgnoreCase))
                                    continue;
                            }

                            var displayName = ShellMenuScanner.ResolveExtensionDisplayName(clsid);
                            var fullDisplay = $"[{root.Split('\\')[0]}] {displayName}";

                            bool isBlocked = dict.ContainsKey(clsid);
                            bool pending = isBlocked && dict.TryGetValue(clsid, out var bc) && bc.IsPending;
                            bool blocked = isBlocked && !pending;
                            bool canModify = !isBlocked || dict[clsid].Type != PackagedComHelper.BlockedClsidType.LocalMachine;

                            var item = new Models.SystemShellItem(
                                clsid.ToString("B"), fullDisplay, root, isVerb: false,
                                clsid.ToString("B"), isBlocked, pending, canModify);

                            if (!isBlocked)
                                sysAllowed.Add(item);
                            else if (pending)
                                sysIntercepted.Add(item);
                            else
                                sysBlocked.Add(item);
                        }
                    }
                    catch { }
                }

                // 2. Shell verbs from shell paths
                foreach (var root in ShellMenuScanner.VerbRoots)
                {
                    try
                    {
                        using var rootKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(root, false);
                        if (rootKey == null) continue;
                        foreach (var verbName in rootKey.GetSubKeyNames())
                        {
                            var verbPath = root + "\\" + verbName;

                            if (!string.IsNullOrEmpty(search))
                            {
                                if (!verbPath.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    && !verbName.Contains(search, StringComparison.OrdinalIgnoreCase))
                                    continue;
                            }

                            var verbDisplay = ShellMenuScanner.ResolveVerbDisplayName(verbPath);
                            var fullDisplay = $"[{root.Split('\\')[0]}] {verbDisplay}";

                            bool verbBlocked = ShellMenuScanner.IsVerbBlocked(verbPath);
                            bool verbPending = verbBlocked && ShellMenuScanner.IsVerbPending(verbPath);

                            var item = new Models.SystemShellItem(
                                verbPath, fullDisplay, root, isVerb: true,
                                handlerClsid: null, verbBlocked, verbPending, canModify: true);

                            if (!verbBlocked)
                                sysAllowed.Add(item);
                            else if (verbPending)
                                sysIntercepted.Add(item);
                            else
                                sysBlocked.Add(item);
                        }
                    }
                    catch { }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    SystemItems = GroupByCategory(sysAllowed);
                    BlockedSystemItems = GroupByCategory(sysBlocked);
                    InterceptedSystemItems = GroupByCategory(sysIntercepted);
                });

                return list;

                static bool GuidContains(Guid guid, string text) =>
                    guid.ToString("B").Contains(text, StringComparison.OrdinalIgnoreCase)
                    || guid.ToString("N").Contains(text, StringComparison.OrdinalIgnoreCase);
            });
        });

        private static IReadOnlyList<Models.SystemShellGroup> GroupByCategory(List<Models.SystemShellItem> items)
        {
            return items
                .GroupBy(i => i.Category.Split('\\')[0])
                .Select(g => new Models.SystemShellGroup(g.Key, g.ToList()))
                .ToList();
        }
    }
}

