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
        private StrikeSettings[] striker = new[] { new StrikeSettings() };
        public StrikeSettings[] Striker
        {
            get => striker;
            set => striker = value == null || value.Length == 0
                ? new[] { new StrikeSettings() } : value;
        }
        internal WeaponSettings Copy()
        {
            var copy = (WeaponSettings)MemberwiseClone();
            var strikes = Striker;
            copy.Striker = new StrikeSettings[strikes.Length];
            for (int i = 0; i < strikes.Length; i++)
                copy.Striker[i] = strikes[i]?.Copy() ?? new StrikeSettings();
            return copy;
        }
    }
}
