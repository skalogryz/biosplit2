namespace Biosplit.Game
{
    public sealed class WeaponSettings
    {
        public WeaponSettings(string name) { Name = name; }
        public string Name { get; }
        public WeaponType Type { get; set; } = WeaponType.None;
        public int Damage { get; set; }
        public int RageBonus { get; set; }
        public int RageCost { get; set; }
        public decimal StaminaCost { get; set; }
        public int CooldownMs { get; set; }
        public int AttackDurationMs { get; set; }
        public StrikeSettings[] Striker { get; set; } = new[] { new StrikeSettings() };
        internal WeaponSettings Copy()
        {
            var copy = (WeaponSettings)MemberwiseClone();
            var strikes = Striker ?? new StrikeSettings[0];
            copy.Striker = new StrikeSettings[strikes.Length];
            for (int i = 0; i < strikes.Length; i++)
                copy.Striker[i] = strikes[i]?.Copy() ?? new StrikeSettings();
            return copy;
        }
    }
}
