using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NeomamWpf
{
    public class RenderViewModel : ViewModelBase
    {
        private MidiFile _midiFile;
        private Config _config;
        public double MaxMicroseconds { get; set; }

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();

        public RenderViewModel(MidiFile midiFile, Config config)
        {
            this._midiFile = midiFile;
            this._config = config;
            this.MaxMicroseconds = this._midiFile.GetTotalMicroseconds();
            this.PropertyChanged += this.OwnPropChanged;
            this._endTime = TimeSpan.FromMilliseconds(this.MaxMicroseconds / 1000);
        }

        private void OwnPropChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentMicrosecond):
                    this.Progress = (this.CurrentMicrosecond / 1000 - this._startTime.TotalMilliseconds)
                        / (this._endTime.TotalMilliseconds - this._startTime.TotalMilliseconds);
                    break;
                case nameof(Busy):
                    this.EnableInput = !this.Busy;
                    break;
            }
        }

        public bool DrawNoteOn
        {
            get => this._config.DrawNoteOn;
            set => this.Set(() => this._config.DrawNoteOn, () => this._config.DrawNoteOn = value);
        }

        public bool DrawNoteOff
        {
            get => this._config.DrawNoteOff;
            set => this.Set(() => this._config.DrawNoteOff, () => this._config.DrawNoteOff = value);
        }

        private TimeSpan _endTime;
        public string EndTime
        {
            get => this._endTime.ToString();
            set
            {
                if (TimeSpan.TryParse(value, out var timeSpan)
                    && timeSpan.TotalMilliseconds * 1000 < this.MaxMicroseconds)
                {
                    this.Set(ref this._endTime, timeSpan);
                }
            }
        }

        private TimeSpan _startTime;
        public string StartTime
        {
            get => this._startTime.ToString();
            set
            {
                if (TimeSpan.TryParse(value, out var timeSpan)
                    && timeSpan.TotalMilliseconds * 1000 <= this.MaxMicroseconds)
                {
                    this.Set(ref this._endTime, timeSpan);
                }
            }
        }

        private double _progress;
        public double Progress
        {
            get => this._progress;
            set => this.Set(ref this._progress, value);
        }

        private double _currentMicrosecond;
        public double CurrentMicrosecond
        {
            get => this._currentMicrosecond;
            set => this.Set(ref this._currentMicrosecond, value);
        }

        private bool _busy = false;
        public bool Busy
        {
            get => this._busy;
            set => this.Set(ref this._busy, value);
        }

        private bool _enableInput = true;
        public bool EnableInput
        {
            get => this._enableInput;
            set => this.Set(ref this._enableInput, value);
        }

        public void Cancel() => this._cancelSource.Cancel();

        public async void StartRender(string filename)
        {
            var microsecond = (this._startTime.TotalMilliseconds) * 1000;
            var tempoMap = this._midiFile.GetTempoMap();
            var microsecondsIncrement = 1 / 60D * 1000 * 1000;
            using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));

            var ctx = SynchronizationContext.Current ?? throw new InvalidOperationException();
            this._cancelSource = new CancellationTokenSource();
            var token = this._cancelSource.Token;
            this.Busy = true;
            await Task.Run(() =>
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -f rawvideo -pix_fmt bgra -s 1920x1080 -r 60 -i - {filename}",
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                };
                proc.Start();
                while (this.CurrentMicrosecond <= (this._endTime.TotalMilliseconds) * 1000)
                {
                    microsecond += microsecondsIncrement;
                    this.Draw(surface.Canvas, microsecond);

                    using var pixmap = surface.PeekPixels();
                    var pixels = pixmap.GetPixelSpan();

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    proc.StandardInput.BaseStream.Write(pixels);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    ctx.Post(obj => this.CurrentMicrosecond = microsecond, null);
                }

                proc.StandardInput.Close();
            });
            this.Busy = false;
        }

        public static RenderViewModel Create(MidiFile file, Config config)
        {
            return new RenderViewModel(file.Clone(), config.CloneByJson());
        }

        public void Draw(SKCanvas canvas, double microsecond)
        {
            Common.DrawMidi(canvas, this._midiFile, this._config, microsecond);
        }
    }
}
