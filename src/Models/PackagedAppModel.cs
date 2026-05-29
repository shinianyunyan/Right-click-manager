using Avalonia.Media.Imaging;
using RightClickManager.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RightClickManager.Models
{
    public partial class PackagedAppModel : Base.ObservableObject
    {
        private bool _isItemsExpanded;
        private bool _isUpdatingSelection;
        private bool? _selectAllState;

        public PackagedAppModel(
            AppInfo appInfo,
            PackageInfo packageInfo,
            IEnumerable<ContextMenuItem> items,
            Dictionary<Guid, PackagedComHelper.BlockedClsid> blockedClsids,
            Dictionary<Guid, string>? clsidDllPaths = null)
        {
            AppInfo = appInfo;
            PackageInfo = packageInfo;
            ContextMenuItems = items.Select(c =>
            {
                string? dllPath = null;
                clsidDllPaths?.TryGetValue(c.Clsid, out dllPath);
                if (blockedClsids.TryGetValue(c.Clsid, out var blockedClsid))
                {
                    return new ContextMenuItemCheckModel(c, false, blockedClsid.Type != PackagedComHelper.BlockedClsidType.LocalMachine, blockedClsid.IsPending, dllPath);
                }
                return new ContextMenuItemCheckModel(c, true, true, false, dllPath);
            }).ToArray();

            foreach (var item in ContextMenuItems)
                item.PropertyChanged += OnChildItemPropertyChanged;
            RecalculateSelectAllState();

            if (string.IsNullOrEmpty(AppInfo.DisplayName))
            {
                DisplayName = $"{PackageInfo.PackageFamilyName}";
            }
            else
            {
                DisplayName = $"{AppInfo.DisplayName}\n{PackageInfo.PackageFamilyName}";
            }
        }

        public AppInfo AppInfo { get; }

        public PackageInfo PackageInfo { get; }

        public IReadOnlyList<ContextMenuItemCheckModel> ContextMenuItems { get; }

        public string DisplayName { get; }

        private Bitmap? _icon;
        private bool _iconResolved;

        public Bitmap? Icon
        {
            get
            {
                if (!_iconResolved)
                {
                    _iconResolved = true;
                    if (!string.IsNullOrEmpty(AppInfo.IconPath))
                    {
                        var iconPath = System.IO.Path.Combine(PackageInfo.PackageInstallLocation, AppInfo.IconPath);
                        if (System.IO.File.Exists(iconPath))
                            _icon = new Bitmap(iconPath);
                    }
                }
                return _icon;
            }
        }

        public bool HasMoreItems => ContextMenuItems.Count > 3;

        public bool IsItemsExpanded
        {
            get => _isItemsExpanded;
            set => SetProperty(ref _isItemsExpanded, value, notifyWhenNotChanged: true);
        }

        public IReadOnlyList<ContextMenuItemCheckModel> VisibleContextMenuItems =>
            IsItemsExpanded ? ContextMenuItems : ContextMenuItems.Take(3).ToList();

        public Base.RelayCommand ToggleItemsExpandCommand => new Base.RelayCommand(() =>
        {
            IsItemsExpanded = !IsItemsExpanded;
            OnPropertyChanged(nameof(VisibleContextMenuItems));
        });

        public bool? SelectAllState
        {
            get => _selectAllState;
            private set => SetProperty(ref _selectAllState, value);
        }

        public Base.RelayCommand SelectAllCommand => new Base.RelayCommand(() =>
        {
            _isUpdatingSelection = true;
            bool newState = _selectAllState != true;
            foreach (var item in ContextMenuItems)
                if (item.CanModify) item.IsSelected = newState;
            _isUpdatingSelection = false;
            RecalculateSelectAllState();
        });

        public Base.RelayCommand EnableSelectedCommand => new Base.RelayCommand(() =>
        {
            foreach (var item in ContextMenuItems)
                if (item.IsSelected && item.CanModify) item.Enabled = true;
        });

        public Base.RelayCommand DisableSelectedCommand => new Base.RelayCommand(() =>
        {
            foreach (var item in ContextMenuItems)
                if (item.IsSelected && item.CanModify) item.Enabled = false;
        });

        public Base.RelayCommand DeleteSelectedCommand => new Base.RelayCommand(() =>
        {
            foreach (var item in ContextMenuItems)
            {
                if (item.IsSelected && item.CanModify)
                {
                    PackagedComHelper.SetBlockedClsid(
                        item.ContextMenuItem.Clsid,
                        PackagedComHelper.BlockedClsidType.CurrentUser,
                        blocked: false);
                    item.Enabled = true;
                }
            }
        });

        private void RecalculateSelectAllState()
        {
            if (_isUpdatingSelection) return;
            if (ContextMenuItems.Count == 0) { SelectAllState = false; return; }
            bool hasSelected = false, hasUnselected = false;
            foreach (var item in ContextMenuItems)
            {
                if (!item.CanModify) continue;
                if (item.IsSelected) hasSelected = true;
                else hasUnselected = true;
            }
            if (hasSelected && hasUnselected) SelectAllState = null;
            else if (hasSelected) SelectAllState = true;
            else SelectAllState = false;
        }

        private void OnChildItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContextMenuItemCheckModel.IsSelected))
                RecalculateSelectAllState();
        }
    }
}
