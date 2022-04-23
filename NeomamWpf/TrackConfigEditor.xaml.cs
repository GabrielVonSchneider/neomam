using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for ConfigEditor.xaml
    /// </summary>
    public partial class TrackConfigEditor : UserControl
    {
        TrackConfigViewModel? ViewModel => this.DataContext as TrackConfigViewModel;

        private MainWindow GetMainWindow()
        {
            return this.VisualParents().OfType<MainWindow>().FirstOrDefault()
                ?? throw new InvalidOperationException("no window found");
        }

        public TrackConfigEditor()
        {
            InitializeComponent();
            this.DataContextChanged += ConfigEditor_DataContextChanged;
        }

        private void ConfigEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.ViewModel != null)
            {
                this.OnButton.Background = new SolidColorBrush(this.ViewModel.OnColor);
                this.OffButton.Background = new SolidColorBrush(this.ViewModel.OffColor);
            }
        }

        private void OnButton_Click(object sender, RoutedEventArgs e)
        {
            this.GetMainWindow().ShowColorPicker(c =>
            {
                this.OnButton.Background = new SolidColorBrush(c);
                if (this.ViewModel != null)
                {
                    this.ViewModel.OnColor = c;
                }
            }, this.ViewModel?.OnColor);
        }

        private void OffButton_Click(object sender, RoutedEventArgs e)
        {
            this.GetMainWindow().ShowColorPicker(c =>
            {
                this.OffButton.Background = new SolidColorBrush(c);
                if (this.ViewModel != null)
                {
                    this.ViewModel.OffColor = c;
                }
            }, this.ViewModel?.OffColor);
        }

        private void EditDrumsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrumWindow
            { 
                DataContext = this.ViewModel?.Dto.Drums ?? throw new InvalidOperationException()
            };

            window.Show();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.Move;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(this, this.ViewModel, DragDropEffects.Move);
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(typeof(TrackConfigViewModel)) && this.ViewModel != null)
            {
                var source = (TrackConfigViewModel)e.Data.GetData(typeof(TrackConfigViewModel));
                bool moveBefore = (Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) > 0;
                source.MoveTo(this.ViewModel, moveBefore);
            }
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }
    }
}
