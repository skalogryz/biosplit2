using System;

namespace Biosplit.Game
{
    public static class GameConfiguration
    {
        // The host supplies the executable directory; this library knows nothing about Godot.
        public static void Load(string executableDirectory, GameSettings settings, Action<string> warning = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            WeaponConfiguration.Load(executableDirectory, settings, warning);
            HeroConfiguration.Load(executableDirectory, settings, warning);
            EnemyConfiguration.Load(executableDirectory, settings, warning);
        }
    }
}
