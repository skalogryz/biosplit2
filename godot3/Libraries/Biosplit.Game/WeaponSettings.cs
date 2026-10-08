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
        internal WeaponSettings Copy() => (WeaponSettings)MemberwiseClone();
    }
}
