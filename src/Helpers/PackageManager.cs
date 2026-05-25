using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.Win32;


namespace RightClickManager.Helpers
{
    internal static class PackageManager
    {
        private static string[]? defaultLanguages;

        public static PackageInfo? GetPackageInfoByFullName(string packageFullName)
        {
            try
            {
                using var packageInfoKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\{packageFullName}", false);
                var installPath = packageInfoKey?.GetValue("Path")?.ToString();
                if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                {
                    return new PackageInfo(
                        installPath,
                        packageFullName,
                        packageFullName.Split('_')[0],
                        new PackageId(
                            PackageId.ProcessorArchitecture.X64,
                            new Version(0, 0),
                            packageFullName.Split('_')[0],
                            "",
                            "",
                            ""));
                }
            }
            catch { }

            return null;
        }


        public static async Task<AppInfo?> GetPackageAppInfoAsync(PackageInfo packageInfo, CancellationToken cancellationToken = default)
        {
            var xmlDocument = await GetAppxManifestDocumentAsync(packageInfo.PackageInstallLocation, cancellationToken);

            if (xmlDocument != null)
            {
                IReadOnlyList<ContextMenuItem>? clsids = null;

                var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);

                namespaceManager.AddNamespace("default", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

                var nodes = xmlDocument.SelectNodes("//*[local-name()='FileExplorerContextMenus']//*[local-name()='Verb']", namespaceManager);

                if (nodes?.Count is > 0)
                {
                    var list = new List<ContextMenuItem>(nodes.Count);
                    if (nodes != null)
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            var type = nodes[i]?.ParentNode?.Attributes?["Type"]?.Value ?? "";
                            var id = nodes[i]?.Attributes?["Id"]?.Value ?? "";
                            var clsid = nodes[i]?.Attributes?["Clsid"]?.Value;
                            if (Guid.TryParse(clsid, out var guid))
                            {
                                list.Add(new(type, id, guid, PackagedComHelper.TryGetExplorerCommandTitle(guid, type)));
                            }
                        }
                    }

                    clsids = [.. list.Distinct()];
                }

                var logoNode = xmlDocument.SelectSingleNode("//default:Properties/default:Logo", namespaceManager);
                string logo = logoNode?.InnerText ?? "";
                var logoFullPath = Path.Combine(packageInfo.PackageInstallLocation, logo);

                if (!File.Exists(logoFullPath))
                {
                    var logoDirectory = Path.GetDirectoryName(logoFullPath);
                    logoFullPath = "";
                    var logoKey = Path.GetFileNameWithoutExtension(logo);
                    var ext = Path.GetExtension(logo);
                    if (Directory.Exists(logoDirectory))
                    {
                        var files = Directory.GetFiles(logoDirectory, $"{logoKey}*{ext}");
                        logoFullPath = files?.FirstOrDefault(c => !c.Contains("contrast"));
                        if (string.IsNullOrEmpty(logoFullPath)) logoFullPath = files?.FirstOrDefault() ?? "";
                    }
                }

                var appNodes = xmlDocument.SelectNodes("//uap:VisualElements", namespaceManager);

                if (appNodes != null && appNodes.Count > 0)
                {
                    foreach (XmlNode appNode in appNodes.OfType<XmlNode>().OrderBy(c => c.Attributes?["AppListEntry"]?.Value == "none" ? 1 : 0))
                    {
                        var displayName = appNode.Attributes?["DisplayName"]?.Value ?? "";

                        if (displayName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase) is true)
                        {
                            var displayName2 = GetDisplayNameFromResource(packageInfo.PackageFullName, displayName);
                            if (!string.IsNullOrEmpty(displayName2)) displayName = displayName2;
                        }

                        return new(displayName, logoFullPath, appNode.Attributes?["AppListEntry"]?.Value != "none", clsids ?? []);
                    }
                }

