using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for DrumNoteConfig.xaml
    /// </summary>
    public partial class DrumNoteEditor : UserControl
    {
        public DrumNoteEditor()
        {
            InitializeComponent();
        }

        private DrumNote Vm => (DrumNote)this.DataContext;

        private void PaintSurface(SKPaintSurfaceEventArgs e, string svgString)
        {
            using var svg = new SKSvg();
            svg.FromSvg(svgString);

            if (svg.Picture is null)
            {
                return;
            }

            var svgSize = svg.Picture.CullRect.Size;
            var bounds = e.Surface.Canvas.DeviceClipBounds;
            var fit = e.Info.Rect.AspectFit(svgSize.ToSizeI());
            e.Surface.Canvas.Scale(fit.Width / svg.Picture.CullRect.Size.Width);
            e.Surface.Canvas.Translate(new SKPoint(0, (bounds.Height - fit.Height)));
            e.Surface.Canvas.DrawPicture(svg.Picture);
        }

        private void PaintBeforeHitSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.Vm.BeforeHitSvg is not string svgString)
            {
                return;
            }

            this.PaintSurface(e, svgString);
        }

        private void PaintAfterHitSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.Vm.AfterHitSvg is not string svgString)
            {
                return;
            }

            this.PaintSurface(e, svgString);
        }

        private void ClickPickImage(object sender, RoutedEventArgs e)
        {
            
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private string? GetSvgString()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "svg|*.svg",
            };

            if (dlg.ShowDialog() == true)
            {
                return File.ReadAllText(dlg.FileName);
            }

            return null;
        }

        private void PickBeforeHitImage(object sender, MouseButtonEventArgs e)
        {
            if (this.GetSvgString() is string svgString)
            {
                this.Vm.BeforeHitSvg = svgString;
                this.BeforeHitElement.InvalidateVisual();
            }
        }

        private void PickAfterHitImage(object sender, MouseButtonEventArgs e)
        {
            if (this.GetSvgString() is string svgString)
            {
                this.Vm.AfterHitSvg = svgString;
                this.AfterHitElement.InvalidateVisual();
            }
        }
    }
}
