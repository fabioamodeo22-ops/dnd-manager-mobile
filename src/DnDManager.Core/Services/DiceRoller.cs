using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DnDManager.Services
{
    public class DiceResult
    {
        public int[] Rolls { get; set; } = Array.Empty<int>();
        public int Modifier { get; set; }
        public int Total { get; set; }
        public string Expression { get; set; } = "";
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(string.Join(", ", Rolls)).Append(']');
            if (Modifier != 0) sb.Append(Modifier > 0 ? $" + {Modifier}" : $" - {-Modifier}");
            sb.Append($" = {Total}");
            return sb.ToString();
        }
    }

    /// <summary>Parsa e tira espressioni tipo "2d6+3", "d20", "1d8-1".</summary>
    public static class DiceRoller
    {
        private static readonly Regex Pattern = new(@"^\s*(\d*)d(\d+)\s*([+-]\s*\d+)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Random Rnd = new();

        public static DiceResult Roll(string expression)
        {
            var m = Pattern.Match(expression);
            if (!m.Success)
                throw new FormatException("Formato non valido. Usa es. 2d6+3, d20, 1d8-1.");

            int count = m.Groups[1].Value == "" ? 1 : int.Parse(m.Groups[1].Value);
            int sides = int.Parse(m.Groups[2].Value);
            int mod = m.Groups[3].Success ? int.Parse(m.Groups[3].Value.Replace(" ", "")) : 0;

            count = Math.Clamp(count, 1, 100);
            sides = Math.Clamp(sides, 2, 1000);

            var rolls = Enumerable.Range(0, count).Select(_ => Rnd.Next(1, sides + 1)).ToArray();
            return new DiceResult
            {
                Rolls = rolls,
                Modifier = mod,
                Total = rolls.Sum() + mod,
                Expression = expression.Trim()
            };
        }

        public static DiceResult RollSimple(int sides, int modifier = 0) =>
            Roll(modifier == 0 ? $"1d{sides}" : $"1d{sides}{(modifier > 0 ? "+" : "")}{modifier}");
    }
}
