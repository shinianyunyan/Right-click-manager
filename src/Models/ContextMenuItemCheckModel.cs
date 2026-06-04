using RightClickManager.Base;
using RightClickManager.Helpers;
using System;

namespace RightClickManager.Models
{
    public partial class ContextMenuItemCheckModel : ObservableObject
    {
        private bool enabled;

        public ContextMenuItemCheckModel(ContextMenuItem contextMenuItem, bool enabled, bool canModify, bool isPending = false, string? knownDllPath = null)
        {
            ContextMenuItem = contextMenuItem;
            CanModify = canModify;
            this.enabled = enabled;
            this.isPending = isPending;
            _knownDllPath = knownDllPath;
        }

        public ContextMenuItem ContextMenuItem { get; }

        public bool CanModify { get; }

        public bool IsPending { get => isPending; set => SetProperty(ref isPending, value); }
        private bool isPending;

        public bool Enabled
        {
            get => enabled;
            set
            {
                SetProperty(ref enabled, value,
                    onPropertyChanging: (oldValue, newValue) =>
                        PackagedComHelper.SetBlockedClsid(ContextMenuItem.Clsid, PackagedComHelper.BlockedClsidType.CurrentUser, !newValue, false),
                    onPropertyChanged: (_, _) => OnPropertyChanged(nameof(StatusColor)),
                    notifyWhenNotChanged: true,
                    asyncNotifyWhenNotChanged: true);
            }
        }

        public string StatusColor => enabled ? "#4CAF50" : "#F44336";

        private string? _displayName;

        public string DisplayName
        {
            get
            {
                if (_displayName != null) return _displayName;

                var title = ContextMenuItem.Title;
                // TryGetExplorerCommandTitle 会 CoCreateInstance，只在第一次调用，结果缓存
                if (string.IsNullOrEmpty(title))
                    title = PackagedComHelper.TryGetExplorerCommandTitle(ContextMenuItem.Clsid, ContextMenuItem.Type);
                if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(ContextMenuItem.Id))
                    title = $"[{ContextMenuItem.Id}]";

                if (!string.IsNullOrEmpty(ContextMenuItem.Type) && !string.IsNullOrEmpty(title))
                    _displayName = $"[{ContextMenuItem.Type}] {title}\n{ContextMenuItem.Clsid:B}";
                else if (!string.IsNullOrEmpty(ContextMenuItem.Type))
                    _displayName = $"[{ContextMenuItem.Type}]\n{ContextMenuItem.Clsid:B}";
                else if (!string.IsNullOrEmpty(title))
                    _displayName = $"{title}\n{ContextMenuItem.Clsid:B}";
                else
                    _displayName = $"{ContextMenuItem.Clsid:B}";

                return _displayName;
            }
        }

        private string? _knownDllPath;
        private string? _filePath;
        private bool _filePathResolved;

        public string? FilePath
        {
            get
            {
                if (!_filePathResolved)
                {
                    // Prefer the known DLL path from PackagedCom data
                    if (!string.IsNullOrEmpty(_knownDllPath))
                    {
                        var expanded = System.Environment.ExpandEnvironmentVariables(_knownDllPath!);
                        if (System.IO.File.Exists(expanded))
                            _filePath = expanded;
                    }
                    // Fall back to registry CLSID lookup
                    _filePath ??= ShellMenuScanner.ResolveClsidFilePath(ContextMenuItem.Clsid);
                    _filePathResolved = true;
                }
                return _filePath;
            }
        }

        public bool HasFilePath => !string.IsNullOrEmpty(FilePath);

        public RelayCommand OpenFileLocationCommand => new RelayCommand(() =>
        {
            var path = FilePath;
            if (!string.IsNullOrEmpty(path))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        });
    }
}
