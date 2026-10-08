using System;
using System.Collections.Generic;

namespace Biosplit.Game
{
    // Time is supplied by the host; all values are seconds unless named *Ms.
    public sealed class GameSession
    {
        private readonly GameSettings settings;
        private decimal dodgeRemaining, enemyClock, respawnRemaining;
        private readonly List<Character> characters = new List<Character>();
        public IReadOnlyList<Character> Characters { get; }
        private sealed class Attack
        {
            public WeaponItem Item;
            public WeaponSettings Definition;
            public decimal Remaining;
        }
        private Attack activeAttack;
        public bool IsAttacking => activeAttack != null;
        public WeaponItem ActiveWeapon => activeAttack?.Item;
        public decimal AttackRemaining => activeAttack?.Remaining ?? 0m;
        public event EventHandler<GameEvent> Changed;

        public GameSession(GameSettings settings)
        {
            this.settings = (settings ?? throw new ArgumentNullException(nameof(settings))).NormalizedCopy();
            Health = this.settings.MaxHealth;
            EnemyHealth = this.settings.EnemyHealth;
            Player = new Character("hero");
            characters.Add(Player);
            Characters = characters.AsReadOnly();
            Player.Inventory.Weapon1 = new WeaponItem(this.settings.Weapons["knife"]);
            Player.Inventory.Weapon2 = new WeaponItem(this.settings.Weapons["handgun"]);
        }

        public Character Player { get; }
        public int Health { get; private set; }
        public int Rage { get; private set; }
        public decimal Stamina { get; private set; }
        public int EnemyHealth { get; private set; }
        public int Wave { get; private set; } = 1;
        public long Coins { get; private set; }
        public int Kits { get; private set; } = 2;
        public bool Blocking { get; private set; }
        public bool Dodging => dodgeRemaining > 0m;
        public bool GameOver => Health == 0;
        public bool InventoryOpen { get; private set; }
        public bool Paused => InventoryOpen || GameOver;
        public int MaxHealth => settings.MaxHealth;
        public int MaxRage => settings.MaxRage;
        public decimal MaxStamina => settings.MaxStamina;
        public int EnemyMaxHealth => settings.EnemyHealth;
        public int RagePointStep => Math.Max(1, MaxRage / 10);
        public int RagePerPunch => Math.Max(0, Player.Inventory.Weapon1?.Definition.RageBonus ?? 0);
        public int ShotCost => Math.Max(0, Player.Inventory.Weapon2?.Definition.RageCost ?? 0);
        public WeaponType PunchWeaponType => Player.Inventory.Weapon1?.Definition.Type ?? WeaponType.None;
        public WeaponType ShotWeaponType => Player.Inventory.Weapon2?.Definition.Type ?? WeaponType.None;
        public decimal DodgeRemaining => dodgeRemaining;
        public bool EnemyWarning => EnemyHealth > 0 && enemyClock >= settings.EnemyAttackInterval - settings.EnemyWarningTime;
        public void AddCharacter(Character character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (!characters.Contains(character)) characters.Add(character);
        }
        public bool CanPunch => CanStartAttack(Player.Inventory.Weapon1);
        public bool CanShoot => CanStartAttack(Player.Inventory.Weapon2);
        public bool CanStartAttack(WeaponItem weapon) => GetAttackStartResult(weapon) == AttackResult.Success;
        public AttackResult GetAttackStartResult(WeaponItem weapon)
        {
            if (weapon == null) return AttackResult.NoWeapon;
            var definition = weapon.Definition;
            if (definition.Type != WeaponType.Melee && definition.Type != WeaponType.Firearm)
                return AttackResult.UnsupportedWeaponType;
            if (GameOver) return AttackResult.GameOver;
            if (InventoryOpen) return AttackResult.InventoryOpen;
            if (Blocking) return AttackResult.Blocking;
            if (EnemyHealth <= 0) return AttackResult.NoEnemy;
            if (IsAttacking) return AttackResult.AttackInProgress;
            if (weapon.Cooldown > 0m) return AttackResult.WeaponOnCooldown;
            if (Dodging && definition.Type == WeaponType.Melee) return AttackResult.Dodging;
            if (Rage < Math.Max(0, definition.RageCost)) return AttackResult.InsufficientRage;
            if (Stamina < Math.Max(0m, definition.StaminaCost)) return AttackResult.InsufficientStamina;
            return AttackResult.Success;
        }
        public bool CanDodge => !Paused && !Blocking && !Dodging && EnemyHealth > 0 && Stamina >= settings.DodgeStamina;
        public bool CanHeal => InventoryOpen && !GameOver && Kits > 0 && Health < MaxHealth;

