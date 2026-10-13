using System;
using System.Collections.Generic;

namespace DnDManager.Models
{
    /// <summary>Combattente nel tracker iniziativa. Vive solo per la sessione di gioco (non persistito).</summary>
    public class Combatant : System.ComponentModel.INotifyPropertyChanged
    {
        public long Id { get; set; }
        private string _name = "";
        private int _initiative;
        private int _hp;
        private bool _isCurrent;
        private int _gridX = -1;
        private int _gridY = -1;

        public string Name { get => _name; set { _name = value; Raise(nameof(Name)); } }
        public int Initiative { get => _initiative; set { _initiative = value; Raise(nameof(Initiative)); } }
        public int Hp { get => _hp; set { _hp = value; Raise(nameof(Hp)); } }
        public bool IsCurrent { get => _isCurrent; set { _isCurrent = value; Raise(nameof(IsCurrent)); } }
        public string PortraitPath { get; set; } = "";
        public long? CharacterId { get; set; }
        public int GridX { get => _gridX; set { _gridX = value; Raise(nameof(GridX)); } }
        public int GridY { get => _gridY; set { _gridY = value; Raise(nameof(GridY)); } }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class Campaign
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Story { get; set; } = "";
        public override string ToString() => Name;
    }

    public class CampaignFile
    {
        public long Id { get; set; }
        public long CampaignId { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public override string ToString() => Title;
    }

    public class Character
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Race { get; set; } = "";
        public string Class { get; set; } = "";
        public int Level { get; set; } = 1;
        public string Notes { get; set; } = "";
        public string SheetPath { get; set; } = "";
        public string PortraitPath { get; set; } = "";
        public long? CampaignId { get; set; }
        public string CampaignName { get; set; } = "";
        public bool IsNpc { get; set; }
        public string Owner { get; set; } = "";
        public Guid? OwnerId { get; set; }

        // Scheda personaggio completa
        public int Str { get; set; } = 10;
        public int Dex { get; set; } = 10;
        public int Con { get; set; } = 10;
        public int IntScore { get; set; } = 10;
        public int Wis { get; set; } = 10;
        public int Cha { get; set; } = 10;
        public string ProficientSaves { get; set; } = "";   // es. "For,Cos"
        public string ProficientSkills { get; set; } = "";  // es. "Percezione,Furtività"
        public int ArmorClass { get; set; } = 10;
        public int Speed { get; set; } = 9;
        public int HpMax { get; set; } = 0;
        public int HpCurrent { get; set; } = 0;
        public int HpTemp { get; set; } = 0;
        public string HitDice { get; set; } = "";
        public string Languages { get; set; } = "";
        public string OtherProficiencies { get; set; } = "";
        public int CoinsPp { get; set; }
        public int CoinsPo { get; set; }
        public int CoinsPe { get; set; }
        public int CoinsPa { get; set; }
        public int CoinsPr { get; set; }

        public int ProficiencyBonus => 2 + (Math.Max(1, Level) - 1) / 4;
        public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

        public override string ToString() =>
            (IsNpc ? "🎭 " : "") + $"{Name} — {Race} {Class} liv.{Level}" +
            (string.IsNullOrEmpty(CampaignName) ? "" : $"  [{CampaignName}]") +
            (string.IsNullOrEmpty(Owner) ? "" : $"  👤{Owner}");
    }

    public class Equipment
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Cost { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public List<string> AssignedNames { get; set; } = new();
        public override string ToString() =>
            $"{Name}" + (string.IsNullOrEmpty(Type) ? "" : $"  ({Type})") +
            (AssignedNames.Count == 0 ? "" : $"  → {string.Join(", ", AssignedNames)}");
    }

    public class Spell
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; } = 0;
        public string School { get; set; } = "";
        public string Classes { get; set; } = "";
        public string Components { get; set; } = "";
        public string CastingTime { get; set; } = "";
        public string Range { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public List<string> AssignedNames { get; set; } = new();
        public bool IsCantrip => Level <= 0;
        public override string ToString() =>
            $"{Name}" + (IsCantrip ? "  (Trucchetto)" : $"  (Liv.{Level})") +
            (string.IsNullOrEmpty(Classes) ? "" : $"  · {Classes}") +
            (AssignedNames.Count == 0 ? "" : $"  → {string.Join(", ", AssignedNames)}");
    }

    public class Quest
    {
        public long Id { get; set; }
        public long CampaignId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "In corso"; // Pianificata | In corso | Completata | Fallita
        public bool IsLocked { get; set; }
        public long? LinkedQuestId { get; set; }
        public override string ToString()
        {
            if (IsLocked) return "🔒 " + Title;
            var icon = Status switch { "Pianificata" => "📋 ", "Completata" => "✅ ", "Fallita" => "❌ ", _ => "🕓 " };
            return icon + Title;
        }
    }

    public class SharedFile
    {
        public long Id { get; set; }
        public System.Guid OwnerId { get; set; }
        public string OwnerUsername { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string FileType { get; set; } = "file"; // audio | image | pdf | word | file
    }

    public class Manual
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public bool IsUrl => FilePath.StartsWith("http://") || FilePath.StartsWith("https://");
        public override string ToString() => (IsUrl ? "🌐 " : "") + Title;
    }

    public class MapItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long? CampaignId { get; set; }
        public string CampaignName { get; set; } = "";
        public override string ToString() =>
            Name + (string.IsNullOrEmpty(CampaignName) ? "" : $"  [{CampaignName}]");
    }
}
