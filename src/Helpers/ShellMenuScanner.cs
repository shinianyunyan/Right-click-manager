using Microsoft.Win32;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Scans PackagedCom + all shellex\ContextMenuHandlers paths and returns all CLSIDs.
        /// </summary>
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

        /// <summary>
        /// Scans all shell verb paths and returns the set of full registry paths (relative to HKCR).
        /// </summary>
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

        /// <summary>
        /// Blocks a shell verb by setting HideBasedOnVelocityId, LegacyDisable, and ProgrammaticAccessOnly.
        /// When isPending is true, also writes a pending marker so the UI can show it as intercepted.
        /// </summary>
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

        /// <summary>
        /// Unblocks a shell verb by removing all hide/disable values and the pending marker.
        /// </summary>
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

        /// <summary>
        /// Checks if a shell verb is currently blocked/hidden.
        /// </summary>
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

        /// <summary>
        /// Checks if the pending marker exists on a shell verb key.
        /// </summary>
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
        /// Resolves a human-readable display name for a shell verb registry key.
        /// </summary>
        public static string ResolveVerbDisplayName(string verbRegistryPath)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(verbRegistryPath, false);
                if (key != null)
                {
                    var muiverb = key.GetValue("MUIVerb") as string;
                    if (!string.IsNullOrEmpty(muiverb))
                        return muiverb;

                    var defaultVal = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(defaultVal))
                        return defaultVal;
                }
            }
            catch { }

            // Fallback: use the verb key name (last segment of the path)
            var lastSlash = verbRegistryPath.LastIndexOf('\\');
            return lastSlash >= 0 ? verbRegistryPath[(lastSlash + 1)..] : verbRegistryPath;
        }

        /// <summary>
        /// Resolves a display name for a shell extension from the CLSID key.
        /// </summary>
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
                        return defaultVal;
                }
            }
            catch { }
            return clsid.ToString("B");
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
            using var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(
                $@"CLSID\{clsidStr}\{serverKey}", false);
            return key?.GetValue(null) as string;
        }

        /// <summary>Resolves the target file for a shell verb from its command key.</summary>
        public static string? ResolveVerbFilePath(string verbRegistryPath)
        {
            try
            {
                using var cmdKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(
                    verbRegistryPath + @"\command", false);
                var cmd = cmdKey?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(cmd))
                    return ExtractExePath(cmd);
            }
            catch { }
            return null;
        }

        private static string? ExtractExePath(string commandLine)
        {
            // Strip quotes and extract the first path component
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
