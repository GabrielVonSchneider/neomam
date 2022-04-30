
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using Svg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace NeomamWpf
{
    public class RenderConfig
    {
        public int HorizontalPixels { get; set; } = 1920;
        public int VerticalPixels { get; set; } = 1080;
        public double Framerate { get; set; } = 60;
        public string? FfmpegArgs { get; set; }

        public string GetResString()
        {
            return $"{this.HorizontalPixels}x{this.VerticalPixels}";
        }
    }

    public class Config
    {
        public string? BackColor { get; set; }
        public double TicksPerVertical { get; set; } = 5000;
        public double CurrentTick { get; set; }
        public List<TrackConfig>? Tracks { get; set; }
        public bool DrawNoteOn { get; set; } = true;
        public bool DrawNoteOff { get; set; } = true;

        public Color GetMediaBackColor() => this.BackColor is string backColor
            ? (Color)ColorConverter.ConvertFromString(backColor)
            : Color.FromRgb(0, 0, 0);

        public RenderConfig RenderConfig { get; set; } = new();
    }

    public class TrackConfig
    {
        public TrackConfig(string trackName)
        {
            this.TrackName = trackName;
        }

        public string? OnColor { get; set; }
        public string? OffColor { get; set; }
        public bool Visible { get; set; }
        public string TrackName { get; set; }
        public double? ZoomInMultiplier { get; set; }
        public bool IsDrumTrack { get; set; }
        public DrumTrackConfig? Drums { get; set; }
    }

    public class TrackConfigViewModel : NeomamViewModel
    {
        private MainViewModel _parent;

        public TrackConfigViewModel(MainViewModel parent, TrackConfig dto)
        {
            Dto = dto;
            _parent = parent;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            this._parent.NotifyConfigChanged();
            if (args.PropertyName == nameof(IsDrumTrack))
            {
                if (this.IsDrumTrack && this.Dto.Drums is null)
                {
                    this.Dto.Drums = new DrumTrackConfig();
                    var midiFile = this._parent.MidiFile ?? throw new InvalidOperationException();
                    var track = midiFile.Chunks.OfType<TrackChunk>().Single(t => t.GetName() == this.Dto.TrackName);
                    this.Dto.Drums.Notes.AddRange(track.GetNotes()
                            .Select(n => n.NoteNumber)
                            .Distinct()
                            .Select(n => new DrumNote
                            {
                                NoteNumber = n,
                                Name = $"Note {n}",
                                OutputNoteNumber = n,
                            })
                        );
                }
            }
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

        internal void MoveTo(TrackConfigViewModel target, bool before)
        {
            this._parent.Reorder(this, target, before);
        }

        public string TrackName => this.Dto.TrackName;

        public bool Visible
        {
            get => this.Dto.Visible;
            set => this.Set(() => this.Dto.Visible, () => this.Dto.Visible = value);
        }

        public bool IsDrumTrack
        {
            get => this.Dto.IsDrumTrack;
            set => this.Set(() => this.Dto.IsDrumTrack, () => this.Dto.IsDrumTrack = value);
        }

        public double ZoomInMultiplier
        {
            get
            {
                if (this.Dto.ZoomInMultiplier is null)
                {
                    return 0;
                }

                return Math.Log2(this.Dto.ZoomInMultiplier.Value);
            }
            set => this.Set(
                () => ZoomInMultiplier,
                () => this.Dto.ZoomInMultiplier = Math.Pow(2, value)
            );
        }

        public bool HasZoomConfigured
        {
            get => this.Dto.ZoomInMultiplier != null;
            set
            {
                if (value == this.HasZoomConfigured)
                {
                    return;
                }

                if (value)
                {
                    this.ZoomInMultiplier = 0;
                }
                else
                {
                    this.Dto.ZoomInMultiplier = null;
                    this.RaisePropChanged(nameof(this.ZoomInMultiplier));
                }

                this.RaisePropChanged(nameof(this.HasZoomConfigured));
            }
        }

        public TrackConfig Dto { get; }
    }
}
