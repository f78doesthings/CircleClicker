using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CircleClicker.Utils
{
    /// <inheritdoc cref="INotifyPropertyChanged" />
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event; first with the name of the caller, then with each property name passed.
        /// </summary>
        protected void OnPropertyChanged(
            IEnumerable<string>? propNames = null,
            [CallerMemberName] string? propName = null
        )
        {
            propNames ??= [];
            if (propName != null)
            {
                propNames = [propName, .. propNames];
            }

            InvokePropertyChanged(propNames.ToArray());
        }

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event with each property name passed.
        /// </summary>
        internal virtual void InvokePropertyChanged(params string[] propNames)
        {
            if (Main.Instance.IsAutosavingEnabled == false)
            {
                return;
            }

            foreach (string prop in propNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
