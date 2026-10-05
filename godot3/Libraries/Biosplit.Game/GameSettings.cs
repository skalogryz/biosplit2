using System;

namespace Biosplit.Game
{
    public sealed class GameSettings
    {
        public int MaxHealth { get; set; } = 100;
        public int MaxRage { get; set; } = 100;
        public decimal MaxStamina { get; set; } = 100m;
        public decimal StaminaGrow { get; set; } = 2m;
        public decimal DodgeStamina { get; set; } = 10m;
        public int DodgeTimeMs { get; set; } = 3000;
        public int RagePerPunch { get; set; } = 10;
        public int PunchCost { get; set; }
        public int ShotCost { get; set; } = 30;
        public decimal PunchStaminaCost { get; set; } = 3m;
        public decimal ShotStaminaCost { get; set; }
        public int PunchDamage { get; set; } = 12;
        public int ShotDamage { get; set; } = 40;
        public int PunchCooldownMs { get; set; } = 260;
        public int ShotCooldownMs { get; set; } = 550;
        public int EnemyDamage { get; set; } = 15;
        public int EnemyHealth { get; set; } = 80;
        public decimal EnemyAttackInterval { get; set; } = 2.4m;
        public decimal EnemyWarningTime { get; set; } = 0.8m;

        internal GameSettings NormalizedCopy()
        {
            var copy = (GameSettings)MemberwiseClone();
            copy.MaxHealth = Math.Max(1, MaxHealth);
            copy.MaxRage = Math.Max(1, MaxRage);
            copy.MaxStamina = Math.Max(0m, MaxStamina);
            copy.StaminaGrow = Math.Max(0m, StaminaGrow);
            copy.DodgeStamina = Math.Max(0m, DodgeStamina);
            copy.DodgeTimeMs = Math.Max(1, DodgeTimeMs);
            copy.RagePerPunch = Math.Max(1, RagePerPunch);
            copy.PunchCost = Math.Max(0, PunchCost);
            copy.ShotCost = Math.Max(0, ShotCost);
            copy.PunchStaminaCost = Math.Max(0m, PunchStaminaCost);
            copy.ShotStaminaCost = Math.Max(0m, ShotStaminaCost);
            copy.PunchDamage = Math.Max(1, PunchDamage);
            copy.ShotDamage = Math.Max(1, ShotDamage);
            copy.PunchCooldownMs = Math.Max(0, PunchCooldownMs);
            copy.ShotCooldownMs = Math.Max(0, ShotCooldownMs);
            copy.EnemyDamage = Math.Max(0, EnemyDamage);
            copy.EnemyHealth = Math.Max(1, EnemyHealth);
            copy.EnemyAttackInterval = Math.Max(0.001m, EnemyAttackInterval);
            copy.EnemyWarningTime = Math.Max(0.1m, EnemyWarningTime);
            return copy;
        }
    }
}
