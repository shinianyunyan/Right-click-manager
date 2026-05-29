using RightClickManager.Base;
using RightClickManager.Helpers;
using RightClickManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using static RightClickManager.Helpers.PackagedComHelper;

namespace RightClickManager.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private IReadOnlyList<PackagedAppModel>? apps;
        private IReadOnlyList<PackagedAppModel>? blockedApps;
        private IReadOnlyList<PackagedAppModel>? interceptedApps;
        private string searchingText = "";
        private AsyncRelayCommand<string>? searchCommand;
        private CancellationTokenSource? _searchCts;

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
            Helpers.Logger.Info($"SearchCommand lambda enter, search='{search ?? "(null)"}'");

            // Cancel any previous search still in flight
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            // Clear all lists immediately so UI shows empty placeholders
            Apps = null;
            BlockedApps = null;
            InterceptedApps = null;
            SystemItems = null;
            BlockedSystemItems = null;
            InterceptedSystemItems = null;

            try
            {
            // 注意：Task.Run 内部不能直接 async/await GetPackageAppInfoAsync（否则 Task.Run 返回 Task<Task>）
            // 改为先在后台线程同步完成所有 IO，避免 continuation 调度混乱
            var result = await Task.Run(async () =>
            {
                Helpers.Logger.Info("Task.Run begin: scanning registry");
                token.ThrowIfCancellationRequested();
                var localComPackages = PackagedComHelper.GetAllComPackages();
                var localBlockedClsids = PackagedComHelper.GetBlockedClsids();
                token.ThrowIfCancellationRequested();

                var clsidDllPaths = new Dictionary<Guid, string>();
                foreach (var pkg in localComPackages)
                    foreach (var ci in pkg.Clsids)
                        if (!string.IsNullOrEmpty(ci.DllPath))
                            clsidDllPaths[ci.Clsid] = ci.DllPath;

                var dict = localBlockedClsids
                    .GroupBy(c => c.Clsid)
                    .Select(g => g.OrderByDescending(c => c.Type == PackagedComHelper.BlockedClsidType.CurrentUser ? 1 : 0)
                                  .ThenByDescending(c => c.IsPending ? 1 : 0)
                                  .First())
                    .ToDictionary(c => c.Clsid, c => c);

                var allowedList = new List<PackagedAppModel>(localComPackages.Length);
                var blockedList = new List<PackagedAppModel>();
                var interceptedList = new List<PackagedAppModel>();

                for (int i = 0; i < localComPackages.Length; i++)
                {
                    if (i % 5 == 0) token.ThrowIfCancellationRequested();
                {
                    if (localComPackages[i].Clsids.Length > 0)
                    {
                        var packageInfo = PackageManager.GetPackageInfoByFullName(localComPackages[i].PackageFullName);
                        if (packageInfo != null)
                        {
                            var appInfo = await PackageManager.GetPackageAppInfoAsync(packageInfo).ConfigureAwait(false);
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
                                        allowedList.Add(new PackagedAppModel(appInfo, packageInfo, allowed, dict, clsidDllPaths));
                                    if (blockedManually.Count > 0)
                                        blockedList.Add(new PackagedAppModel(appInfo, packageInfo, blockedManually, dict, clsidDllPaths));
                                    if (autoIntercepted.Count > 0)
                                        interceptedList.Add(new PackagedAppModel(appInfo, packageInfo, autoIntercepted, dict, clsidDllPaths));
                                }
                            }
                        }
                    }
                }
                }

                // --- System-level item enumeration ---
                var sysAllowed = new List<Models.SystemShellItem>();
                var sysBlocked = new List<Models.SystemShellItem>();
                var sysIntercepted = new List<Models.SystemShellItem>();

                var packagedComClsids = new HashSet<Guid>();
                foreach (var pkg in localComPackages)
                    foreach (var info in pkg.Clsids)
                        packagedComClsids.Add(info.Clsid);

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

                            if (!isBlocked) sysAllowed.Add(item);
                            else if (pending) sysIntercepted.Add(item);
                            else sysBlocked.Add(item);
                        }
                    }
                    catch { }
                }

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

                            // 一次句柄同时读取状态+名称，避免三次重复注册表 IO
                            var (verbBlocked, verbPending, verbDisplay) = ShellMenuScanner.GetVerbInfo(verbPath);
                            var fullDisplay = $"[{root.Split('\\')[0]}] {verbDisplay}";

                            var item = new Models.SystemShellItem(
                                verbPath, fullDisplay, root, isVerb: true,
                                handlerClsid: null, verbBlocked, verbPending, canModify: true);

                            if (!verbBlocked) sysAllowed.Add(item);
                            else if (verbPending) sysIntercepted.Add(item);
                            else sysBlocked.Add(item);
                        }
                    }
                    catch { }
                }

                Helpers.Logger.Info("Task.Run returning all results");
                return (allowedList, blockedList, interceptedList, sysAllowed, sysBlocked, sysIntercepted);

                static bool GuidContains(Guid guid, string text) =>
                    guid.ToString("B").Contains(text, StringComparison.OrdinalIgnoreCase)
                    || guid.ToString("N").Contains(text, StringComparison.OrdinalIgnoreCase);
            });

            // All UI updates happen here, on the UI thread, in one batch
            Helpers.Logger.Info("Setting all properties on UI thread");
            Apps = result.allowedList;
            BlockedApps = result.blockedList;
            InterceptedApps = result.interceptedList;
            SystemItems = GroupByCategory(result.sysAllowed);
            BlockedSystemItems = GroupByCategory(result.sysBlocked);
            InterceptedSystemItems = GroupByCategory(result.sysIntercepted);
            Helpers.Logger.Info("All properties set");
            }
            catch (OperationCanceledException)
            {
                Helpers.Logger.Info("Search cancelled by new request");
            }
            catch (Exception ex)
            {
                Helpers.Logger.Error("Search failed", ex.ToString());
            }
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

