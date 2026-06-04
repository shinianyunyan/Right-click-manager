using Microsoft.Win32;
using System;
using System.Collections.Generic;
using Windows.Win32;

namespace RightClickManager.Helpers
{
    public static class ShellMenuScanner
    {
        /// <summary>HKCR paths that host shell context menu verbs.</summary>
        public static readonly string[] VerbRoots =
        [
            @"*\shell",
            @"Directory\shell",
            @"Directory\Background\shell",
            @"Drive\shell",
            @"Folder\shell",
            @"AllFilesystemObjects\shell",
            @"DesktopBackground\shell",
            @"LibraryFolder\shell",
            @"LibraryFolder\Background\shell",
        ];

        /// <summary>HKCR paths that host shell extension CLSIDs.</summary>
        public static readonly string[] ExtensionRoots =
        [
            @"*\shellex\ContextMenuHandlers",
            @"Directory\shellex\ContextMenuHandlers",
            @"Directory\Background\shellex\ContextMenuHandlers",
            @"Drive\shellex\ContextMenuHandlers",
            @"Folder\shellex\ContextMenuHandlers",
            @"AllFilesystemObjects\shellex\ContextMenuHandlers",
            @"DesktopBackground\shellex\ContextMenuHandlers",
        ];

        private const int HideBasedOnVelocityIdDisabled = 0x639bc8;
        private const string PendingMarker = "RightClickManager_Pending";

        public static HashSet<Guid> ScanAllExtensionClsids()
        {
            var set = new HashSet<Guid>();

            // 1. PackagedCom (UWP apps)
            var packages = PackagedComHelper.GetAllComPackages();
            foreach (var pkg in packages)
                foreach (var info in pkg.Clsids)
                    set.Add(info.Clsid);

            // 2. All shellex\ContextMenuHandlers paths
            foreach (var root in ExtensionRoots)
            {
                try
                {
                    using var key = Registry.ClassesRoot.OpenSubKey(root, false);
                    if (key == null) continue;
                    foreach (var name in key.GetSubKeyNames())
                    {
                        if (Guid.TryParse(name, out var clsid))
                            set.Add(clsid);
                    }
                }
                catch { }
            }

            return set;
        }

        public static HashSet<string> ScanAllVerbPaths()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in VerbRoots)
            {
                try
                {
                    using var key = Registry.ClassesRoot.OpenSubKey(root, false);
                    if (key == null) continue;
                    foreach (var verbName in key.GetSubKeyNames())
                    {
                        set.Add(root + "\\" + verbName);
                    }
                }
                catch { }
            }

