using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RightClickManager.Base
{
    public partial class ObservableObject : INotifyPropertyChanged
    {
        protected virtual bool SetProperty<T>(ref T field, T value, OnPropertyChangingDelegate<T>? onPropertyChanging = null, OnPropertyChangedDelegate<T>? onPropertyChanged = null, [CallerMemberName] string? propertyName = null, bool notifyWhenNotChanged = false, bool asyncNotifyWhenNotChanged = false)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                if (onPropertyChanging != null && !onPropertyChanging.Invoke(field, value))
                {
                    if (notifyWhenNotChanged)
                    {
                        Notify(asyncNotifyWhenNotChanged, propertyName);
                    }
                    return false;
                }
                var oldValue = field;
                field = value;
                Notify(false, propertyName);
                onPropertyChanged?.Invoke(oldValue, value);
                return true;
            }
            return false;

            async void Notify(bool _async, string? _propertyName)
            {
                try
                {
                    if (_async)
                    {
                        Helpers.Logger.Info($"Notify async begin: {_propertyName}");
                        Dispatcher.UIThread.VerifyAccess();
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Helpers.Logger.Info($"Notify async firing: {_propertyName}");
                            OnPropertyChanged(_propertyName);
                        }, DispatcherPriority.Loaded);
                        Helpers.Logger.Info($"Notify async end: {_propertyName}");
                    }
                    else
                    {
                        OnPropertyChanged(_propertyName);
                    }
                }
                catch (System.Exception ex)
                {
                    Helpers.Logger.Error($"Notify crash: {_propertyName}", ex.ToString());
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected delegate bool OnPropertyChangingDelegate<T>(T oldValue, T newValue);
        protected delegate void OnPropertyChangedDelegate<T>(T oldValue, T newValue);

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
