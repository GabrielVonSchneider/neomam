using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace neomam
{
    public partial class MainForm : Form
    {
        private MainViewModels

        public MainForm()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //load the midi file.
            using var dlg = new OpenFileDialog
            {
                Filter = "midi|*.mid",
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this._midiFile = MidiFile.Read(dlg.FileName);
                this.pictureBox1.Invalidate();
                this._notes = this._midiFile.GetNotes().ToList();

                var tempoMap = this._midiFile.GetTempoMap();

                this.timeline.Minimum = 0;
                this.timeline.Maximum = (int)_notes.Max(n => n.Time);
                Debug.WriteLine(this._midiFile.GetTempoMap().TimeDivision);
                var endOfFile = this._midiFile.GetTimedEvents().Max(ev => ev.Time);
                var fileLength = TimeConverter.ConvertTo<MetricTimeSpan>(endOfFile, tempoMap);
                this.timeline.Minimum = 0;
                this.timeline.Maximum = (int)(fileLength.TotalMicroseconds / 1000);
            }
        }

        interface ITimedEvent
        {
            MidiEvent Event { get; }
            long Time { get; }
        }

        private static IEnumerable<double> IterFrames(long totalMicroseconds)
        {
            var incrementMicroseconds = 1 / 60d * 1000 * 1000;
            double currentTime = 0;
            while (currentTime < (totalMicroseconds))
            {
                yield return currentTime += incrementMicroseconds;
            }
        }

        private void btnSnip_Click(object sender, EventArgs e)
        {
            if (this._midiFile is null || this._notes is null)
            {
                return;
            }

            var parms = this.GetParams();
            var tempoMap = this._midiFile.GetTempoMap();
            var endOfFile = this._midiFile.GetTimedEvents().Max(ev => ev.Time);
            var fileLength = TimeConverter.ConvertTo<MetricTimeSpan>(endOfFile, tempoMap);
            var timer = Stopwatch.StartNew();
            using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));

            DrawMidiSkia(
                    this._notes,
                    surface.Canvas,
                    LengthConverter.ConvertFrom(new MetricTimeSpan(this.timeline.Value * 1000), 0, tempoMap),
                    parms
                );
            using var pixels = surface.PeekPixels();
            var data = pixels.Encode(new SKPngEncoderOptions());
            using var file = File.OpenWrite("c:/users/jacob/desktop/mampics/snip.png");
            file.Write(data.AsSpan());
        }

        private async void Render()
        {
            if (this._midiFile is null || this._notes is null)
            {
                return;
            }

            var parms = this.GetParams();
            var tempoMap = this._midiFile.GetTempoMap();
            var endOfFile = this._midiFile.GetTimedEvents().Max(ev => ev.Time);
            var fileLength = TimeConverter.ConvertTo<MetricTimeSpan>(endOfFile, tempoMap);
            var timer = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                using var ffmpegProc = new Process();
                ffmpegProc.StartInfo.FileName = "ffmpeg";
                ffmpegProc.StartInfo.RedirectStandardInput = true;
                ffmpegProc.StartInfo.RedirectStandardOutput = true;
                ffmpegProc.StartInfo.RedirectStandardError = true;
                ffmpegProc.StartInfo.UseShellExecute = false;
                ffmpegProc.StartInfo.CreateNoWindow = true;
                ffmpegProc.ErrorDataReceived += FfmpegProc_ErrorDataReceived;
                ffmpegProc.OutputDataReceived += FfmpegProc_OutputDataReceived;
                ffmpegProc.StartInfo.Arguments = $"-y -f rawvideo -pix_fmt argb -s 1920x1080 -r 60 -i - c:/users/jacob/desktop/mampics/out_vid.mp4";
                ffmpegProc.Start();
                ffmpegProc.BeginErrorReadLine();
                ffmpegProc.BeginOutputReadLine();

                Debug.WriteLine("starting render");
                using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
                
                foreach (var currentTime in IterFrames(fileLength.TotalMicroseconds))
                {
                    var microsecond = (long)Math.Round(currentTime);
                    DrawMidiSkia(
                            this._notes,
                            surface.Canvas,
                            LengthConverter.ConvertFrom(new MetricTimeSpan(microsecond), 0, tempoMap),
                            parms
                        );
                    this.Invoke(() =>
                    {
                        this.progressBar1.Value = (int)(currentTime / fileLength.TotalMicroseconds * 100);
                    });
                    using var pixels = surface.PeekPixels();
                    ffmpegProc.StandardInput.BaseStream.Write(pixels.GetPixelSpan());
                }

                ffmpegProc.StandardInput.Close();
            });

            Debug.WriteLine($"Took {timer.Elapsed.Seconds}");
        }

        private void FfmpegProc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine($"ffmpeg output: {e.Data}");
        }

        private void FfmpegProc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine($"ffmpeg error: {e.Data}");
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (this._notes is null || !this._notes.Any())
            {
                return;
            }

            var currentTick = LengthConverter.ConvertFrom(
                    new MetricTimeSpan(this.timeline.Value * 1000),
                    0,
                    this._midiFile.GetTempoMap()
                );
            DrawMidi(
                    this._notes,
                    e.Graphics,
                    currentTick,
                    this.GetParams()
                );
        }

        private void slider_Scroll(object sender, EventArgs e)
        {
            this.pictureBox1.Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.pictureBox1.Invalidate();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void tbZoom_Scroll(object sender, EventArgs e)
        {
            this.pictureBox1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Render();
        }
    }
}
