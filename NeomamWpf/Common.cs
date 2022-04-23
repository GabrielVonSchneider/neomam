using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeomamWpf
{
    public class ConfiguredTrack
    {
        public ConfiguredTrack(TrackConfig config, IEnumerable<Note> notes)
        {
            this.Config = config;
            this.Notes = notes;
        }

        public TrackConfig Config { get; }
        public IEnumerable<Note> Notes { get; }
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

            return new RenderContext(midiFile, config);
        }

        private RenderContext(MidiFile midiFile, Config config)
        {
            this.Midi = midiFile;
            this.Config = config;
            this.ConfiguredTracks = midiFile.GetConfiguredTracks(Config).ToList();

            var allNotes = midiFile.GetNotes();
            var drumNotes = this.ConfiguredTracks.Where(t => t.Config.IsDrumTrack).SelectMany(t => t.Notes).ToList();
            var regularNotes = this.ConfiguredTracks.Where(t => !t.Config.IsDrumTrack).SelectMany(t => t.Notes).ToList();

            var maxRegNote = (int)regularNotes.Max(n => n.NoteNumber);
            this.MinRegNote = (int)regularNotes.Min(n => n.NoteNumber);
            this.MaxNote = maxRegNote + 1; // leave single note border
            var minNote = this.MinRegNote - drumNotes.Select(n => n.NoteNumber).Distinct().Count() - 1; //leave single note border
            this.VerticalNotes = this.MaxNote - minNote - 1;
        }

        public MidiFile Midi { get; }
        public Config Config { get; }
        public int VerticalNotes { get; }

        public List<ConfiguredTrack> ConfiguredTracks { get; }
        public int MinRegNote { get; }
        public int MaxNote { get; }

        //calculate the max.
        //calculate some kind of slot map? x let's just stick to conversion for now
    }

    internal static class Common
    {

        public static IEnumerable<ConfiguredTrack> GetConfiguredTracks(this MidiFile midiFile, Config config)
        {
            return config.Tracks.Safe().Reverse().Select(t => //reverse so "top track = top layer"
            {
                return new ConfiguredTrack(
                        t,
                        midiFile.Chunks.OfType<TrackChunk>().Where(ch => ch.GetName() == t.TrackName)
                            .SelectMany(ch => ch.GetNotes())
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
                    int noteNumber;
                    DrumNote? drumNoteConfig = null;
                    if (cTrack.Config.IsDrumTrack)
                    {
                        int i;
                        (i, drumNoteConfig) = cTrack.Config.Drums?.Notes
                            .Enumerate()
                            .First(n => n.item.NoteNumber == note.NoteNumber)
                            ?? throw new InvalidOperationException();
                        noteNumber = context.MinRegNote - i - 1;
                    }
                    else
                    {
                        noteNumber = note.NoteNumber;
                    }

                    var ticksPerVertical = context.Config.TicksPerVertical;
                    if (cTrack.Config.ZoomInMultiplier is double zoomIn)
                    {
                        ticksPerVertical /= zoomIn;
                    }

                    var ticksPerPixel = ticksPerVertical / bounds.Height;
                    var x1 = (note.Time - tick) / (float)ticksPerPixel + centerX;
                    var x2 = (note.Time + note.Length - tick) / (float)ticksPerPixel + centerX;
                    var noteHeight = (int)Math.Round(slotHeight * (float)(context.Config.TicksPerVertical / ticksPerVertical));
                    var y1 = (context.MaxNote - noteNumber) * slotHeight;
                    var y2 = y1 + noteHeight;

                    if (x1 < bounds.Width)
                    {
                        bool noteIsOn = note.Time <= tick && (note.Time + note.Length) >= tick;
                        if ((noteIsOn && !context.Config.DrawNoteOn) || (!noteIsOn && !context.Config.DrawNoteOff))
                        {
                            continue;
                        }

                        if (drumNoteConfig != null)
                        {
                            bool isBeforeHit = note.Time > tick;
                            string? svgString = isBeforeHit ? drumNoteConfig.BeforeHitSvg : drumNoteConfig.AfterHitSvg;
                            if (svgString != null)
                            {
                                const long ticksFadeout = 1000;
                                byte opacity;
                                if (isBeforeHit)
                                {
                                    opacity = 255;
                                }
                                else
                                {
                                    //fade out
                                    var delta = tick - note.Time;
                                    if (delta > ticksFadeout)
                                    {
                                        opacity = 0;
                                    }
                                    else
                                    {
                                        opacity = (byte)((ticksFadeout - delta) * 255 / ticksFadeout);
                                    }
                                }

                                var svg = new SKSvg();
                                svg.FromSvg(svgString);
                                var svgSize = svg.Picture?.CullRect.Size ?? throw new InvalidOperationException("no svg picture");
                                var rect = new SKRect(x1, y1, x2, y2);
                                var fit = rect.AspectFit(svgSize.ToSizeI());
                                canvas.Translate(x1, y1);
                                canvas.Scale(rect.Height / svgSize.Height);
                                canvas.DrawPicture(svg.Picture, new SKPaint { Color = new SKColor(255, 255, 255, opacity)});
                                canvas.ResetMatrix();
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
                                noteHeight,
                                new SKPaint { Color = noteIsOn ? onColor : offColor, IsAntialias = true, }
                            );
                        }
                    }
                }
            }
        }
    }
}
