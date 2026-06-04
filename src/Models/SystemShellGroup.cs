using RightClickManager.Base;
using RightClickManager.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RightClickManager.Models
{
    public partial class SystemShellGroup : ObservableObject
    {
        private bool _isItemsExpanded;

        public SystemShellGroup(string category, IReadOnlyList<SystemShellItem> items)
        {
            Category = category;
            DisplayName = category switch
            {
                "*" => "Files (*)",
                "Directory" => "Directory",
                "Drive" => "Drive",
                "Folder" => "Folder",
                "AllFilesystemObjects" => "All Filesystem Objects",
                "DesktopBackground" => "Desktop Background",
                "LibraryFolder" => "Library Folder",
                _ => category
            };
            Items = items;

            foreach (var item in Items)
                item.PropertyChanged += OnChildItemPropertyChanged;
            RecalculateSelectAllState();
        }

        public string Category { get; }
        public string DisplayName { get; }
        public IReadOnlyList<SystemShellItem> Items { get; }

        public bool HasMoreItems => Items.Count > 3;

        public bool IsItemsExpanded
        {
            get => _isItemsExpanded;
            set => SetProperty(ref _isItemsExpanded, value, notifyWhenNotChanged: true);
        }

        public IReadOnlyList<SystemShellItem> VisibleItems =>
            IsItemsExpanded ? Items : Items.Take(3).ToList();

        public RelayCommand ToggleItemsExpandCommand => new RelayCommand(() =>
        {
            IsItemsExpanded = !IsItemsExpanded;
            OnPropertyChanged(nameof(VisibleItems));
        });

        public RelayCommand EnableAllCommand => new RelayCommand(() =>
        {
            foreach (var item in Items)
                if (item.CanModify) item.Enabled = true;
        });

        public RelayCommand DisableAllCommand => new RelayCommand(() =>
        {
            foreach (var item in Items)
            {
                if (!item.CanModify) continue;
                if (item.IsPending)
                {
                    // Clear pending marker in registry while keeping block
                    if (item.IsVerb)
                        ShellMenuScanner.DeleteVerbPendingMarker(item.RegistryPath);
                    else if (item.HandlerClsid is not null && Guid.TryParse(item.HandlerClsid, out var clsid))
                        PackagedComHelper.SetBlockedClsid(clsid, PackagedComHelper.BlockedClsidType.CurrentUser, blocked: true, isPending: false);
                    item.IsPending = false;
                }
                item.Enabled = false;
            }
        });

        private bool _isUpdatingSelection;
        private bool? _selectAllState;

        public bool? SelectAllState
        {
            get => _selectAllState;
            private set => SetProperty(ref _selectAllState, value);
        }

        public RelayCommand SelectAllCommand => new RelayCommand(() =>
        {
            _isUpdatingSelection = true;
            bool newState = _selectAllState != true;
            foreach (var item in Items)
                if (item.CanModify) item.IsSelected = newState;
            _isUpdatingSelection = false;
            RecalculateSelectAllState();
        });

        public RelayCommand EnableSelectedCommand => new RelayCommand(() =>
        {
            foreach (var item in Items)
                if (item.IsSelected && item.CanModify) item.Enabled = true;
        });

        public RelayCommand DisableSelectedCommand => new RelayCommand(() =>
        {
            foreach (var item in Items)
            {
                if (!item.IsSelected || !item.CanModify) continue;
                if (item.IsPending)
                {
                    if (item.IsVerb)
                        ShellMenuScanner.DeleteVerbPendingMarker(item.RegistryPath);
                    else if (item.HandlerClsid is not null && Guid.TryParse(item.HandlerClsid, out var clsid))
                        PackagedComHelper.SetBlockedClsid(clsid, PackagedComHelper.BlockedClsidType.CurrentUser, blocked: true, isPending: false);
                    item.IsPending = false;
                }
                item.Enabled = false;
            }
        });

        private void RecalculateSelectAllState()
        {
            if (_isUpdatingSelection) return;
            if (Items.Count == 0) { SelectAllState = false; return; }
            bool hasSel = false, hasUnsel = false;
            foreach (var item in Items)
            {
                if (!item.CanModify) continue;
                if (item.IsSelected) hasSel = true; else hasUnsel = true;
            }
            if (hasSel && hasUnsel) SelectAllState = null;
            else if (hasSel) SelectAllState = true;
            else SelectAllState = false;
        }

        private void OnChildItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemShellItem.IsSelected))
                RecalculateSelectAllState();
        }
    }
}
