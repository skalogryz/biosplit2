using System;

namespace Biosplit.Game
{
    public enum GameEventKind
    {
        Punch, Shot, Dodge, BlockStarted, BlockEnded, EnemyAttacked,
        EnemyHurt, EnemyDefeated, EnemySpawned, InventoryChanged, Healed, GameOver
    }

    public sealed class GameEvent : EventArgs
    {
        public GameEventKind Kind { get; }
        public int Damage { get; }
        public bool Blocked { get; }
        public bool Dodged { get; }

        public GameEvent(GameEventKind kind, int damage = 0, bool blocked = false, bool dodged = false)
        {
            Kind = kind;
            Damage = damage;
            Blocked = blocked;
            Dodged = dodged;
        }
    }
}