                if (clsids != null) return new AppInfo("", logoFullPath, false, clsids);
            }
            return null;

            static string? GetDisplayNameFromResource(string packageFullName, string resourceUri)
            {
                const int BufferLength = 1000;

                using var owner = MemoryPool<char>.Shared.Rent(BufferLength);
                var span = owner.Memory.Span[..BufferLength];
                var hr = PInvoke.SHLoadIndirectString($"@{{{packageFullName}? {resourceUri}}}", span);
                if (hr == 0x80073B17)
                {
                    resourceUri = resourceUri.Replace("ms-resource:", "ms-resource:Resources/", StringComparison.OrdinalIgnoreCase);
                    hr = PInvoke.SHLoadIndirectString($"@{{{packageFullName}? {resourceUri}}}", span);
                }
                if (hr.Succeeded)
                {
                    var length = span.IndexOf('\0');
                    if(length > 0)
                    {
                        return span[..length].ToString();
                    }
                }
                return null;
            }
        }

        private static async Task<XmlDocument?> GetAppxManifestDocumentAsync(string packageInstallLocation, CancellationToken cancellationToken = default)
        {
            try
            {
                var manifestPath = Path.Combine(packageInstallLocation, "AppxManifest.xml");
                if (File.Exists(manifestPath))
                {
                    var contents = await File.ReadAllTextAsync(manifestPath, cancellationToken);

                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(contents);

                    return xmlDocument;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
            return null;
        }

        private unsafe static string[] GetPreferredUILanguages()
        {
            if (defaultLanguages != null) return defaultLanguages;

            const uint MUI_LANGUAGE_NAME = 0x8;
            const uint MUI_MERGE_SYSTEM_FALLBACK = 0x10;
            const uint MUI_MERGE_USER_FALLBACK = 0x20;

            uint langCount = 0;
            uint bufferLen = 0;

            if (Windows.Win32.PInvoke.GetThreadPreferredUILanguages(
                 MUI_MERGE_SYSTEM_FALLBACK | MUI_MERGE_USER_FALLBACK | MUI_LANGUAGE_NAME,
                 &langCount,
                 default,
                 &bufferLen))
            {
                var buffer = new char[bufferLen];
                fixed (char* pBuffer = buffer)
                {
                    if (Windows.Win32.PInvoke.GetThreadPreferredUILanguages(
                        MUI_MERGE_SYSTEM_FALLBACK | MUI_MERGE_USER_FALLBACK | MUI_LANGUAGE_NAME,
                        &langCount,
                        pBuffer,
                        &bufferLen))
                    {
                        bool enFlag = false;
                        bool enUSFlag = false;

                        var langs = new List<string>((int)langCount);
                        for (int start = 0, i = 0; i < bufferLen; i++)
                        {
                            if (buffer[i] == '\0')
                            {
                                if (i - start > 0)
                                {
                                    var lang = new string(buffer, start, i - start);
                                    langs.Add(lang);

                                    if (!enFlag && string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase)) enFlag = true;
                                    else if (!enUSFlag && string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase)) enUSFlag = true;
                                    else if (string.Equals(lang, "zh-Hans", StringComparison.OrdinalIgnoreCase)) langs.Add("zh-CHS");
                                    else if (string.Equals(lang, "zh-Hant", StringComparison.OrdinalIgnoreCase)) langs.Add("zh-CHT");
                                }
                                start = i + 1;
                            }
                        }

                        defaultLanguages = [.. langs];
                    }
                }
            }

            if (defaultLanguages == null) defaultLanguages = [];

            return defaultLanguages;
        }
    }

    public record class ContextMenuItem(string Type, string Id, Guid Clsid, string? Title);

    public record class AppInfo(string DisplayName, string IconPath, bool AppListEntry, IReadOnlyList<ContextMenuItem> ContextMenuItems);

    public record class PackageInfo(
        string PackageInstallLocation,
        string PackageFullName,
        string PackageFamilyName,
        PackageId PackageId);

    public record PackageId(
        PackageId.ProcessorArchitecture Architecture,
        Version Version,
        string Name,
        string Publisher,
        string? ResourceId,
        string PublisherId)
    {
        public enum ProcessorArchitecture : uint
        {
            /// <summary>
            /// The ARM processor architecture.
            /// </summary>
            Arm = 5,

            /// <summary>
            /// The Arm64 processor architecture.
            /// </summary>
            Arm64 = 12,

            /// <summary>
            /// A neutral processor architecture.
            /// </summary>
            Neutral = 11,

            /// <summary>
            /// An unknown processor architecture.
            /// </summary>
            Unknown = 65535,

            /// <summary>
            /// The x64 processor architecture.
            /// </summary>
            X64 = 9,

            /// <summary>
            /// The x86 processor architecture.
            /// </summary>
            X86 = 0,

            /// <summary>
            /// The Arm64 processor architecture emulating the X86 architecture.
            /// </summary>
            X86OnArm64 = 14,
        }
    }
}
