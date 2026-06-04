using RightClickManager.Base;
using RightClickManager.Helpers;
using System;

namespace RightClickManager.Models
{
    public partial class SystemShellItem : ObservableObject
    {
        private bool enabled;
        private bool isPending;

        public SystemShellItem(
            string registryPath,
            string displayName,
            string category,
            bool isVerb,
            string? handlerClsid,
            bool isBlocked,
            bool isPending,
            bool canModify = true)
        {
            RegistryPath = registryPath;
            DisplayName = displayName;
            Category = category;
            IsVerb = isVerb;
            HandlerClsid = handlerClsid;
            CanModify = canModify;
            this.enabled = !isBlocked;
            this.isPending = isPending;
        }

        public string RegistryPath { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public bool IsVerb { get; }
        public string? HandlerClsid { get; }
        public bool CanModify { get; }

        public bool IsPending
        {
            get => isPending;
            set => SetProperty(ref isPending, value, onPropertyChanged: (_, _) => OnPropertyChanged(nameof(StatusColor)));
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                SetProperty(ref enabled, value,
                    onPropertyChanging: (oldValue, newValue) =>
                    {
                        if (IsVerb)
                        {
                            if (newValue)
                                ShellMenuScanner.UnblockVerb(RegistryPath);
                            else
                                ShellMenuScanner.BlockVerb(RegistryPath, isPending: false);
                        }
                        else if (HandlerClsid is not null && Guid.TryParse(HandlerClsid, out var clsid))
                        {
                            PackagedComHelper.SetBlockedClsid(
                                clsid,
                                PackagedComHelper.BlockedClsidType.CurrentUser,
                                !newValue,
                                isPending: false);
                        }

                        IsPending = false;
                        return true;
                    },
                    onPropertyChanged: (_, _) => OnPropertyChanged(nameof(StatusColor)),
                    notifyWhenNotChanged: true,
                    asyncNotifyWhenNotChanged: true);
            }
        }

        public string StatusColor => isPending ? "#FFC107" : (enabled ? "#4CAF50" : "#F44336");

        private string? _filePath;
        private bool _filePathResolved;

        private string? ResolveFilePath()
        {
            if (IsVerb)
                return ShellMenuScanner.ResolveVerbFilePath(RegistryPath);
            if (HandlerClsid is not null && Guid.TryParse(HandlerClsid, out var clsid))
                return ShellMenuScanner.ResolveClsidFilePath(clsid);
            return null;
        }

        public string? FilePath
        {
            get
            {
                if (!_filePathResolved)
                {
                    _filePath = ResolveFilePath();
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
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        });
    }
}
