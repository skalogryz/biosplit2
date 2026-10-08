using System;

namespace Biosplit.Game
{
    // Describes one strike; phase durations are in milliseconds.
    public sealed class StrikeSettings
    {
        // Animation name interpreted by the presentation layer.
        public string Name { get; set; } = "";
        public int WindupMs { get; set; }
        public int DamageDurationMs { get; set; }
        public int RecoveryMs { get; set; }
        public decimal DamageModifier { get; set; } = 1m;
        public int AbsoluteDamage { get; set; }

        // Fractional damage is rounded down; damage cannot be negative.
        public int CalculateDamage(int weaponDamage)
        {
            if (DamageModifier == 0m) return Math.Max(0, AbsoluteDamage);
            if (DamageModifier < 0m || weaponDamage <= 0) return 0;
            if (DamageModifier >= (decimal)int.MaxValue / weaponDamage) return int.MaxValue;
            return (int)(weaponDamage * DamageModifier);
        }

        internal StrikeSettings Copy() => (StrikeSettings)MemberwiseClone();
    }
}