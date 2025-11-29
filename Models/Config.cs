using System;
using System.Collections.Generic;

namespace NeroUnfreeze.Models
{
    public class Config
    {
        public List<Preset> Presets { get; set; } = new List<Preset>();
        public int SelectedPresetIndex { get; set; } = 0;
        public bool AutoStart { get; set; } = true;
        public bool PreventMinimizeOnWinD { get; set; } = false;
    }

    public class Preset
    {
        public string Name { get; set; } = "默认组合";
        public DateTime TargetDate { get; set; } = new DateTime(DateTime.Now.Year, 12, 25);
        public int CountdownDays { get; set; } = 7;
        public string CharacterImagePath { get; set; } = "";
        public string IceImagePath { get; set; } = "";
        public string AudioPath { get; set; } = "";
        public double CharacterOpacity { get; set; } = 1.0;
        public double IceOpacity { get; set; } = 1.0;
        public double CharacterImageScale { get; set; } = 1.0;
        public double IceImageScale { get; set; } = 1.0;
        public double CharacterOffsetX { get; set; } = 0.0;
        public double CharacterOffsetY { get; set; } = 0.0;
        public double IceOffsetX { get; set; } = 0.0;
        public double IceOffsetY { get; set; } = 0.0;
        public double MaxAudioBlur { get; set; } = 0.8;
        public double MinAudioVolume { get; set; } = 0.2;
    }
}

