namespace Rewind.MVVM.Core
{
    /// <summary>
    /// Алиас ViewModelBase для сущностных моделей, которые не являются ViewModel-ами
    /// конкретной страницы, но всё равно нуждаются в INotifyPropertyChanged
    /// (например, карточка трека в списке).
    /// </summary>
    public abstract class ObservableObject : ViewModelBase { }
}
