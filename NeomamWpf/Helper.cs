using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace NeomamWpf
{
    internal static class Helper
    {
        public static long GetEnd(this Note t)
        {
            return t.Time + t.Length;
        }

        public static IEnumerable<(int i, T item)> Enumerate<T>(this IEnumerable<T> self)
        {
            int i = 0;
            foreach (var item in self)
            {
                yield return (i++, item);
            }
        }

        public static IEnumerable<DependencyObject> VisualParents(this DependencyObject obj)
        {
            while ((obj = VisualTreeHelper.GetParent(obj)) != null)
            {
                yield return obj;
            }
        }

        public static SKColor ToSkia(this Color c) => new SKColor(c.R, c.G, c.B, c.A);

        public static string? GetName(this TrackChunk chunk) => chunk.Events
            .OfType<SequenceTrackNameEvent>()
            .FirstOrDefault()?.Text;

        public static T CloneByJson<T>(this T obj)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj))
                ?? throw new InvalidOperationException();
        }

        public static double GetTotalMicroseconds(this MidiFile file)
        {
            return TimeConverter.ConvertTo<MetricTimeSpan>(
                    file.GetNotes().Max(n => n.Time),
                    file.GetTempoMap()
                ).TotalMicroseconds;
        }

        public static IEnumerable<T> Safe<T>(this IEnumerable<T>? self)
        {
            return self ?? Enumerable.Empty<T>();
        }
    }
}
