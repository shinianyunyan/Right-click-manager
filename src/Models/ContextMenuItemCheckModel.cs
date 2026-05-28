using RightClickManager.Base;
using RightClickManager.Helpers;
using System;

namespace RightClickManager.Models
{
    public partial class ContextMenuItemCheckModel : ObservableObject
    {
        private bool enabled;

        public ContextMenuItemCheckModel(ContextMenuItem contextMenuItem, bool enabled, bool canModify, bool isPending = false)
        {
            ContextMenuItem = contextMenuItem;
            CanModify = canModify;
            this.enabled = enabled;
            this.isPending = isPending;
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
                    notifyWhenNotChanged: true,
                    asyncNotifyWhenNotChanged: true);
            }
        }

        public string DisplayName
        {
            get
            {
                var title = ContextMenuItem.Title;
                if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(ContextMenuItem.Id)) title = $"[{ContextMenuItem.Id}]";

                if (!string.IsNullOrEmpty(ContextMenuItem.Type) && !string.IsNullOrEmpty(title))
                {
                    return $"[{ContextMenuItem.Type}] {title}\n{ContextMenuItem.Clsid:B}";
                }
                else if (!string.IsNullOrEmpty(ContextMenuItem.Type))
                {
                    return $"[{ContextMenuItem.Type}]\n{ContextMenuItem.Clsid:B}";
                }
                else if (!string.IsNullOrEmpty(title))
                {
                    return $"{title}\n{ContextMenuItem.Clsid:B}";
                }

                return $"{ContextMenuItem.Clsid:B}";
            }
        }

        private string? _filePath;
        private bool _filePathResolved;

        public string? FilePath
        {
            get
            {
                if (!_filePathResolved)
                {
                    _filePath = ShellMenuScanner.ResolveClsidFilePath(ContextMenuItem.Clsid);
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
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        });
    }
}
