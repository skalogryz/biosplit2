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
        public GameSettings() { Weapons = new ReadOnlyDictionary<string, WeaponSettings>(weapons); }
        public IReadOnlyDictionary<string, WeaponSettings> Weapons { get; private set; }
        internal void RegisterWeapon(WeaponSettings weapon) { weapons[weapon.Name] = weapon; }

        public decimal HeroPos { get; set; } = 360m;
        // Lerp coefficient for approach and return: weight = coefficient * deltaSeconds.
        public decimal HeroSpeed { get; set; } = 20m;
        public decimal EnemyMeleePos { get; set; } = 530m;
        // Lerp coefficient for enemy approach and return.
        public decimal EnemySpeed { get; set; } = 20m;
        public decimal EnemyMeleeSize { get; set; } = 80m;
        public int MaxHealth { get; set; } = 100;
        public int MaxRage { get; set; } = 100;
        public decimal MaxStamina { get; set; } = 100m;
        public decimal StaminaGrow { get; set; } = 2m;
        public decimal DodgeStamina { get; set; } = 10m;
        public int DodgeTimeMs { get; set; } = 3000;
        public int EnemyDamage { get; set; } = 15;
        public int EnemyHealth { get; set; } = 80;
        public decimal EnemyAttackInterval { get; set; } = 2.4m;
        public decimal EnemyWarningTime { get; set; } = 0.8m;

        internal GameSettings NormalizedCopy()
        {
            var copy = (GameSettings)MemberwiseClone();
            copy.weapons = new Dictionary<string, WeaponSettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in weapons) copy.weapons.Add(entry.Key, entry.Value.Copy());
            copy.Weapons = new ReadOnlyDictionary<string, WeaponSettings>(copy.weapons);
            copy.HeroSpeed = Math.Max(0.001m, HeroSpeed);
            copy.EnemySpeed = Math.Max(0.001m, EnemySpeed);
            copy.EnemyMeleeSize = Math.Max(0m, EnemyMeleeSize);
            copy.MaxHealth = Math.Max(1, MaxHealth);
            copy.MaxRage = Math.Max(1, MaxRage);
            copy.MaxStamina = Math.Max(0m, MaxStamina);
            copy.StaminaGrow = Math.Max(0m, StaminaGrow);
            copy.DodgeStamina = Math.Max(0m, DodgeStamina);
            copy.DodgeTimeMs = Math.Max(1, DodgeTimeMs);
            copy.EnemyDamage = Math.Max(0, EnemyDamage);
            copy.EnemyHealth = Math.Max(1, EnemyHealth);
            copy.EnemyAttackInterval = Math.Max(0.001m, EnemyAttackInterval);
            copy.EnemyWarningTime = Math.Max(0.1m, EnemyWarningTime);
            return copy;
        }
    }
}
