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

        private bool _showError;
        public bool ShowError
        {
            get => this._showError;
            set => this.Set(ref this._showError, value);
        }

        private string _error = "";
        public string Error
        {
            get => this._error;
            set => this.Set(ref this._error, value);
        }

        private void SetError(string error)
        {
            this.Error = error;
            this.ShowError = !string.IsNullOrEmpty(error);
        }

        public Color BackColor
        {
            get => this._config.BackColor is string backColor
                ? (Color)ColorConverter.ConvertFromString(backColor)
                : Color.FromRgb(0, 0, 0);
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
                BackColor = "#FF000000",
                Tracks = file
                    .GetTrackChunks()
                    .Select(ch => ch.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .Select(name => new TrackConfig(name ?? throw new InvalidOperationException()))
                    .ToList(),
            };

            if (!this._config.Tracks.Any())
            {
                this.SetError("No named channels in midi file.");
            }

            this.Channels.AddRange(this._config.Tracks.Select(ch => new ChannelConfigViewModel(this, ch)));
        }

        public void DrawMidi(SKCanvas canvas)
        {
            if (this._midiFile is null || !this._midiFile.GetNotes().Any())
            {
                return;
            }

            var bounds = canvas.DeviceClipBounds;

            var allNotes = this._midiFile.GetNotes();

            var maxNote = (int)allNotes.Max(n => n.NoteNumber) + 1; //leave single note border
            var minNote = (int)allNotes.Min(n => n.NoteNumber) - 1; //leave single note border
            var verticalNotes = maxNote - minNote - 1;

            var noteHeight = bounds.Height / verticalNotes;
            var ticksPerPixel = this.TicksPerVertical / bounds.Height;
            var centerX = bounds.Width / 2;

            canvas.DrawRect(bounds, new SKPaint { Color = this.BackColor.ToSkia() });
            double microsecond = this.CurrentMicrosecond;

            foreach (var track in this._midiFile.GetTrackChunks())
            {
                foreach (var note in track.GetNotes())
                {
                    var x1 = (note.Time - microsecond) / ticksPerPixel + centerX;
                    var x2 = (note.Time + note.Length - microsecond) / ticksPerPixel + centerX;
                    var y1 = (maxNote - note.NoteNumber) * noteHeight;
                    var y2 = y1 + noteHeight;

                    if (x1 < bounds.Width)
                    {
                        bool noteIsOn = note.Time <= microsecond && (note.Time + note.Length) >= microsecond;
                        var onColor = new SKColor(100, 100, 0xFF);
                        var offColor = new SKColor(0, 0, 0xFF);

                        if (this._config.Tracks?.FirstOrDefault(x => x.TrackName == track.GetName())
                            is TrackConfig conf)
                        {
                            if (!conf.Visible)
                            {
                                continue;
                            }

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
        }

        internal void NotifyConfigChanged()
        {
            this.Redraw?.Invoke();
        }
    }
}
