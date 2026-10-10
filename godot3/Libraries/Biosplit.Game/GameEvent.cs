using System;

namespace Biosplit.Game
{
    public enum GameEventKind
    {
        Punch, Shot, Dodge, BlockStarted, BlockEnded, EnemyAttacked,
        EnemyHurt, EnemyDefeated, EnemySpawned, InventoryChanged, Healed, GameOver, AttackStarted, AttackCancelled, AttackCompleted, ApproachStarted, SeparationStarted, SeparationCompleted, StrikeStarted
    }

    public sealed class GameEvent : EventArgs
    {
        public GameEventKind Kind { get; }
        public int Damage { get; }
        public WeaponItem Weapon { get; }
        public Character Attacker { get; }
        public int StrikeIndex { get; }
        public string StrikeName { get; }
        public bool Blocked { get; }
        public bool Dodged { get; }

        public GameEvent(GameEventKind kind, int damage = 0, bool blocked = false, bool dodged = false, WeaponItem weapon = null, Character attacker = null, int strikeIndex = -1, string strikeName = null)
        {
            Kind = kind;
            Weapon = weapon;
            Attacker = attacker;
            StrikeIndex = strikeIndex;
            StrikeName = strikeName;
            Damage = damage;
            Blocked = blocked;
            Dodged = dodged;
        }
    }
}
