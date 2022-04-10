using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MidiFile? _midiFile;

        private readonly MainViewModel _vm = new();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.Redraw += Vm_Redraw;
            _vm.PropertyChanged += this.VmPropChanged;
        }

        private void VmPropChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this._vm.BackColor))
            {
                this.BackColorPicker.Background = new SolidColorBrush(this._vm.BackColor);
            }

            this.outputElement.InvalidateVisual();
        }

        private void Vm_Redraw()
        {
            this.outputElement.InvalidateVisual();
        }

        private void ClickFileOpen(object sender, RoutedEventArgs e)
        {
            //load the midi file.
            var dlg = new OpenFileDialog
            {
                Filter = "midi|*.mid",
            };

            if (dlg.ShowDialog() == true)
            {
                this._midiFile = MidiFile.Read(dlg.FileName);
                this._vm.SetMidiFile(_midiFile);

                var tempoMap = this._midiFile.GetTempoMap();

                this.timelineSlider.Minimum = 0;
                Debug.WriteLine(this._midiFile.GetTempoMap().TimeDivision);
                var endOfFile = this._midiFile.GetTimedEvents().Max(ev => ev.Time);
                var fileLength = TimeConverter.ConvertTo<MetricTimeSpan>(endOfFile, tempoMap);
                this.timelineSlider.Minimum = 0;
                this.timelineSlider.Maximum = fileLength.TotalMicroseconds / 1000;

                this.outputElement.InvalidateVisual();
            }
        }

        class DrawMidiParams
        {
            public double TicksPerVertical { get; set; }
        }

        private void OutputElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this._vm.DrawMidi(e.Surface.Canvas);
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {

        }

        private Action<Color>? _applyColor;

        internal void ShowColorPicker(Action<Color> applyColor)
        {
            this._applyColor = applyColor;
            this.GlobalColorPopup.IsOpen = true;
        }

        private void GlobalColorPicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            var color = this.GlobalColorPicker.Color;
            var mediaColor = Color.FromArgb(
                    (byte)color.A,
                    (byte)color.RGB_R,
                    (byte)color.RGB_G,
                    (byte)color.RGB_B
                );
            this._applyColor?.Invoke(mediaColor);
        }

        private void BackColorPicker_Click(object sender, RoutedEventArgs e)
        {
            this.ShowColorPicker(c =>
            {
                this._vm.BackColor = c;
            });
        }
    }
}
