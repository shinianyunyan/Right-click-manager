using RightClickManager.Base;
using RightClickManager.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RightClickManager.Models
{
    public partial class SystemShellGroup : ObservableObject
    {
        private bool _isItemsExpanded;
        private bool _isUpdatingSelection;
        private bool? _selectAllState;

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
                if (item.IsSelected && item.CanModify) item.Enabled = false;
        });

        private void RecalculateSelectAllState()
        {
            if (_isUpdatingSelection) return;
            if (Items.Count == 0) { SelectAllState = false; return; }
            bool hasSelected = false, hasUnselected = false;
            foreach (var item in Items)
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
            if (e.PropertyName == nameof(SystemShellItem.IsSelected))
                RecalculateSelectAllState();
        }
    }
}
