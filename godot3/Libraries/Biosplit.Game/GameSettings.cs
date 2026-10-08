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

        public int MaxHealth { get; set; } = 100;
        public int MaxRage { get; set; } = 100;
        public decimal MaxStamina { get; set; } = 100m;
        public decimal StaminaGrow { get; set; } = 2m;
        public decimal DodgeStamina { get; set; } = 10m;
        public int DodgeTimeMs { get; set; } = 3000;
        public int RagePerPunch { get => weapons["knife"].RageBonus; set => weapons["knife"].RageBonus = value; }
        public int PunchCost { get => weapons["knife"].RageCost; set => weapons["knife"].RageCost = value; }
        public int ShotCost { get => weapons["handgun"].RageCost; set => weapons["handgun"].RageCost = value; }
        public decimal PunchStaminaCost { get => weapons["knife"].StaminaCost; set => weapons["knife"].StaminaCost = value; }
        public decimal ShotStaminaCost { get => weapons["handgun"].StaminaCost; set => weapons["handgun"].StaminaCost = value; }
        public WeaponType PunchWeaponType { get => weapons["knife"].Type; set => weapons["knife"].Type = value; }
        public WeaponType ShotWeaponType { get => weapons["handgun"].Type; set => weapons["handgun"].Type = value; }
        public int PunchDamage { get => weapons["knife"].Damage; set => weapons["knife"].Damage = value; }
        public int ShotDamage { get => weapons["handgun"].Damage; set => weapons["handgun"].Damage = value; }
        public int PunchCooldownMs { get => weapons["knife"].CooldownMs; set => weapons["knife"].CooldownMs = value; }
        public int ShotCooldownMs { get => weapons["handgun"].CooldownMs; set => weapons["handgun"].CooldownMs = value; }
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
