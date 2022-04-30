using ColorPicker.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = MainViewModel.Instance;

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

        private void ClickOpenProject(object sender, RoutedEventArgs e)
        {
            this.OpenFile("neomam|*.neomam");
        }

        private void ClickFileOpen(object sender, RoutedEventArgs e)
        {
            this.OpenFile("midi|*.mid");
        }

        private bool SaveQuery()
        {
            if (!this._vm.Dirty)
            {
                return true;
            }

            var result = MessageBox.Show(
                    "Save changes?",
                    "neomam",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );
            switch (result)
            {
                case MessageBoxResult.Yes:
                    this.SaveProject();
                    return true;
                case MessageBoxResult.No:
                    return true;
                default:
                case MessageBoxResult.Cancel:
                    return false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!this.SaveQuery())
            {
                e.Cancel = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl) && this._vm.MidiFile != null)
            {
                this.SaveProject();
            }
        }

        private void OpenFile(string filter)
        {
            if (!this.SaveQuery())
            {
                return;
            }

            //load the midi file.
            var dlg = new OpenFileDialog { Filter = filter, };

            if (dlg.ShowDialog() == true)
            {
                if (Path.GetExtension(dlg.FileName).StartsWith(".mid", StringComparison.CurrentCultureIgnoreCase))
                {
                    this._vm.InitFromMidi(MidiFile.Read(dlg.FileName));
                }
                else
                {
                    this._vm.InitFromProject(dlg.FileName);
                }

                this.outputElement.InvalidateVisual();
            }
        }

        private void OutputElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this._vm.DrawMidi(e.Surface.Canvas);
        }

        private void SaveProject()
        {
            if (this._vm.ProjectPath is string projectPath && projectPath.Length > 0)
            {
                this._vm.SaveProject(this._vm.ProjectPath);
            }
            else
            {
                this.SaveProjectAs();
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            this.SaveProject();
        }

        private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            this.SaveProjectAs();
        }

        private void SaveProjectAs()
        {
            var dlg = new SaveFileDialog { Filter = "neomam|*.neomam" };
            if (dlg.ShowDialog(this) == true)
            {
                this._vm.SaveProject(dlg.FileName);
            }
        }

        private Action<Color>? _applyColor;

        internal void ShowColorPicker(Action<Color> applyColor, Color? c = null)
        {
            this._applyColor = applyColor;
            if (c != null)
            {
                this.GlobalColorPicker.SelectedColor = c.Value;
            }

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
            }, this._vm.BackColor);
        }

        private void Render(object sender, RoutedEventArgs e)
        {
            var window = new RenderWindow(this._vm.CreateRenderJob())
            {
                Owner = this,
            };
            window.Show();
        }
    }
}
