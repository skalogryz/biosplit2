using System;

namespace Biosplit.Game
{
    // Time is supplied by the host; all values are seconds unless named *Ms.
    public sealed class GameSession
    {
        private readonly GameSettings settings;
        private decimal cooldown, dodgeRemaining, enemyClock, respawnRemaining;
        private bool knifeCooldown;
        public event EventHandler<GameEvent> Changed;

        public GameSession(GameSettings settings)
        {
            this.settings = (settings ?? throw new ArgumentNullException(nameof(settings))).NormalizedCopy();
            Health = this.settings.MaxHealth;
            EnemyHealth = this.settings.EnemyHealth;
        }

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
        public int RagePerPunch => settings.RagePerPunch;
        public int ShotCost => settings.ShotCost;
        public WeaponType PunchWeaponType => settings.PunchWeaponType;
        public WeaponType ShotWeaponType => settings.ShotWeaponType;
        public decimal CooldownRemaining => cooldown;
        public decimal DodgeRemaining => dodgeRemaining;
        public bool EnemyWarning => EnemyHealth > 0 && enemyClock >= settings.EnemyAttackInterval - settings.EnemyWarningTime;
        private bool CanUseWeapon => !Paused && !Blocking && EnemyHealth > 0 && cooldown <= 0;
        public bool CanPunch => CanUseWeapon && !Dodging && Rage >= settings.PunchCost && Stamina >= settings.PunchStaminaCost;
        public bool CanShoot => CanUseWeapon && Rage >= settings.ShotCost && Stamina >= settings.ShotStaminaCost;
        public bool CanDodge => !Paused && !Blocking && !Dodging && EnemyHealth > 0 && Stamina >= settings.DodgeStamina;
        public bool CanHeal => InventoryOpen && !GameOver && Kits > 0 && Health < MaxHealth;

        public void Tick(decimal seconds)
        {
            if (seconds <= 0m || Paused) return;
            // Regenerate only for the part of this update after the knife cooldown expires.
            AccumulateStamina(knifeCooldown ? Math.Max(0m, seconds - cooldown) : seconds);
            cooldown = Math.Max(0m, cooldown - seconds);
            dodgeRemaining = Math.Max(0m, dodgeRemaining - seconds);
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
            if (GameOver) Emit(GameEventKind.GameOver);
        }

        public bool Punch()
        {
            if (!CanPunch) return false;
            Stamina -= settings.PunchStaminaCost;
            Rage = (int)Math.Min(MaxRage, (long)Rage - settings.PunchCost + settings.RagePerPunch);
            cooldown = settings.PunchCooldownMs / 1000m;
            knifeCooldown = true;
            Emit(GameEventKind.Punch);
            HurtEnemy(settings.PunchDamage);
            return true;
        }

        public bool Shoot()
        {
            if (!CanShoot) return false;
            Stamina -= settings.ShotStaminaCost;
            Rage -= settings.ShotCost;
            cooldown = settings.ShotCooldownMs / 1000m;
            knifeCooldown = false;
            Emit(GameEventKind.Shot);
            HurtEnemy(settings.ShotDamage);
            return true;
        }

        public bool Dodge()
        {
            if (!CanDodge) return false;
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

        private void Emit(GameEventKind kind, int damage = 0, bool blocked = false, bool dodged = false)
            => Changed?.Invoke(this, new GameEvent(kind, damage, blocked, dodged));
    }
}
