using SkiaSharp;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NeomamWpf
{
    internal static class Helper
    {
        public static IEnumerable<DependencyObject> VisualParents(this DependencyObject obj)
        {
            while ((obj = VisualTreeHelper.GetParent(obj)) != null)
            {
                yield return obj;
            }
        }

        public static SKColor ToSkia(this Color c) => new SKColor(c.R, c.G, c.B, c.A);
    }
}
