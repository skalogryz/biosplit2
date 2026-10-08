namespace Biosplit.Game
{
    public sealed class Character
    {
        public Character(string name) { Name = name; }

        public string Name { get; }
        public CharacterInventory Inventory { get; } = new CharacterInventory();
    }
}
