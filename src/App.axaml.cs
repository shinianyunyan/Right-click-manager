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

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            // Start intercepting and blocking new clsids
            Helpers.ContextMenuInterceptor.Instance.Start();

            // When intercepted, refresh the apps list
            Helpers.ContextMenuInterceptor.Instance.OnItemIntercepted += (s, newClsid) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (desktop.MainWindow.DataContext is ViewModels.MainWindowViewModel vm)
                    {
                        vm.SearchCommand.Execute(vm.SearchingText);
                    }
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
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
        }
    }
}