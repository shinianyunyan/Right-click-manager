using RightClickManager.Base;
using RightClickManager.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        PackagedComHelper.SetBlockedClsid(ContextMenuItem.Clsid, PackagedComHelper.BlockedClsidType.CurrentUser, !newValue, false), // User action means it's no longer pending
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
    }
}
