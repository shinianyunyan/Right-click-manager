using RightClickManager.Base;
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
                if (item.CanModify) item.Enabled = false;
        });
    }
}
