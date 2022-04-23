using Microsoft.Win32;
using NeomamWpf;
using SkiaSharp.Views.Desktop;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for RenderWindow.xaml
    /// </summary>
    public partial class RenderWindow : Window
    {
        private RenderViewModel _vm;

        public RenderWindow(RenderViewModel vm)
        {
            this.DataContext = this._vm = vm;
            this._vm.PropertyChanged += this.VmPropChanged;
            InitializeComponent();
        }

        private void VmPropChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (new[]
            {
                nameof(this._vm.CurrentMicrosecond),
                nameof(this._vm.DrawNoteOn),
                nameof(this._vm.DrawNoteOff),
            }.Contains(e.PropertyName))
            {
                this._outputControl.InvalidateVisual();
            }
        }

        private void SKElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this._vm.Draw(e.Surface.Canvas, _vm.CurrentMicrosecond);
        }

        private void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                this._vm.StartRender(dlg.FileName);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this._vm.Cancel();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this._vm.Cancel();
        }
    }
}