        public void Tick(decimal seconds)
        {
            if (seconds <= 0m || Paused) return;
            var attack = activeAttack;
            if (attack != null && attack.Remaining <= seconds)
            {
                decimal elapsed = attack.Remaining;
                AdvanceItemsAndStamina(elapsed);
                dodgeRemaining = Math.Max(0m, dodgeRemaining - elapsed);
                CompleteAttack(attack);
                decimal rest = seconds - elapsed;
                AdvanceItemsAndStamina(rest);
                dodgeRemaining = Math.Max(0m, dodgeRemaining - rest);
                if (EnemyHealth == 0) return;
            }
            else
            {
                AdvanceItemsAndStamina(seconds);
                dodgeRemaining = Math.Max(0m, dodgeRemaining - seconds);
                if (attack != null) attack.Remaining -= seconds;
            }
            if (EnemyHealth <= 0)
            {
                respawnRemaining -= seconds;
                if (respawnRemaining <= 0m)
                {
                    Wave++;
                    EnemyHealth = settings.EnemyHealth;
                    enemyClock = 0m;
                    Emit(GameEventKind.EnemySpawned);
                }
                return;
            }
            // Preserve the existing one-attack-per-update timing policy.
            if (seconds < settings.EnemyAttackInterval - enemyClock)
            {
                enemyClock += seconds;
                return;
            }
            enemyClock = 0m;
            bool dodged = Dodging;
            bool blocked = !dodged && Blocking;
            int damage = dodged || blocked ? 0 : settings.EnemyDamage;
            Health = Math.Max(0, Health - damage);
            Emit(GameEventKind.EnemyAttacked, damage, blocked, dodged);
            if (GameOver) { CancelAttack(); Emit(GameEventKind.GameOver); }
        }

        public bool Punch() => StartAttack(Player.Inventory.Weapon1) == AttackResult.Success;
        public bool Shoot() => StartAttack(Player.Inventory.Weapon2) == AttackResult.Success;

        public AttackResult StartAttack(WeaponItem weapon)
        {
            var result = GetAttackStartResult(weapon);
            if (result != AttackResult.Success) return result;
            var definition = weapon.Definition.Copy();
            Stamina -= Math.Max(0m, definition.StaminaCost);
            Rage -= Math.Max(0, definition.RageCost);
            var attack = new Attack
            {
                Item = weapon,
                Definition = definition,
                Remaining = Math.Max(0, definition.AttackDurationMs) / 1000m
            };
            activeAttack = attack;
            Emit(GameEventKind.AttackStarted, weapon: weapon);
            // An event subscriber may have cancelled this attack already.
            if (activeAttack != attack) return AttackResult.Success;
            Emit(definition.Type == WeaponType.Melee ? GameEventKind.Punch : GameEventKind.Shot, weapon: weapon);
            if (activeAttack == attack && attack.Remaining == 0m) CompleteAttack(attack);
            return AttackResult.Success;
        }

        public bool CancelAttack()
        {
            var attack = activeAttack;
            if (attack == null) return false;
            activeAttack = null;
            attack.Item.BeginCooldown(attack.Definition.CooldownMs);
            Emit(GameEventKind.AttackCancelled, weapon: attack.Item);
            return true;
        }

