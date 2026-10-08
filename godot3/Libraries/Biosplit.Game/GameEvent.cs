using System;

namespace Biosplit.Game
{
    public enum GameEventKind
    {
        Punch, Shot, Dodge, BlockStarted, BlockEnded, EnemyAttacked,
        EnemyHurt, EnemyDefeated, EnemySpawned, InventoryChanged, Healed, GameOver, AttackStarted, AttackCancelled, AttackCompleted
    }

    public sealed class GameEvent : EventArgs
    {
        public GameEventKind Kind { get; }
        public int Damage { get; }
        public WeaponItem Weapon { get; }
        public bool Blocked { get; }
        public bool Dodged { get; }

        public GameEvent(GameEventKind kind, int damage = 0, bool blocked = false, bool dodged = false, WeaponItem weapon = null)
        {
            Kind = kind;
            Weapon = weapon;
            Damage = damage;
            Blocked = blocked;
            Dodged = dodged;
        }
    }
}
