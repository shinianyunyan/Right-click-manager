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
                var handler = PropertyChanged;
                if (handler != null)
                {
                    if (_async)
                    {
                        Dispatcher.UIThread.VerifyAccess();
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
                    }
                    else
                    {
                        OnPropertyChanged(_propertyName);
                    }
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
