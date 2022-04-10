
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

namespace NeomamWpf
{
    public class Config
    {
        public string? BackColor { get; set; }
        public double TicksPerVertical { get; set; }
        public double CurrentTick { get; set; }
        public List<ChannelConfig>? Channels { get; set; }
    }

    public class ChannelConfig
    {
        public string? OnColor { get; set; }
        public string? OffColor { get; set; }
        public bool Visible { get; set; }
        public int ChannelNumber { get; set; }
        public double TicksPerVertical { get; set; }
    }

    public class ChannelConfigViewModel : ViewModelBase
    {
        private MainViewModel _parent;

        public ChannelConfigViewModel(MainViewModel parent, ChannelConfig dto)
        {
            Dto = dto;
            _parent = parent;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            this._parent.NotifyConfigChanged();
        }

        public Color OnColor
        {
            get => this.Dto.OnColor is string onColor
                ? (Color)ColorConverter.ConvertFromString(onColor)
                : Color.FromRgb(255, 255, 255);
            set => this.Set(() => this.OnColor, () => this.Dto.OnColor = value.ToString());
        }

        public Color OffColor
        {
            get => this.Dto.OffColor is string OffColor
                ? (Color)ColorConverter.ConvertFromString(OffColor)
                : Color.FromRgb(255, 255, 255);
            set => this.Set(() => this.OffColor, () => this.Dto.OffColor = value.ToString());
        }

        public int ChannelNumber => this.Dto.ChannelNumber;

        public bool Visible
        {
            get => this.Dto.Visible;
            set => this.Set(() => this.Dto.Visible, () => this.Dto.Visible = value);
        }

        public ChannelConfig Dto { get; }
    }
}
