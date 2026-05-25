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
            set => SetProperty(ref isPending, value);
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
                    notifyWhenNotChanged: true,
                    asyncNotifyWhenNotChanged: true);
            }
        }
    }
}
