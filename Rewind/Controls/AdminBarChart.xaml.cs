using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Rewind.Controls
{
    public class ChartDataPoint
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
    }

    public partial class AdminBarChart : UserControl
    {
        private List<ChartDataPoint> _data = new();

        public AdminBarChart()
        {
            InitializeComponent();
        }

        public void SetData(List<ChartDataPoint> data)
        {
            _data = data ?? new List<ChartDataPoint>();
            DrawChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();

            double canvasW = ChartCanvas.ActualWidth;
            double canvasH = ChartCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return;

            if (_data == null || _data.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text = "Нет данных",
                    FontSize = 13,
                    Foreground = GetBrush("TextMuted", Color.FromRgb(187, 187, 181))
                };
                Canvas.SetLeft(empty, canvasW / 2 - 36);
                Canvas.SetTop(empty, canvasH / 2 - 10);
                ChartCanvas.Children.Add(empty);
                return;
            }

            const double labelH = 28;
            double chartH = canvasH - labelH;
            double maxVal = _data.Max(d => d.Value);
            if (maxVal == 0) maxVal = 1;

            int count = _data.Count;
            double gap = Math.Max(4, Math.Min(12, canvasW / (count * 3)));
            double barW = Math.Max(2, (canvasW - gap * (count + 1)) / count);

            var accentBrush = GetBrush("AccentColor", Color.FromRgb(42, 232, 118));
            var trackBrush = GetBrush("BgCardHover", Color.FromRgb(240, 239, 235));
            var textBrush = GetBrush("TextSecondary", Color.FromRgb(136, 136, 128));
            var valueBrush = GetBrush("TextPrimary", Color.FromRgb(26, 26, 24));
            var borderBrush = GetBrush("BorderColor", Color.FromRgb(235, 235, 231));

            // Baseline
            var line = new Line
            {
                X1 = 0, Y1 = chartH,
                X2 = canvasW, Y2 = chartH,
                Stroke = borderBrush,
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(line);

            for (int i = 0; i < count; i++)
            {
                double x = gap + i * (barW + gap);
                double barHeight = (_data[i].Value / maxVal) * (chartH - 20);
                double y = chartH - barHeight;

                // Background track
                var bgRect = new Rectangle
                {
                    Width = barW,
                    Height = Math.Max(1, chartH - 2),
                    Fill = trackBrush,
                    RadiusX = 5,
                    RadiusY = 5,
                    Opacity = 0.5
                };
                Canvas.SetLeft(bgRect, x);
                Canvas.SetTop(bgRect, 0);
                ChartCanvas.Children.Add(bgRect);

                // Actual bar
                if (barHeight > 0)
                {
                    var rect = new Rectangle
                    {
                        Width = barW,
                        Height = Math.Max(2, barHeight),
                        Fill = accentBrush,
                        RadiusX = 5,
                        RadiusY = 5
                    };
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    ChartCanvas.Children.Add(rect);
                }

                // Value label
                if (_data[i].Value > 0)
                {
                    var tb = new TextBlock
                    {
                        Text = _data[i].Value.ToString("0"),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = valueBrush
                    };
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(tb, x + barW / 2 - tb.DesiredSize.Width / 2);
                    Canvas.SetTop(tb, Math.Max(2, y - 16));
                    ChartCanvas.Children.Add(tb);
                }

                // X label
                var lb = new TextBlock
                {
                    Text = _data[i].Label,
                    FontSize = 10,
                    Foreground = textBrush,
                    Width = barW + gap,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(lb, x - gap / 2);
                Canvas.SetTop(lb, chartH + 5);
                ChartCanvas.Children.Add(lb);
            }
        }

        private Brush GetBrush(string key, Color fallback)
        {
            return (Application.Current.TryFindResource(key) as Brush)
                ?? new SolidColorBrush(fallback);
        }
    }
}