        private void CompleteAttack(Attack attack)
        {
            if (activeAttack != attack) return;
            activeAttack = null;
            attack.Item.BeginCooldown(attack.Definition.CooldownMs);
            int damage = Math.Min(EnemyHealth, Math.Max(1, attack.Definition.Damage));
            Rage = (int)Math.Min(MaxRage, (long)Rage + Math.Max(0, attack.Definition.RageBonus));
            HurtEnemy(Math.Max(1, attack.Definition.Damage));
            Emit(GameEventKind.AttackCompleted, damage, weapon: attack.Item);
        }
        public bool Dodge()
        {
            if (!CanDodge) return false;
            if (activeAttack?.Definition.Type == WeaponType.Melee) CancelAttack();
            Stamina -= settings.DodgeStamina;
            dodgeRemaining = settings.DodgeTimeMs / 1000m;
            Emit(GameEventKind.Dodge);
            return true;
        }

        public void SetBlocking(bool held)
        {
            bool next = held && !GameOver;
            if (next == Blocking) return;
            Blocking = next;
            if (Blocking) CancelAttack();
            Emit(next ? GameEventKind.BlockStarted : GameEventKind.BlockEnded);
        }

        public void SetInventoryOpen(bool open)
        {
            if (GameOver || InventoryOpen == open) return;
            InventoryOpen = open;
            Emit(GameEventKind.InventoryChanged);
        }

        public bool UseKit()
        {
            if (!CanHeal) return false;
            Kits--;
            Health = (int)Math.Min(MaxHealth, (long)Health + 40);
            Emit(GameEventKind.Healed);
            return true;
        }

        private void HurtEnemy(int damage)
        {
            EnemyHealth = Math.Max(0, EnemyHealth - damage);
            Emit(GameEventKind.EnemyHurt, damage);
            if (EnemyHealth != 0) return;
            Coins += 10;
            if (Wave % 3 == 0) Kits++;
            respawnRemaining = 0.9m;
            enemyClock = 0m;
            Emit(GameEventKind.EnemyDefeated);
        }

        private IEnumerable<WeaponItem> CarriedWeapons()
        {
            var seen = new HashSet<WeaponItem>();
            foreach (var character in characters)
            {
                var first = character.Inventory.Weapon1;
                var second = character.Inventory.Weapon2;
                if (first != null && seen.Add(first)) yield return first;
                if (second != null && seen.Add(second)) yield return second;
            }
        }

        private void AdvanceItemsAndStamina(decimal seconds)
        {
            if (seconds <= 0m) return;
            decimal meleeCooldown = 0m;
            var first = Player.Inventory.Weapon1;
            var second = Player.Inventory.Weapon2;
            if (first?.Definition.Type == WeaponType.Melee) meleeCooldown = first.Cooldown;
            if (second?.Definition.Type == WeaponType.Melee) meleeCooldown = Math.Max(meleeCooldown, second.Cooldown);
            AccumulateStamina(Math.Max(0m, seconds - meleeCooldown));
            foreach (var weapon in CarriedWeapons()) weapon.TickCooldown(seconds);
        }
        private void AccumulateStamina(decimal seconds)
        {
            if (seconds <= 0m || Blocking || settings.StaminaGrow <= 0m || Stamina >= MaxStamina) return;
            decimal remaining = MaxStamina - Stamina;
            if (seconds >= 1m && settings.StaminaGrow >= remaining / seconds)
                Stamina = MaxStamina;
            else if (seconds < 1m && settings.StaminaGrow * seconds >= remaining)
                Stamina = MaxStamina;
            else
                Stamina += settings.StaminaGrow * seconds;
        }

        private void Emit(GameEventKind kind, int damage = 0, bool blocked = false, bool dodged = false, WeaponItem weapon = null)
            => Changed?.Invoke(this, new GameEvent(kind, damage, blocked, dodged, weapon));
    }
}