            return set;
        }

        public static bool BlockVerb(string verbRegistryPath, bool isPending = false)
        {
            try
            {
                using var key = Registry.ClassesRoot.CreateSubKey(verbRegistryPath, true);
                if (key == null) return false;

                key.SetValue("HideBasedOnVelocityId", HideBasedOnVelocityIdDisabled, RegistryValueKind.DWord);

                if (key.GetValue("ShowAsDisabledIfHidden") == null)
                {
                    key.SetValue("ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
                }

                if (!IsOpenInNewWindowVerb(verbRegistryPath))
                {
                    key.SetValue("LegacyDisable", string.Empty, RegistryValueKind.String);
                }

                if (isPending)
                {
                    key.SetValue(PendingMarker, 1, RegistryValueKind.DWord);
                }

                return true;
            }
            catch { return false; }
        }

        public static bool UnblockVerb(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.CreateSubKey(verbRegistryPath, true);
                if (key == null) return false;

                key.DeleteValue("HideBasedOnVelocityId", false);
                key.DeleteValue("LegacyDisable", false);
                key.DeleteValue("ProgrammaticAccessOnly", false);
                key.DeleteValue(PendingMarker, false);

                return true;
            }
            catch { return false; }
        }

        public static bool IsVerbBlocked(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(verbRegistryPath, false);
                if (key == null) return false;

                if (key.GetValue("HideBasedOnVelocityId") is int vid && vid == HideBasedOnVelocityIdDisabled)
                    return true;

                if (key.GetValue("LegacyDisable") != null || key.GetValue("ProgrammaticAccessOnly") != null)
                    return true;

                return false;
            }
            catch { return false; }
        }

        public static void DeleteVerbPendingMarker(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.CreateSubKey(verbRegistryPath, true);
                key?.DeleteValue(PendingMarker, false);
            }
            catch { }
        }

        public static bool IsVerbPending(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(verbRegistryPath, false);
                return key?.GetValue(PendingMarker) is int v && v == 1;
            }
            catch { return false; }
        }

        /// <summary>
        /// 一次注册表句柄同时读取 blocked/pending 状态和显示名称，
        /// 避免热路径中对同一个 key 三次重复 IO。
        /// </summary>
        public static (bool isBlocked, bool isPending, string displayName) GetVerbInfo(string verbRegistryPath)
        {
            bool isBlocked = false;
            bool isPending = false;
            string displayName = verbRegistryPath[(verbRegistryPath.LastIndexOf('\\') + 1)..];

            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(verbRegistryPath, false);
                if (key != null)
                {
                    if (key.GetValue("HideBasedOnVelocityId") is int vid && vid == HideBasedOnVelocityIdDisabled)
                        isBlocked = true;
                    else if (key.GetValue("LegacyDisable") != null || key.GetValue("ProgrammaticAccessOnly") != null)
                        isBlocked = true;

                    if (isBlocked && key.GetValue(PendingMarker) is int v && v == 1)
                        isPending = true;

                    var muiverb = key.GetValue("MUIVerb") as string;
                    if (!string.IsNullOrEmpty(muiverb))
                        displayName = ResolveIndirectString(muiverb);
                    else
                    {
                        var defaultVal = key.GetValue("") as string;
                        if (!string.IsNullOrEmpty(defaultVal))
                            displayName = ResolveIndirectString(defaultVal);
                    }
                }
            }
            catch { }

            return (isBlocked, isPending, displayName);
        }

        public static string ResolveVerbDisplayName(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(verbRegistryPath, false);
                if (key != null)
                {
                    var muiverb = key.GetValue("MUIVerb") as string;
                    if (!string.IsNullOrEmpty(muiverb))
                        return ResolveIndirectString(muiverb);

                    var defaultVal = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(defaultVal))
                        return ResolveIndirectString(defaultVal);
                }
            }
            catch { }

            var lastSlash = verbRegistryPath.LastIndexOf('\\');
            return lastSlash >= 0 ? verbRegistryPath[(lastSlash + 1)..] : verbRegistryPath;
        }

        public static string ResolveExtensionDisplayName(Guid clsid)
        {
            try
            {
                var clsidStr = clsid.ToString("B").ToUpperInvariant();
                using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + clsidStr, false);
                if (key != null)
                {
                    var defaultVal = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(defaultVal))
                        return ResolveIndirectString(defaultVal);
                }
            }
            catch { }
            return clsid.ToString("B");
        }

        /// <summary>Resolves indirect string references like @path,-id or @{package?resource}.</summary>
        private static string ResolveIndirectString(string raw)
        {
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith("@"))
                return raw;

            try
            {
                Span<char> buffer = stackalloc char[1024];
                var hr = PInvoke.SHLoadIndirectString(raw, buffer);
                if (hr.Succeeded)
                {
                    var len = buffer.IndexOf('\0');
                    if (len >= 0)
                        return buffer[..len].ToString();
                }
            }
            catch { }
            return raw;
        }

        private static bool IsOpenInNewWindowVerb(string registryPath)
        {
            return registryPath.EndsWith(@"\Folder\shell\opennewwindow", StringComparison.OrdinalIgnoreCase)
                || registryPath.Equals(@"Folder\shell\opennewwindow", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Resolves the file path for a CLSID from registry (InProcServer32 / LocalServer32).</summary>
        public static string? ResolveClsidFilePath(Guid clsid)
        {
            try
            {
                var clsidStr = clsid.ToString("B").ToUpperInvariant();
                var path = GetClsidServerPath(clsidStr, "InProcServer32")
                        ?? GetClsidServerPath(clsidStr, "LocalServer32");
                if (!string.IsNullOrEmpty(path))
                {
                    path = ResolveIndirectString(path);
                    path = System.Environment.ExpandEnvironmentVariables(path);
                    if (System.IO.File.Exists(path))
                        return path;
                }
            }
            catch { }
            return null;
        }

        private static string? GetClsidServerPath(string clsidStr, string serverKey)
        {
            using var key = Registry.ClassesRoot.OpenSubKey(
                $@"CLSID\{clsidStr}\{serverKey}", false);
            return key?.GetValue(null) as string;
        }

        /// <summary>Resolves the target file for a shell verb from its command key.</summary>
        public static string? ResolveVerbFilePath(string verbRegistryPath)
        {
            try
            {
                using var cmdKey = Registry.ClassesRoot.OpenSubKey(
                    verbRegistryPath + @"\command", false);
                var cmd = cmdKey?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(cmd))
                    return ExtractExePath(ResolveIndirectString(cmd));
            }
            catch { }
            return null;
        }

        private static string? ExtractExePath(string commandLine)
        {
            var trimmed = commandLine.Trim();
            if (trimmed.StartsWith("\""))
            {
                var end = trimmed.IndexOf('\"', 1);
                if (end > 1)
                    trimmed = trimmed[1..end];
            }
            else
            {
                var end = trimmed.IndexOf(' ');
                if (end > 0)
                    trimmed = trimmed[..end];
            }

            trimmed = System.Environment.ExpandEnvironmentVariables(trimmed);
            if (System.IO.File.Exists(trimmed))
                return trimmed;
            return null;
        }
    }
}
