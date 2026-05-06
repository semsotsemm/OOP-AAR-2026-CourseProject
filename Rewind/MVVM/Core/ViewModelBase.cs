using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rewind.MVVM.Core
{
    /// <summary>
    /// Базовый класс для всех ViewModel. Реализует INotifyPropertyChanged
    /// и утилиту SetProperty для сжатой записи сеттеров.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Генерирует уведомление PropertyChanged для указанного свойства
        /// (по умолчанию — имя вызывающего свойства).
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Присваивает новое значение полю и уведомляет об изменении свойства,
        /// если значение действительно поменялось. Возвращает true, если было изменение.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
