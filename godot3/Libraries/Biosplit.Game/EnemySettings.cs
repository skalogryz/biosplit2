using System;

namespace Biosplit.Game
{
    public sealed class EnemySettings
    {
        public EnemySettings(string name) { Name = name; }
        public string Name { get; }
        public int Health { get; set; } = 80;
        public int Damage { get; set; } = 15;
        public decimal AttackInterval { get; set; } = 2.4m;
        public decimal WarningTime { get; set; } = 0.8m;
        public int CooldownMs
        {
            get => (int)Math.Min(int.MaxValue, Math.Max(0m, AttackInterval) * 1000m);
            set => AttackInterval = value / 1000m;
        }
        internal EnemySettings Copy() => (EnemySettings)MemberwiseClone();
        internal EnemySettings NormalizedCopy()
        {
            var copy = Copy();
            copy.Health = Math.Max(1, Health);
            copy.Damage = Math.Max(0, Damage);
            copy.AttackInterval = Math.Max(0.001m, AttackInterval);
            copy.WarningTime = Math.Max(0.1m, WarningTime);
            return copy;
        }
    }
}
