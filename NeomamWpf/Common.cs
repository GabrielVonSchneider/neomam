using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeomamWpf
{
    class ConfiguredTrack
    {
        public ConfiguredTrack(TrackConfig config, IEnumerable<Note> notes)
        {
            this.Config = config;
            this.Notes = notes;
        }

        public TrackConfig Config { get; }
        public IEnumerable<Note> Notes { get; }
    }

    internal static class Common
    {

        public static IEnumerable<ConfiguredTrack> GetConfiguredTracks(this MidiFile midiFile, Config config)
        {
            return config.Tracks.Safe().Select(t =>
            {
                return new ConfiguredTrack(
                        t,
                        midiFile.Chunks.OfType<TrackChunk>().Where(ch => ch.GetName() == t.TrackName)
                            .SelectMany(ch => ch.GetNotes())
                    );
            });
        }

        public static void DrawMidi(SKCanvas canvas, MidiFile midiFile, Config config, double microsecond)
        {
            if (!midiFile.GetNotes().Any())
            {
                return;
            }

            var tick = TimeConverter.ConvertFrom(
                    new MetricTimeSpan((long)Math.Round(microsecond)),
                    midiFile.GetTempoMap()
                );

            var tracks = midiFile.GetConfiguredTracks(config);

            var bounds = canvas.DeviceClipBounds;
            canvas.Clear();
            canvas.DrawRect(bounds, new SKPaint { Color = config.GetMediaBackColor().ToSkia() });

            var allNotes = midiFile.GetNotes();
            var drumNotes = tracks.Where(t => t.Config.IsDrumTrack).SelectMany(t => t.Notes).ToList();
            var regularNotes = tracks.Where(t => !t.Config.IsDrumTrack).SelectMany(t => t.Notes).ToList();

            var maxRegNote = (int)regularNotes.Max(n => n.NoteNumber);
            var minRegNote = (int)regularNotes.Min(n => n.NoteNumber);
            var maxNote = maxRegNote + 1; // leave single note border
            var minNote = minRegNote - drumNotes.Select(n => n.NoteNumber).Distinct().Count() - 1; //leave single note border
            var verticalNotes = maxNote - minNote - 1;

            var noteHeight = bounds.Height / verticalNotes;
            var ticksPerPixel = config.TicksPerVertical / bounds.Height;
            var centerX = bounds.Width / 2;

            foreach (var cTrack in tracks)
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
                        noteNumber = minRegNote - i - 1;
                    }
                    else
                    {
                        noteNumber = note.NoteNumber;
                    }

                    var x1 = (note.Time - tick) / (float)ticksPerPixel + centerX;
                    var x2 = (note.Time + note.Length - tick) / (float)ticksPerPixel + centerX;
                    var y1 = (maxNote - noteNumber) * noteHeight;
                    var y2 = y1 + noteHeight;

                    if (x1 < bounds.Width)
                    {
                        bool noteIsOn = note.Time <= tick && (note.Time + note.Length) >= tick;
                        if ((noteIsOn && !config.DrawNoteOn) || (!noteIsOn && !config.DrawNoteOff))
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
                                canvas.Scale(0.2f);
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
