using System;

namespace Biosplit.Game
{
    public sealed class EnemySettings
    {
        public EnemySettings(string name) { Name = name; }
        public string Name { get; }
        public FrameAnimationSettings FIdle { get; set; }
        public FrameAnimationSettings FBlock { get; set; }
        public FrameAnimationSettings FHurt { get; set; }
        public FrameAnimationSettings FDefeat { get; set; }
        public FrameAnimationSettings FAttack { get; set; }
        public string Weapon0 { get; set; }
        public WeaponSettings Weapon0Settings { get; internal set; }
        public int Health { get; set; } = 80;
        public DamageRange Damage { get; set; } = 15;
        public decimal AttackInterval { get; set; } = 2.4m;
        public decimal WarningTime { get; set; } = 0.8m;
        public int CooldownMs
        {
            get => (int)Math.Min(int.MaxValue, Math.Max(0m, AttackInterval) * 1000m);
            set => AttackInterval = value / 1000m;
        }
        internal EnemySettings Copy()
        {
            var copy = (EnemySettings)MemberwiseClone();
            copy.FIdle = FIdle?.Copy();
            copy.FBlock = FBlock?.Copy();
            copy.FHurt = FHurt?.Copy();
            copy.FDefeat = FDefeat?.Copy();
            copy.FAttack = FAttack?.Copy();
            return copy;
        }
        internal EnemySettings NormalizedCopy()
        {
            var copy = Copy();
            copy.Health = Math.Max(1, Health);
            copy.Damage = Damage;
            copy.AttackInterval = Math.Max(0.001m, AttackInterval);
            copy.WarningTime = Math.Max(0.1m, WarningTime);
            return copy;
        }
    }
}
