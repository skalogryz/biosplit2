using System;

namespace Biosplit.Game
{
    // A concrete item owns its ammo, while its definition comes from the weapon lookup.
    public sealed class WeaponItem
    {
        public WeaponItem(WeaponSettings definition, int ammo = 0)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Ammo = ammo;
        }

        public WeaponSettings Definition { get; }
        public int Ammo { get; set; }
        // Remaining cooldown in seconds, independent for each physical item.
        public decimal Cooldown { get; private set; }
        internal void BeginCooldown(int milliseconds) => Cooldown = Math.Max(0, milliseconds) / 1000m;
        internal void TickCooldown(decimal seconds) => Cooldown = Math.Max(0m, Cooldown - seconds);
    }
}
