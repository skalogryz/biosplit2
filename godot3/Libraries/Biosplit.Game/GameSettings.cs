using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Biosplit.Game
{
    public sealed class GameSettings
    {
        private Dictionary<string, WeaponSettings> weapons = new Dictionary<string, WeaponSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["knife"] = new WeaponSettings("knife") { Type = WeaponType.Melee, Damage = 12, RageBonus = 10, StaminaCost = 3m, CooldownMs = 260 },
            ["handgun"] = new WeaponSettings("handgun") { Type = WeaponType.Firearm, Damage = 40, RageCost = 30, CooldownMs = 550 }
        };
        private Dictionary<string, EnemySettings> enemies = new Dictionary<string, EnemySettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["zombie"] = new EnemySettings("zombie")
        };
        public GameSettings()
        {
            Weapons = new ReadOnlyDictionary<string, WeaponSettings>(weapons);
            Enemies = new ReadOnlyDictionary<string, EnemySettings>(enemies);
        }
        public IReadOnlyDictionary<string, EnemySettings> Enemies { get; private set; }
        internal void RegisterEnemy(EnemySettings enemy) { enemies[enemy.Name] = enemy; }
        public IReadOnlyDictionary<string, WeaponSettings> Weapons { get; private set; }
        internal void RegisterWeapon(WeaponSettings weapon) { weapons[weapon.Name] = weapon; }

        public int MovementDurationMs { get; set; } = 150;
        public int MovementAccelerationMs { get; set; } = 50;
        public int MovementDecelerationMs { get; set; } = 30;
        public decimal HeroPos { get; set; } = 360m;
        public decimal EnemyMeleePos { get; set; } = 530m;
        public decimal EnemyMeleeSize { get; set; } = 80m;
        public int MaxHealth { get; set; } = 100;
        public int MaxRage { get; set; } = 100;
        public decimal MaxStamina { get; set; } = 100m;
        public decimal StaminaGrow { get; set; } = 2m;
        public decimal DodgeStamina { get; set; } = 10m;
        public int DodgeTimeMs { get; set; } = 3000;
        public int EnemyDamage { get => enemies["zombie"].Damage; set => enemies["zombie"].Damage = value; }
        public int EnemyHealth { get => enemies["zombie"].Health; set => enemies["zombie"].Health = value; }
        public decimal EnemyAttackInterval { get => enemies["zombie"].AttackInterval; set => enemies["zombie"].AttackInterval = value; }
        public decimal EnemyWarningTime { get => enemies["zombie"].WarningTime; set => enemies["zombie"].WarningTime = value; }

        internal GameSettings NormalizedCopy()
        {
            var copy = (GameSettings)MemberwiseClone();
            copy.weapons = new Dictionary<string, WeaponSettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in weapons) copy.weapons.Add(entry.Key, entry.Value.Copy());
            copy.Weapons = new ReadOnlyDictionary<string, WeaponSettings>(copy.weapons);
            copy.enemies = new Dictionary<string, EnemySettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in enemies) copy.enemies.Add(entry.Key, entry.Value.NormalizedCopy());
            copy.Enemies = new ReadOnlyDictionary<string, EnemySettings>(copy.enemies);
            copy.MovementDurationMs = Math.Max(1, MovementDurationMs);
            copy.MovementAccelerationMs = Math.Min(copy.MovementDurationMs, Math.Max(0, MovementAccelerationMs));
            copy.MovementDecelerationMs = Math.Min(copy.MovementDurationMs - copy.MovementAccelerationMs, Math.Max(0, MovementDecelerationMs));
            copy.EnemyMeleeSize = Math.Max(0m, EnemyMeleeSize);
            copy.MaxHealth = Math.Max(1, MaxHealth);
            copy.MaxRage = Math.Max(1, MaxRage);
            copy.MaxStamina = Math.Max(0m, MaxStamina);
            copy.StaminaGrow = Math.Max(0m, StaminaGrow);
            copy.DodgeStamina = Math.Max(0m, DodgeStamina);
            copy.DodgeTimeMs = Math.Max(1, DodgeTimeMs);
            return copy;
        }
    }
}
