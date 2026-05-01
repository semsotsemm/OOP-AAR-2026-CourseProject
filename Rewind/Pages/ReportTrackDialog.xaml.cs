using Rewind.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Rewind.Pages
{
    public partial class ReportTrackDialog : Window
    {
        private readonly int _trackId;

        public ReportTrackDialog(int trackId, string trackName)
        {
            InitializeComponent();
            _trackId = trackId;
            TrackNameText.Text = $"«{trackName}»";
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (Session.UserId <= 0)
            {
                MessageBox.Show("Необходимо войти в аккаунт.", "Rewind");
                return;
            }

            var reasonItem = ReasonCombo.SelectedItem as ComboBoxItem;
            string reason = reasonItem?.Content?.ToString() ?? "Другое";
            string detail = DetailBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(detail))
                reason += $": {detail}";

            TrackReportService.CreateReport(_trackId, Session.UserId, reason);
            MessageBox.Show("Жалоба отправлена администратору.", "Rewind",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
