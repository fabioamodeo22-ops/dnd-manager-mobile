using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDManager.Models
{
    /// <summary>Regole D&amp;D 5e della scheda personaggio (stesse tabelle della versione WPF).</summary>
    public static class CharacterRules
    {
        /// <summary>(chiave proprietà, sigla salvata nel DB, nome completo)</summary>
        public static readonly (string Key, string Short, string Full)[] Abilities =
        {
            ("Str", "FOR", "Forza"), ("Dex", "DES", "Destrezza"), ("Con", "COS", "Costituzione"),
            ("IntScore", "INT", "Intelligenza"), ("Wis", "SAG", "Saggezza"), ("Cha", "CAR", "Carisma")
        };

        public static readonly (string Name, string AbilityKey)[] Skills =
        {
            ("Acrobazia", "Dex"), ("Addestrare Animali", "Wis"), ("Arcano", "IntScore"), ("Atletica", "Str"),
            ("Furtività", "Dex"), ("Inganno", "Cha"), ("Intimidire", "Cha"), ("Intrattenere", "Cha"),
            ("Intuizione", "Wis"), ("Investigare", "IntScore"), ("Medicina", "Wis"), ("Natura", "IntScore"),
            ("Percezione", "Wis"), ("Persuasione", "Cha"), ("Rapidità di Mano", "Dex"), ("Religione", "IntScore"),
            ("Sopravvivenza", "Wis"), ("Storia", "IntScore")
        };

        // SRD 5.1 (CC-BY-4.0); a questi si aggiungono razze/classi personalizzate da Supabase
        public static readonly string[] SrdRaces =
            { "Draconide", "Elfo", "Gnomo", "Halfling", "Mezzelfo", "Mezzorco", "Nano", "Tiefling", "Umano" };

        public static readonly string[] SrdClasses =
            { "Barbaro", "Bardo", "Chierico", "Druido", "Guerriero", "Ladro", "Mago", "Monaco", "Paladino", "Ranger", "Stregone", "Warlock" };

        public static readonly string[] QuestStatuses = { "Pianificata", "In corso", "Completata", "Fallita" };

        public static string QuestIcon(string status) => status switch
        {
            "Pianificata" => "📋",
            "Completata" => "✅",
            "Fallita" => "❌",
            _ => "🕓"
        };

        public static int GetScore(Character c, string key) => key switch
        {
            "Str" => c.Str, "Dex" => c.Dex, "Con" => c.Con,
            "IntScore" => c.IntScore, "Wis" => c.Wis, "Cha" => c.Cha,
            _ => 10
        };

        public static void SetScore(Character c, string key, int value)
        {
            value = Math.Clamp(value, 1, 30);
            switch (key)
            {
                case "Str": c.Str = value; break;
                case "Dex": c.Dex = value; break;
                case "Con": c.Con = value; break;
                case "IntScore": c.IntScore = value; break;
                case "Wis": c.Wis = value; break;
                case "Cha": c.Cha = value; break;
            }
        }

        public static string FormatMod(int mod) => mod >= 0 ? $"+{mod}" : mod.ToString();

        public static HashSet<string> ParseList(string csv) =>
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        public static string ToList(IEnumerable<string> items) => string.Join(",", items);

        public static int SaveBonus(Character c, string key, string shortName) =>
            Character.Modifier(GetScore(c, key)) + (ParseList(c.ProficientSaves).Contains(shortName) ? c.ProficiencyBonus : 0);

        public static int SkillBonus(Character c, string skillName, string abilityKey) =>
            Character.Modifier(GetScore(c, abilityKey)) + (ParseList(c.ProficientSkills).Contains(skillName) ? c.ProficiencyBonus : 0);

        public static int PassivePerception(Character c) => 10 + SkillBonus(c, "Percezione", "Wis");
    }
}
