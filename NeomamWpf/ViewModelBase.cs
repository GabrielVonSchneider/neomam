using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeomamWpf
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise(string propName)
            => this.OnPropertyChanged(new PropertyChangedEventArgs(propName));

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            this.PropertyChanged?.Invoke(this, args);
        }

        protected void Set<T>(
                Func<T> getter,
                Action setter,
                [CallerMemberName] string? propname = null
            )
        {
            var oldValue = getter();
            setter();
            if (!EqualityComparer<T>.Default.Equals(oldValue, getter()))
            {
                Raise(propname ?? throw new ArgumentOutOfRangeException(nameof(propname)));
            }
        }

        protected void Set<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propname = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                Raise(propname ?? throw new ArgumentOutOfRangeException(nameof(propname)));
            }
        }
    }
}
