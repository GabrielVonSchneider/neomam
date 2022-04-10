using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace NeomamWpf
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
        }

        public SKCanvas? DrawSurface { get; set; }

        private Config _config = new();

        public event Action? Redraw;
        private MidiFile? _midiFile;

        public double MaxMicrosecond => this._midiFile?.GetNotes().Max(n => n.Time) ?? 1;

        public double CurrentMicrosecond
        {
            get => this._config.CurrentTick;
            set => this.Set(() => this._config.CurrentTick, () => this._config.CurrentTick = value);
        }

        public double TicksPerVertical
        {
            get => this._config.TicksPerVertical;
            set => this.Set(() => this._config.TicksPerVertical, () => this._config.TicksPerVertical = value);
        }

        public Color BackColor
        {
            get => this._config.BackColor is string backColor
                ? (Color)ColorConverter.ConvertFromString(backColor)
                : Color.FromRgb(255, 255, 255);
            set => this.Set(() => this.BackColor, () => this._config.BackColor = value.ToString());
        }

        public ObservableCollection<ChannelConfigViewModel> Channels { get; }
            = new ObservableCollection<ChannelConfigViewModel>();

        public void SetMidiFile(MidiFile file)
        {
            this.Channels.Clear();
            this._midiFile = file;

            this._config = new()
            {
                BackColor = "#FFFFFFFF",
                Channels = file
                    .GetChannels()
                    .OrderBy(ch => ch)
                    .Select(ch => new ChannelConfig
                    {
                        ChannelNumber = ch,
                    }).ToList(),
            };

            this.Channels.AddRange(this._config.Channels.Select(ch => new ChannelConfigViewModel(this, ch)));
        }

        public void DrawMidi(SKCanvas canvas)
        {
            if (this._midiFile is null)
            {
                return;
            }

            var bounds = canvas.DeviceClipBounds;
            var notes = this._midiFile.GetNotes();

            var maxNote = (int)notes.Max(n => n.NoteNumber) + 1; //leave single note border
            var minNote = (int)notes.Min(n => n.NoteNumber) - 1; //leave single note border
            var verticalNotes = maxNote - minNote - 1;

            var noteHeight = bounds.Height / verticalNotes;
            var ticksPerPixel = this.TicksPerVertical / bounds.Height;
            var centerX = bounds.Width / 2;

            canvas.DrawRect(bounds, new SKPaint { Color = this.BackColor.ToSkia() });
            double microsecond = this.CurrentMicrosecond;

            foreach (var note in notes)
            {
                var x1 = (note.Time - microsecond) / ticksPerPixel + centerX;
                var x2 = (note.Time + note.Length - microsecond) / ticksPerPixel + centerX;
                var y1 = (maxNote - note.NoteNumber) * noteHeight;
                var y2 = y1 + noteHeight;

                if (x1 < bounds.Width)
                {
                    bool noteIsOn = note.Time <= microsecond && (note.Time + note.Length) >= microsecond;
                    var onColor = new SKColor(100, 100, 100);
                    var offColor = new SKColor(0, 0, 0);

                    if (this._config.Channels?.FirstOrDefault(x => x.ChannelNumber == note.Channel)
                        is ChannelConfig conf)
                    {
                        if (SKColor.TryParse(conf.OnColor, out var parsed))
                        {
                            onColor = parsed;
                        }
                        if (SKColor.TryParse(conf.OffColor, out parsed))
                        {
                            offColor = parsed;
                        }
                    }

                    var color = noteIsOn ? onColor : offColor;
                    canvas.DrawRect(
                            (float)x1,
                            y1,
                            (float)(x2 - x1),
                            noteHeight,
                            new SKPaint { Color = color, IsAntialias = true, }
                        );
                }
            }
        }

        internal void NotifyConfigChanged()
        {
            this.Redraw?.Invoke();
        }
    }
}
