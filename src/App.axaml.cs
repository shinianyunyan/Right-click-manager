using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace RightClickManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private MainWindow? _mainWindow;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            var isHidden = System.Linq.Enumerable.Contains(desktop.Args ?? System.Array.Empty<string>(), "--hidden");

            if (!isHidden)
            {
                _mainWindow = new MainWindow();
                desktop.MainWindow = _mainWindow;
            }

            // Start intercepting and blocking new clsids
            Helpers.ContextMenuInterceptor.Instance.Start();

            // When intercepted, refresh the apps list
            Helpers.ContextMenuInterceptor.Instance.OnItemIntercepted += (s, newClsid) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var vm = ViewModels.ViewModelLocator.Instance.MainWindowViewModel;
                    vm.SearchCommand.Execute(vm.SearchingText);
                });
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    private void TrayIcon_Clicked(object? sender, System.EventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowMainWindow_Click(object? sender, System.EventArgs e)
    {
        ShowMainWindow();
    }

    private void ExitApp_Click(object? sender, System.EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow.IsRealExit = true;
            desktop.Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                desktop.MainWindow = _mainWindow;
            }
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }
}