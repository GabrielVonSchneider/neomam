using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

    public class DrumTrackConfigViewModel : NeomamViewModel
    {
        private DrumTrackConfig _dto;
        public ObservableCollection<DrumNoteViewModel> Notes { get; } = new ObservableCollection<DrumNoteViewModel>();

        public DrumTrackConfigViewModel(DrumTrackConfig dto)
        {
            this._dto = dto;
            this.Notes.AddRange(this._dto.Notes.Select(n => new DrumNoteViewModel(n)));
        }
    }

    public class DrumNoteViewModel : NeomamViewModel
    {
        private DrumNote _dto;

        public DrumNoteViewModel(DrumNote dto)
        {
            this._dto = dto;
        }

        public string? Name
        {
            get => this._dto.Name;
            set => this.Set(() => this._dto.Name, () => this._dto.Name = value);
        }

        public int NoteNumber
        {
            get => this._dto.NoteNumber;
            set => this.Set(() => this._dto.NoteNumber, () => this._dto.NoteNumber = value);
        }

        public string? AfterHitSvg
        {
            get => this._dto.AfterHitSvg;
            set => this.Set(() => this._dto.AfterHitSvg, () => this._dto.AfterHitSvg = value);
        }

        public string? BeforeHitSvg
        {
            get => this._dto.BeforeHitSvg;
            set => this.Set(() => this._dto.BeforeHitSvg, () => this._dto.BeforeHitSvg = value);
        }

        public int OutputNoteNumber
        {
            get => this._dto.OutputNoteNumber;
            set => this.Set(() => this._dto.OutputNoteNumber, () => this._dto.OutputNoteNumber = value);
        }

        public double HeightMultiplier
        {
            get => this._dto.HeightMultiplier;
            set => this.Set(() => this._dto.HeightMultiplier, () => this._dto.HeightMultiplier = value);
        }
    }
}
