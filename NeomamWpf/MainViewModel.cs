using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace NeomamWpf
{
    public class MainViewModel : ViewModelBase
    {
        public static readonly MainViewModel Instance = new MainViewModel();

        private MainViewModel()
        {
        }

        public static void NotifyChange()
        {
            Instance.Dirty = true;
            Instance.Redraw?.Invoke();
        }

        public SKCanvas? DrawSurface { get; set; }

        private Config _config = new();

        private string? _projectPath;
        public string? ProjectPath
        {
            get => this._projectPath;
            set => this.Set(ref this._projectPath, value);
        }

        public event Action? Redraw;
        public MidiFile? MidiFile
        {
            get => _midiFile;
            private set
            {
                this._midiFile = value;
                this.MaxMicrosecond = (this._midiFile?.GetTotalMicroseconds() ?? 0);
            }
        }

        private bool _dirty;
        public bool Dirty
        {
            get => this._dirty;
            set => this.Set(ref this._dirty, value);
        }

        public string Title => this.DepProp(() => this.Dirty ? "neomam*" : "neomam");

        private double _maxMicrosecond;
        public double MaxMicrosecond
        {
            get => this._maxMicrosecond;
            set => this.Set(ref this._maxMicrosecond, value);
        }

        public double CurrentMicrosecond
        {
            get => this._config.CurrentTick;
            set => this.Set(() => this._config.CurrentTick, () => this._config.CurrentTick = value);
        }

        public double TicksPerVertical
        {
            get => this._config.TicksPerVertical;
            set => this.Set(() => this._config.TicksPerVertical, () => this._config.TicksPerVertical = value);
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

        private bool _showError;
        public bool ShowError
        {
            get => this._showError;
            set => this.Set(ref this._showError, value);
        }

        private string _error = "";
        private MidiFile? _midiFile;

        public string Error
        {
            get => this._error;
            set => this.Set(ref this._error, value);
        }

        private void SetError(string error)
        {
            this.Error = error;
            this.ShowError = !string.IsNullOrEmpty(error);
        }

        public Color BackColor
        {
            get => this._config.GetMediaBackColor();
            set => this.Set(() => this.BackColor, () => this._config.BackColor = value.ToString());
        }

        public bool CanRender => this.MidiFile != null;

        public ObservableCollection<TrackConfigViewModel> Tracks { get; }
            = new ObservableCollection<TrackConfigViewModel>();

        public RenderViewModel CreateRenderJob()
        {
            return RenderViewModel.Create(this.MidiFile ?? throw new InvalidOperationException(), this._config);
        }

        private bool InitChannels()
        {
            if (this._config?.Tracks?.Any() != true)
            {
                this.SetError("No named channels in midi file.");
                return false;
            }

            this.Tracks.Clear();
            this.Tracks.AddRange(this._config.Tracks.Select(ch => new TrackConfigViewModel(this, ch)));
            return true;
        }

        public void InitFromMidi(MidiFile file)
        {
            this.Tracks.Clear();
            this.MidiFile = file;

            this._config = new()
            {
                BackColor = "#FF000000",
                Tracks = file
                    .GetTrackChunks()
                    .Select(ch => ch.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .Select(name => new TrackConfig(name ?? throw new InvalidOperationException()))
                    .ToList(),
            };

            this.InitChannels();
            this.Dirty = true;
        }

        public void InitFromProject(string projectPath)
        {
            this.Tracks.Clear();
            var tempDir = GetTempDir();
            using var zipFile = ZipFile.OpenRead(projectPath);
            zipFile.ExtractToDirectory(tempDir.FullName);
            using var jsonFile = File.OpenRead(Path.Combine(tempDir.FullName, "config.json"));
            this._config = JsonSerializer.Deserialize<Config>(jsonFile) ?? throw new InvalidOperationException();
            this.MidiFile = MidiFile.Read(Path.Combine(tempDir.FullName, "midi.midi"));
            this.InitChannels();
            this.ProjectPath = projectPath;
            this.Dirty = false;
        }

        public void Reorder(TrackConfigViewModel source, TrackConfigViewModel target, bool before)
        {
            //find the index of the source.
            //find the index of the target
            if (source == target)
            {
                return;
            }

            if (this._config.Tracks is null)
            {
                return;
            }

            //find index of target
            var targetIndex = this._config.Tracks.IndexOf(target.Dto);
            var sourceIndex = this._config.Tracks.IndexOf(source.Dto);
            if (targetIndex == -1 || sourceIndex == -1)
            {
                return;
            }

            if (targetIndex > sourceIndex)
            {
                targetIndex--; //because we remove the source.
            }

            if (!before)
            {
                targetIndex++;
            }

            this._config.Tracks.RemoveAt(sourceIndex);
            this._config.Tracks.Insert(targetIndex, source.Dto);
            this.InitChannels();
        }

        public void SaveProject(string filePath)
        {
            if (this.MidiFile is null)
            {
                return;
            }

            //temp dir
            var tempDir = GetTempDir();
            var configJson = Path.Combine(tempDir.FullName, "config.json");
            var midiName = Path.Combine(tempDir.FullName, "midi.midi");
            //save config as json
            using (var jsonFile = File.OpenWrite(configJson))
            {
                JsonSerializer.Serialize(jsonFile, this._config);
            }

            this.MidiFile.Write(midiName);
            File.Delete(filePath);

            ZipFile.CreateFromDirectory(tempDir.FullName, filePath);
            this.Dirty = false;
        }

        private static DirectoryInfo GetTempDir()
        {
            var dir = Directory.CreateDirectory(Path.Combine(
                    Path.GetTempPath(),
                    "neomam-temp-" + DateTime.Now.TimeOfDay.TotalMilliseconds.ToString()
                ));
            foreach (var file in dir.EnumerateFiles())
            {
                file.Delete();
            }

            return dir;
        }

        public RenderContext? GetRenderContext()
        {
            return RenderContext.Get(this.MidiFile, this._config);
        }

        public void DrawMidi(SKCanvas canvas)
        {
            if (this.GetRenderContext() is RenderContext ctx)
            {
                Common.DrawMidi(canvas, ctx, this.CurrentMicrosecond);
            }
        }

        internal void NotifyConfigChanged()
        {
            this.Redraw?.Invoke();
        }
    }
}
