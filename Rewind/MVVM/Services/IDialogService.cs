namespace Rewind.MVVM.Services
{
    /// <summary>
    /// Абстракция над диалогами/сообщениями, чтобы VM не тянули MessageBox напрямую.
    /// </summary>
    public interface IDialogService
    {
        void Info(string message, string title = "Rewind");
        void Error(string message, string title = "Ошибка");
        bool Confirm(string message, string title = "Подтвердите");
        string? PickImage();
        string? PickAudio();
    }
}
