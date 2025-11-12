using System.Windows;
using System.Windows.Media;
using VoiceChanger.ViewModels;

namespace VoiceChanger
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        public MainWindow()
        {
            InitializeComponent();
            _vm = (MainViewModel)DataContext;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, System.EventArgs e)
        {
            if (_vm.TryGetWaveform(1024, out var samples))
            {
                DrawWaveform(samples);
            }
        }

        private void DrawWaveform(float[] samples)
        {
            var dc = WaveformCanvas;
            double w = dc.ActualWidth > 0 ? dc.ActualWidth : dc.Width;
            double h = dc.ActualHeight > 0 ? dc.ActualHeight : dc.Height;
            dc.Children.Clear();
            if (w <= 0 || h <= 0 || samples.Length == 0) return;
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(new System.Windows.Point(0, h / 2), false, false);
                for (int i = 0; i < samples.Length; i++)
                {
                    double x = i * (w / samples.Length);
                    double y = (0.5 - samples[i] * 0.45) * h;
                    ctx.LineTo(new System.Windows.Point(x, y), true, false);
                }
            }
            geom.Freeze();
            var path = new System.Windows.Shapes.Path
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 1,
                Data = geom
            };
            dc.Children.Add(path);
        }
    }
}

