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
            public bool EnemyAttacker;
            public bool Approaching;
            public decimal MovementElapsed;
            public decimal MovementStart;
            public decimal MovementTarget;
            public WeaponSettings Definition;
            public decimal Remaining;
            public decimal UntilDamage;
            public bool DamageApplied;
            public bool CancellationRequested;
            public int DamageDealt;
        }
        private Attack activeAttack;
        private bool separating;
        private Attack separatingAttack;
        private decimal separationElapsed, separationHeroStart, separationEnemyStart;
        public bool IsApproaching => activeAttack?.Approaching ?? false;
        public bool IsSeparating => separating;
        public decimal HeroPos { get; private set; }
        public decimal EnemyMeleePos { get; private set; }
        public decimal HeroInitialPos => settings.HeroPos;
        public decimal EnemyInitialPos => settings.EnemyMeleePos;
        public bool IsAttacking => activeAttack != null;
        public bool IsAttackCancellationRequested => activeAttack?.CancellationRequested ?? false;
        public WeaponItem ActiveWeapon => activeAttack?.Item;
        public decimal AttackRemaining => activeAttack?.Remaining ?? 0m;
        public StrikeSettings CurrentStrike => activeAttack == null || activeAttack.Approaching ? null
            : activeAttack.Definition.Striker[activeAttack.Item.CurrentStrikeIndex].Copy();
        public event EventHandler<GameEvent> Changed;

        public GameSession(GameSettings settings)
        {
            this.settings = (settings ?? throw new ArgumentNullException(nameof(settings))).NormalizedCopy();
            Player = new Character("hero");
            characters.Add(Player);
            Enemy = new Character("enemy");
            characters.Add(Enemy);
            Health = this.settings.MaxHealth;
            EnemyHealth = this.settings.EnemyHealth;
            Enemy.Inventory.Weapon1 = new WeaponItem(new WeaponSettings("enemy_melee") { Type = WeaponType.Melee, Damage = this.settings.EnemyDamage });
            HeroPos = this.settings.HeroPos;
            EnemyMeleePos = this.settings.EnemyMeleePos;
            Characters = characters.AsReadOnly();
            Player.Inventory.Weapon1 = new WeaponItem(this.settings.Weapons["knife"]);
            Player.Inventory.Weapon2 = new WeaponItem(this.settings.Weapons["handgun"]);
        }

        public Character Player { get; }
        public Character Enemy { get; }
        public int Health
        {
            get => Player.health;
            private set => Player.health = value;
        }
        public int Rage
        {
            get => Player.rage;
            private set => Player.rage = value;
        }
        public decimal Stamina
        {
            get => Player.stamina;
            private set => Player.stamina = value;
        }
        public int EnemyHealth
        {
            get => Enemy.health;
            private set => Enemy.health = value;
        }
        public int Wave { get; private set; } = 1;
        public long Coins { get; private set; }
        public int Kits { get; private set; } = 2;
        public bool Blocking
        {
            get => Player.blocking;
            private set => Player.blocking = value;
        }
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
        public AttackResult GetAttackStartResult(WeaponItem weapon) => GetAttackStartResult(Player, weapon);
        public AttackResult GetAttackStartResult(Character attacker, WeaponItem weapon)
        {
            if (attacker != Player && attacker != Enemy) return AttackResult.UnsupportedAttacker;
            bool enemyAttacker = attacker == Enemy;
            if (weapon == null) return AttackResult.NoWeapon;
            var definition = weapon.Definition;
            if (definition.Type != WeaponType.Melee && definition.Type != WeaponType.Firearm)
                return AttackResult.UnsupportedWeaponType;
            if (GameOver) return AttackResult.GameOver;
            if (InventoryOpen) return AttackResult.InventoryOpen;
            if (!enemyAttacker && Blocking) return AttackResult.Blocking;
            if (EnemyHealth <= 0) return AttackResult.NoEnemy;
            if (IsAttacking) return AttackResult.AttackInProgress;
            if (separating && definition.Type == WeaponType.Melee) return AttackResult.Separating;
            if (weapon.Cooldown > 0m) return AttackResult.WeaponOnCooldown;
            if (!enemyAttacker && Dodging && definition.Type == WeaponType.Melee) return AttackResult.Dodging;
            if (!enemyAttacker && Rage < Math.Max(0, definition.RageCost)) return AttackResult.InsufficientRage;
            if (!enemyAttacker && Stamina < Math.Max(0m, definition.StaminaCost)) return AttackResult.InsufficientStamina;
            return AttackResult.Success;
        }
        public bool CanDodge => !Paused && !Blocking && !Dodging && EnemyHealth > 0 && Stamina >= settings.DodgeStamina;
        public bool CanHeal => InventoryOpen && !GameOver && Kits > 0 && Health < MaxHealth;

        public void Tick(decimal seconds)
        {
            if (seconds <= 0m || InventoryOpen) return;
            if (GameOver) { AdvanceSeparation(seconds); return; }
            bool killedByAttack = AdvanceAttackAndItems(seconds);
            if (killedByAttack) return;
            if (EnemyHealth <= 0)
            {
                if (IsAttacking) return;
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
            if (IsAttacking || separating) return;
            if (seconds < settings.EnemyAttackInterval - enemyClock)
            {
                enemyClock += seconds;
                return;
            }
            enemyClock = 0m;
            StartAttack(Enemy, Enemy.Inventory.Weapon1);
        }
        public bool Punch() => StartAttack(Player.Inventory.Weapon1) == AttackResult.Success;
        public bool Shoot() => StartAttack(Player.Inventory.Weapon2) == AttackResult.Success;

        public AttackResult StartAttack(WeaponItem weapon) => StartAttack(Player, weapon);
        public AttackResult StartAttack(Character attacker, WeaponItem weapon)
        {
            var result = GetAttackStartResult(attacker, weapon);
            if (result != AttackResult.Success) return result;
            var definition = weapon.Definition.Copy();
            if (attacker == Player)
            {
                Stamina -= Math.Max(0m, definition.StaminaCost);
                Rage -= Math.Max(0, definition.RageCost);
            }
            var attack = new Attack
            {
                Item = weapon,
                Definition = definition,
                EnemyAttacker = attacker == Enemy,
                Approaching = definition.Type == WeaponType.Melee,
                MovementStart = attacker == Enemy ? EnemyMeleePos : HeroPos,
                MovementTarget = attacker == Enemy ? HeroPos + settings.EnemyMeleeSize : EnemyMeleePos - settings.EnemyMeleeSize
            };
            activeAttack = attack;
            weapon.CurrentStrikeIndex = 0;
            Emit(GameEventKind.AttackStarted, weapon: weapon);
            // Event subscribers may request cancellation; the first strike still runs.
            if (activeAttack != attack) return AttackResult.Success;
            if (attack.Approaching) EmitAttackEvent(GameEventKind.ApproachStarted, attack);
            if (activeAttack != attack) return AttackResult.Success;
            if (!attack.Approaching) BeginStrike(attack);
            AdvanceAttackAndItems(0m);
            return AttackResult.Success;
        }

        // Soft cancellation finishes the current strike; interruption stops it immediately.
        public bool CancelAttack(bool isInterrupt = false)
        {
            var attack = activeAttack;
            if (attack == null || (!isInterrupt && attack.CancellationRequested)) return false;
            attack.CancellationRequested = true;
            if (isInterrupt)
            {
                activeAttack = null;
                attack.Item.CurrentStrikeIndex = -1;
                attack.Item.BeginCooldown(attack.Definition.CooldownMs);
                BeginSeparation(attack);
            }
            Emit(GameEventKind.AttackCancelled, weapon: attack.Item);
            if (isInterrupt) Emit(GameEventKind.AttackCompleted, attack.DamageDealt, weapon: attack.Item);
            return true;
        }
        private void CompleteAttack(Attack attack)
        {
            if (activeAttack != attack) return;
            activeAttack = null;
            attack.Item.CurrentStrikeIndex = -1;
            attack.Item.BeginCooldown(attack.Definition.CooldownMs);
                BeginSeparation(attack);
            Emit(GameEventKind.AttackCompleted, attack.DamageDealt, weapon: attack.Item);
        }
        private static decimal StrikeDuration(StrikeSettings strike)
        {
            decimal total = (decimal)Math.Max(0, strike.WindupMs) + Math.Max(0, strike.DamageDurationMs) + Math.Max(0, strike.RecoveryMs);
            return total / 1000m;
        }

        private void BeginStrike(Attack attack)
        {
            var strike = attack.Definition.Striker[attack.Item.CurrentStrikeIndex];
            attack.Remaining = StrikeDuration(strike);
            attack.UntilDamage = Math.Max(0, strike.WindupMs) / 1000m;
            attack.DamageApplied = false;
            EmitAttackEvent(GameEventKind.StrikeStarted, attack);
            if (activeAttack != attack) return;
            if (!attack.EnemyAttacker) Emit(attack.Definition.Type == WeaponType.Melee ? GameEventKind.Punch : GameEventKind.Shot, weapon: attack.Item);
        }

        private void FinishStrike(Attack attack)
        {
            if (activeAttack != attack) return;
            if (!attack.CancellationRequested && EnemyHealth > 0 && attack.Item.CurrentStrikeIndex + 1 < attack.Definition.Striker.Length)
            {
                attack.Item.CurrentStrikeIndex++;
                BeginStrike(attack);
            }
            else CompleteAttack(attack);
        }

        private void ApplyAttackDamage(Attack attack)
        {
            if (activeAttack != attack || attack.DamageApplied) return;
            attack.DamageApplied = true;
            var strike = attack.Definition.Striker[attack.Item.CurrentStrikeIndex];
            int damage = strike.CalculateDamage(attack.Definition.Damage);
            if (attack.EnemyAttacker)
            {
                bool dodged = Dodging;
                bool blocked = !dodged && Blocking;
                int applied = dodged || blocked ? 0 : Math.Min(Health, damage);
                Health -= applied;
                attack.DamageDealt = (int)Math.Min(int.MaxValue, (long)attack.DamageDealt + applied);
                Emit(GameEventKind.EnemyAttacked, applied, blocked, dodged);
                if (GameOver) { CancelAttack(isInterrupt: true); Emit(GameEventKind.GameOver); }
                return;
            }
            attack.DamageDealt = (int)Math.Min(int.MaxValue, (long)attack.DamageDealt + Math.Min(EnemyHealth, damage));
            Rage = (int)Math.Min(MaxRage, (long)Rage + Math.Max(0, attack.Definition.RageBonus));
            if (damage > 0) HurtEnemy(damage);
        }

        private bool AdvanceAttackAndItems(decimal seconds)
        {
            bool killed = false;
            while (true)
            {
                var attack = activeAttack;
                if (attack == null)
                {
                    AdvanceItemsAndStamina(seconds);
                    dodgeRemaining = Math.Max(0m, dodgeRemaining - seconds);
                    return killed;
                }
                if (attack.Approaching)
                {
                    decimal duration = settings.MovementDurationMs / 1000m;
                    decimal approachElapsed = attack.MovementStart == attack.MovementTarget ? 0m
                        : Math.Min(seconds, duration - attack.MovementElapsed);
                    AdvanceItemsAndStamina(approachElapsed);
                    dodgeRemaining = Math.Max(0m, dodgeRemaining - approachElapsed);
                    seconds -= approachElapsed;
                    attack.MovementElapsed += approachElapsed;
                    decimal progress = attack.MovementStart == attack.MovementTarget ? 1m : MovementProgress(attack.MovementElapsed);
                    decimal position = attack.MovementStart + (attack.MovementTarget - attack.MovementStart) * progress;
                    if (attack.EnemyAttacker) EnemyMeleePos = position;
                    else HeroPos = position;
                    if (progress < 1m) return killed;
                    attack.Approaching = false;
                    BeginStrike(attack);
                    continue;
                }
                decimal boundary = attack.DamageApplied ? attack.Remaining : attack.UntilDamage;
                if (seconds == 0m && boundary > 0m) return killed;
                decimal elapsed = Math.Min(seconds, boundary);
                AdvanceItemsAndStamina(elapsed);
                dodgeRemaining = Math.Max(0m, dodgeRemaining - elapsed);
                seconds -= elapsed;
                attack.Remaining = Math.Max(0m, attack.Remaining - elapsed);
                attack.UntilDamage = Math.Max(0m, attack.UntilDamage - elapsed);
                if (!attack.DamageApplied && attack.UntilDamage == 0m)
                {
                    int healthBefore = EnemyHealth;
                    ApplyAttackDamage(attack);
                    killed |= healthBefore > 0 && EnemyHealth == 0;
                }
                if (activeAttack == attack && attack.Remaining == 0m) FinishStrike(attack);
            }
        }
        public bool Dodge()
        {
            if (!CanDodge) return false;
            if (activeAttack != null && !activeAttack.EnemyAttacker && activeAttack.Definition.Type == WeaponType.Melee) CancelAttack();
            Stamina -= settings.DodgeStamina;
            dodgeRemaining = settings.DodgeTimeMs / 1000m;
            Emit(GameEventKind.Dodge);
            return true;
        }

        public void SetBlocking(bool held)
        {
            bool next = held && !GameOver && !Dodging;
            if (next == Blocking) return;
            Blocking = next;
            if (Blocking && activeAttack != null && !activeAttack.EnemyAttacker) CancelAttack();
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
            AdvanceSeparation(seconds);
            decimal meleeCooldown = 0m;
            var first = Player.Inventory.Weapon1;
            var second = Player.Inventory.Weapon2;
            if (first?.Definition.Type == WeaponType.Melee) meleeCooldown = first.Cooldown;
            if (second?.Definition.Type == WeaponType.Melee) meleeCooldown = Math.Max(meleeCooldown, second.Cooldown);
            AccumulateStamina(Math.Max(0m, seconds - meleeCooldown));
            foreach (var weapon in CarriedWeapons()) weapon.TickCooldown(seconds);
        }
        private void EmitAttackEvent(GameEventKind kind, Attack attack)
            => Changed?.Invoke(this, new GameEvent(kind, weapon: attack.Item,
                attacker: attack.EnemyAttacker ? Enemy : Player,
                strikeIndex: attack.Item.CurrentStrikeIndex,
                strikeName: attack.Item.CurrentStrikeIndex < 0 ? null : attack.Definition.Striker[attack.Item.CurrentStrikeIndex].Name));

        private void BeginSeparation(Attack attack)
        {
            if (attack.Definition.Type != WeaponType.Melee || separating) return;
            separating = HeroPos != settings.HeroPos || EnemyMeleePos != settings.EnemyMeleePos;
            if (!separating) return;
            separatingAttack = attack;
            separationElapsed = 0m;
            separationHeroStart = HeroPos;
            separationEnemyStart = EnemyMeleePos;
            EmitAttackEvent(GameEventKind.SeparationStarted, attack);
        }

        private void AdvanceSeparation(decimal seconds)
        {
            if (!separating) return;
            separationElapsed += seconds;
            decimal progress = MovementProgress(separationElapsed);
            HeroPos = separationHeroStart + (settings.HeroPos - separationHeroStart) * progress;
            EnemyMeleePos = separationEnemyStart + (settings.EnemyMeleePos - separationEnemyStart) * progress;
            separating = HeroPos != settings.HeroPos || EnemyMeleePos != settings.EnemyMeleePos;
            if (!separating)
            {
                var finished = separatingAttack;
                separatingAttack = null;
                EmitAttackEvent(GameEventKind.SeparationCompleted, finished);
            }
        }
        // Integral of a trapezoidal velocity profile: accelerate, cruise, decelerate.
        private decimal MovementProgress(decimal seconds)
        {
            decimal duration = settings.MovementDurationMs;
            decimal time = Math.Max(0m, seconds * 1000m);
            if (time >= duration) return 1m;
            decimal acceleration = settings.MovementAccelerationMs;
            decimal deceleration = settings.MovementDecelerationMs;
            decimal area = duration - (acceleration + deceleration) / 2m;
            if (acceleration > 0m && time < acceleration)
                return time * time / (2m * acceleration * area);
            if (deceleration > 0m && time > duration - deceleration)
            {
                decimal remaining = duration - time;
                return 1m - remaining * remaining / (2m * deceleration * area);
            }
            return (time - acceleration / 2m) / area;
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
