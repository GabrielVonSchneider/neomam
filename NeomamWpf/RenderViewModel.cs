using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeomamWpf
{
    public class RenderViewModel : NeomamViewModel
    {
        private RenderContext _renderCtx;
        public double MaxMicroseconds { get; set; }

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();

        public RenderConfigViewModel RenderConfig { get; }

        public RenderViewModel(MidiFile midiFile, Config config, MainViewModel parent)
        {
            this._renderCtx = RenderContext.Get(midiFile, config) ?? throw new InvalidOperationException("Tried to render empty file.");
            this.MaxMicroseconds = this._renderCtx.Midi.GetTotalMicroseconds();
            this.PropertyChanged += this.OwnPropChanged;
            this._endTime = TimeSpan.FromMilliseconds(this.MaxMicroseconds / 1000);
            this.RenderConfig = new RenderConfigViewModel(config.RenderConfig, parent);
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

        public Config Config => this._renderCtx.Config;

        public bool DrawNoteOn
        {
            get => this.Config.DrawNoteOn;
            set => this.Set(() => this.Config.DrawNoteOn, () => this.Config.DrawNoteOn = value);
        }

        public bool DrawNoteOff
        {
            get => this.Config.DrawNoteOff;
            set => this.Set(() => this.Config.DrawNoteOff, () => this.Config.DrawNoteOff = value);
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
                    this.Set(ref this._startTime, timeSpan);
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
            var tempoMap = this._renderCtx.Midi.GetTempoMap();
            var microsecondsIncrement = 1 / this.Config.RenderConfig.Framerate * 1000 * 1000;
            using var surface = SKSurface.Create(new SKImageInfo(
                    this.Config.RenderConfig.HorizontalPixels,
                    this.Config.RenderConfig.VerticalPixels
                ));

            var ctx = SynchronizationContext.Current ?? throw new InvalidOperationException();
            this._cancelSource = new CancellationTokenSource();
            var token = this._cancelSource.Token;
            this.Busy = true;
            await Task.Run(() =>
            {
                using var proc = new Process();
                if (this.Config.RenderConfig.FfmpegArgs is string ffmpegArgs)
                {
                    ffmpegArgs = ffmpegArgs.Replace("%filename%", filename);
                }
                else
                {
                    var resString = this.Config.RenderConfig.GetResString();
                    var fps = this.Config.RenderConfig.Framerate;
                    ffmpegArgs = $"-y -f rawvideo -pix_fmt bgra -s {resString} -r {fps} -i - -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p {filename}";
                }

                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
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

        public static RenderViewModel Create(MidiFile file, Config config, MainViewModel parent)
        {
            return new RenderViewModel(file.Clone(), config.CloneByJson(), parent);
        }

        public void Draw(SKCanvas canvas, double microsecond)
        {
            Common.DrawMidi(canvas, this._renderCtx, microsecond);
        }
    }

    public class RenderConfigViewModel : ViewModelBase
    {
        private readonly RenderConfig _dto;
        private readonly MainViewModel _parent;

        public RenderConfigViewModel(RenderConfig dto, MainViewModel parent)
        {
            this._dto = dto;
            this._parent = parent;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            this._parent.SetRenderConfig(this._dto);
        }

        public string Resolution
        {
            get => this._dto.GetResString();
            set
            {
                var match = Regex.Match(value, @"(\d+)x(\d+)");
                if (match.Success)
                {
                    this._dto.HorizontalPixels = int.Parse(match.Groups[1].Value);
                    this._dto.VerticalPixels = int.Parse(match.Groups[2].Value);
                    this.RaisePropChanged(nameof(Resolution));
                }
            }
        }

        public string? FfmpegArgs
        {
            get => this._dto.FfmpegArgs;
            set => this.Set(() => this.FfmpegArgs, () => this._dto.FfmpegArgs = value);
        }

        public double Framerate
        {
            get => this._dto.Framerate;
            set => this.Set(() => this.Framerate, () => this._dto.Framerate = value);
        }

        public bool UseStandardFfmpegArgs => this.DepProp(() => !this.UseCustomFfmpegArgs);

        public bool UseCustomFfmpegArgs
        {
            get => !string.IsNullOrWhiteSpace(this.FfmpegArgs);
            set
            {
                if (value == this.UseCustomFfmpegArgs)
                {
                    return;
                }

                if (value)
                {
                    var resString = this._dto.GetResString();
                    this.FfmpegArgs = $"-y -f rawvideo -pix_fmt bgra -s {resString} -r {this._dto.Framerate} -i - -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p %filename%";
                }
                else
                {
                    this.FfmpegArgs = null;
                }

                this.RaisePropChanged();
            }
        }
    }
}
