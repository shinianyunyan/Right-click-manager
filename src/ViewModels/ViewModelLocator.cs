using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightClickManager.Base;

namespace RightClickManager.ViewModels
{
    public class ViewModelLocator
    {
        private MainWindowViewModel? mainWindowViewModel;
        public MainWindowViewModel MainWindowViewModel => mainWindowViewModel ??= new MainWindowViewModel();

        public static ViewModelLocator Instance => (ViewModelLocator)App.Current!.Resources["Locator"]!;

        public RelayCommand ShowWindowCommand => new RelayCommand(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Show();
                desktop.MainWindow?.Activate();
            }
        });

        public RelayCommand ExitAppCommand => new RelayCommand(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow.IsRealExit = true;
                desktop.Shutdown();
            }
        });
    }
}
