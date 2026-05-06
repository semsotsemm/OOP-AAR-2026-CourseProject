using Microsoft.Win32;
using System.Windows;

namespace Rewind.MVVM.Services
{
    /// <summary>Реализация диалогов поверх MessageBox / OpenFileDialog.</summary>
    public sealed class DialogService : IDialogService
    {
        public void Info(string message, string title = "Rewind")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void Error(string message, string title = "Ошибка")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public bool Confirm(string message, string title = "Подтвердите")
            => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
               == MessageBoxResult.Yes;

        public string? PickImage()
        {
            var dlg = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp" };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string? PickAudio()
        {
            var dlg = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav" };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
