namespace Biosplit.Game
{
    public sealed class Character
    {
        public Character(string name) { Name = name; }
        public EnemySettings EnemySettings { get; private set; }

        public static Character AllocEnemy(EnemySettings settings)
        {
            if (settings == null) throw new System.ArgumentNullException(nameof(settings));
            return new Character(settings.Name)
            {
                EnemySettings = settings,
                health = settings.Health
            };
        }
        public int health;
        public decimal stamina;
        public int rage;
        public bool blocking;
        public string Name { get; }
        public CharacterInventory Inventory { get; } = new CharacterInventory();
    }
}
