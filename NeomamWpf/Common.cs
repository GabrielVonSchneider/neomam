using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeomamWpf
{
    public class ConfiguredNote
    {
        public ConfiguredNote(Note note)
        {
            this.Note = note;
        }

        public Note Note { get; }
        public DrumNote? DrumConfig { get; set; }
        public int NoteNumber => this.DrumConfig?.OutputNoteNumber ?? Note.NoteNumber;
        public double HeightMultiplier => this.DrumConfig?.HeightMultiplier ?? 1;
    }

    public class ConfiguredTrack
    {
        public ConfiguredTrack(TrackConfig config, IEnumerable<ConfiguredNote> notes)
        {
            this.Config = config;
            this.Notes = notes;
        }

        public TrackConfig Config { get; }
        public IEnumerable<ConfiguredNote> Notes { get; }
    }

    public class RenderContext
    {
        public static RenderContext? Get(MidiFile? midiFile, Config? config)
        {
            if (midiFile is null || !midiFile.GetNotes().Any())
            {
                return null;
            }

            if (config is null)
            {
                return null;
            }

            var configuredTracks = midiFile.GetConfiguredTracks(config).ToList();
            if (!configuredTracks.SelectMany(t => t.Notes).Any())
            {
                return null;
            }

            return new RenderContext(midiFile, config, configuredTracks);
        }

        private RenderContext(MidiFile midiFile, Config config, List<ConfiguredTrack> configuredTracks)
        {
            this.Midi = midiFile;
            this.Config = config;
            this.ConfiguredTracks = configuredTracks;

            var allNotes = this.ConfiguredTracks.SelectMany(t => t.Notes).ToList();
            this.MaxNote = allNotes.Max(n => n.NoteNumber) + 1; // leave single note border
            this.MinNote = allNotes.Min(n => n.NoteNumber - ((int)Math.Ceiling(n.HeightMultiplier)) - 1) - 1; // leave single note border

            this.VerticalNotes = this.MaxNote - this.MinNote + 1;
        }

        public MidiFile Midi { get; }
        public Config Config { get; }
        public int VerticalNotes { get; }

        public List<ConfiguredTrack> ConfiguredTracks { get; }
        public int MinNote { get; }
        public int MaxNote { get; }
    }

    internal static class Common
    {

        public static IEnumerable<ConfiguredTrack> GetConfiguredTracks(this MidiFile midiFile, Config config)
        {
            return config.Tracks.Safe()
                .Where(t => t.Visible)
                .Reverse() //reverse so "top track = top layer"
                .Select(t => 
            {
                var drums = new Dictionary<int, DrumNote>();
                if (t.IsDrumTrack && t.Drums != null)
                {
                    drums = t.Drums.Notes.ToDictionary(n => n.NoteNumber);
                }

                return new ConfiguredTrack(
                        t,
                        midiFile.Chunks.OfType<TrackChunk>().Where(ch => ch.GetName() == t.TrackName)
                            .SelectMany(ch => ch.GetNotes())
                            .Select(n =>
                            {
                                var cNote = new ConfiguredNote(n);
                                if (drums.TryGetValue(n.NoteNumber, out var drumNote))
                                {
                                    cNote.DrumConfig = drumNote;
                                }

                                return cNote;
                            })
                    );
            });
        }

        public static void DrawMidi(SKCanvas canvas, RenderContext context, double microsecond)
        {
            var tick = TimeConverter.ConvertFrom(
                    new MetricTimeSpan((long)Math.Round(microsecond)),
                    context.Midi.GetTempoMap()
                );

            var bounds = canvas.DeviceClipBounds;
            canvas.Clear();
            canvas.DrawRect(bounds, new SKPaint { Color = context.Config.GetMediaBackColor().ToSkia() });

            var slotHeight = bounds.Height / context.VerticalNotes;
            var centerX = bounds.Width / 2;

            foreach (var cTrack in context.ConfiguredTracks)
            {
                foreach (var note in cTrack.Notes)
                {
                    var ticksPerVertical = context.Config.TicksPerVertical;
                    if (cTrack.Config.ZoomInMultiplier is double zoomIn)
                    {
                        ticksPerVertical /= zoomIn;
                    }

                    var ticksPerPixel = ticksPerVertical / bounds.Height;
                    var x1 = (note.Note.Time - tick) / (float)ticksPerPixel + centerX;
                    var x2 = (note.Note.Time + note.Note.Length - tick) / (float)ticksPerPixel + centerX;

                    var noteHeightFloat = slotHeight * context.Config.TicksPerVertical / ticksPerVertical;
                    if (note.DrumConfig != null)
                    {
                        noteHeightFloat *= note.DrumConfig.HeightMultiplier;
                    }
                    var noteHeight = (int)Math.Round(noteHeightFloat);

                    var y1 = (context.MaxNote - note.NoteNumber) * slotHeight;
                    var y2 = y1 + noteHeight;

                    if (x1 < bounds.Width && x2 > (-bounds.Width))
                    {
                        bool noteIsOn = note.Note.Time <= tick && (note.Note.Time + note.Note.Length) >= tick;
                        if ((noteIsOn && !context.Config.DrawNoteOn) || (!noteIsOn && !context.Config.DrawNoteOff))
                        {
                            continue;
                        }

                        if (note.DrumConfig is DrumNote drumNoteConfig)
                        {
                            bool isBeforeHit = note.Note.Time > tick;
                            if (drumNoteConfig.BeforeHitSvg != null || drumNoteConfig.AfterHitSvg != null)
                            {
                                const long ticksFadeout = 1000;
                                byte onOpacity;
                                if (isBeforeHit)
                                {
                                    onOpacity = 0;
                                }
                                else
                                {
                                    var delta = tick - note.Note.Time;
                                    if (delta > ticksFadeout)
                                    {
                                        onOpacity = 0;
                                    }
                                    else
                                    {
                                        onOpacity = (byte)((ticksFadeout - delta) * 255 / ticksFadeout);
                                    }
                                }
                                byte offOpacity = (byte)(255 - onOpacity);
                                var rect = new SKRect(x1, y1, x2, y2);

                                if (drumNoteConfig.BeforeHitSvg != null)
                                {
                                    DrawSvg(canvas, drumNoteConfig.BeforeHitSvg, offOpacity, rect);
                                }

                                if (drumNoteConfig.AfterHitSvg != null)
                                {
                                    DrawSvg(canvas, drumNoteConfig.AfterHitSvg, onOpacity, rect);
                                }
                            }
                        }
                        else
                        {
                            var onColor = new SKColor(100, 100, 0xFF);
                            var offColor = new SKColor(0, 0, 0xFF);

                            if (!cTrack.Config.Visible)
                            {
                                continue;
                            }

                            if (SKColor.TryParse(cTrack.Config.OnColor, out var parsed))
                            {
                                onColor = parsed;
                            }
                            if (SKColor.TryParse(cTrack.Config.OffColor, out parsed))
                            {
                                offColor = parsed;
                            }

                            canvas.DrawRect(
                                (float)x1,
                                y1,
                                (float)(x2 - x1),
                                (int)Math.Round(noteHeightFloat),
                                new SKPaint { Color = noteIsOn ? onColor : offColor, IsAntialias = true, }
                            );
                        }
                    }
                }
            }
        }

        private static void DrawSvg(SKCanvas canvas, string svgString, byte onOpacity, SKRect rect)
        {
            var svg = new SKSvg();
            svg.FromSvg(svgString);
            var svgSize = svg.Picture?.CullRect.Size ?? throw new InvalidOperationException("no svg picture");
            var fit = rect.AspectFit(svgSize.ToSizeI());
            canvas.Translate(rect.Left, rect.Top);
            canvas.Scale(rect.Height / svgSize.Height);
            canvas.DrawPicture(svg.Picture, new SKPaint { Color = new SKColor(255, 255, 255, onOpacity) });
            canvas.ResetMatrix();
        }
    }
}
