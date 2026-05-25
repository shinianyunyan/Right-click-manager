using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace RightClickManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyLanguage(ViewModels.ViewModelLocator.Instance.MainWindowViewModel.CurrentLanguage);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    public static void ApplyLanguage(string lang)
    {
        var targetLang = lang;
        if (targetLang == "system")
        {
            ushort langId = GetUserDefaultUILanguage();
            if ((langId & 0x00FF) == 0x04)
            {
                targetLang = "zh-CN";
            }
            else
            {
                targetLang = "en-US";
            }
        }

        string dictSource = "avares://RightClickManager/Lang/en-US.axaml";
        if (targetLang.StartsWith("zh", System.StringComparison.OrdinalIgnoreCase))
        {
            dictSource = "avares://RightClickManager/Lang/zh-CN.axaml";
        }

        var app = Avalonia.Application.Current;
        if (app != null)
        {
            app.Resources.MergedDictionaries.Clear();
            var newDict = new Avalonia.Markup.Xaml.Styling.ResourceInclude(new System.Uri("avares://RightClickManager/App.axaml"))
            {
                Source = new System.Uri(dictSource)
            };
            app.Resources.MergedDictionaries.Add(newDict);
        }
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
                    try
                    {
                        object? titleObj = null, msgObj = null;
                        Avalonia.Application.Current?.Resources.TryGetResource("LangText_InterceptedTitle", null, out titleObj);
                        Avalonia.Application.Current?.Resources.TryGetResource("LangText_InterceptedMsg", null, out msgObj);
                        string title = titleObj as string ?? "Interception Alert";
                        string msg = msgObj as string ?? "A newly added context menu item was detected and intercepted.";

                        var toastXml = Windows.UI.Notifications.ToastNotificationManager.GetTemplateContent(Windows.UI.Notifications.ToastTemplateType.ToastText02);
                        var textElements = toastXml.GetElementsByTagName("text");
                        textElements[0].AppendChild(toastXml.CreateTextNode(title));
                        textElements[1].AppendChild(toastXml.CreateTextNode(msg));
                        var toast = new Windows.UI.Notifications.ToastNotification(toastXml);
                        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier("RightClickManager").Show(toast);
                    }
                    catch { }

                    var vm = ViewModels.ViewModelLocator.Instance.MainWindowViewModel;
                    vm.SearchCommand.Execute(vm.SearchingText);
                });
            };

            // When a new shell verb is intercepted, notify and refresh
            Helpers.ContextMenuInterceptor.Instance.OnVerbIntercepted += (s, verbPath) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        object? titleObj = null, msgObj = null;
                        Avalonia.Application.Current?.Resources.TryGetResource("LangText_InterceptedTitle", null, out titleObj);
                        Avalonia.Application.Current?.Resources.TryGetResource("LangText_InterceptedVerbMsg", null, out msgObj);
                        string title = titleObj as string ?? "Interception Alert";
                        string msg = msgObj as string ?? "A new shell verb was detected and intercepted.";

                        var toastXml = Windows.UI.Notifications.ToastNotificationManager.GetTemplateContent(Windows.UI.Notifications.ToastTemplateType.ToastText02);
                        var textElements = toastXml.GetElementsByTagName("text");
                        textElements[0].AppendChild(toastXml.CreateTextNode(title));
                        textElements[1].AppendChild(toastXml.CreateTextNode(msg + "\n" + verbPath));
                        var toast = new Windows.UI.Notifications.ToastNotification(toastXml);
                        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier("RightClickManager").Show(toast);
                    }
                    catch { }

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


