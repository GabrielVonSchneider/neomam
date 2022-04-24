using Svg;
using Svg.Skia;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NeomamWpf
{
    public class DrumTrackConfig
    {
        public List<DrumNote> Notes { get; set; } = new List<DrumNote>();
    }

    public class DrumNote
    {
        public string? Name { get; set; }
        public int NoteNumber { get; set; }
        public string? AfterHitSvg { get; set; }
        public string? BeforeHitSvg { get; set; }
        public int OutputNoteNumber { get; set; }
        public double HeightMultiplier { get; set; } = 1;
    }
}
