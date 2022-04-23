using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeomamWpf
{
    /// <summary>
    /// Interaction logic for ConfigEditor.xaml
    /// </summary>
    public partial class ChannelConfigEditor : UserControl
    {
        TrackConfigViewModel? ViewModel => this.DataContext as TrackConfigViewModel;

        private MainWindow GetMainWindow()
        {
            return this.VisualParents().OfType<MainWindow>().FirstOrDefault()
                ?? throw new InvalidOperationException("no window found");
        }

        public ChannelConfigEditor()
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
            });
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
            });
        }

        private void EditDrumsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrumWindow
            { 
                DataContext = this.ViewModel?.Dto.Drums ?? throw new InvalidOperationException()
            };

            window.Show();
        }
    }
}
