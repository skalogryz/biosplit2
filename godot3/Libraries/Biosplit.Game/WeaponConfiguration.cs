using System;
using System.IO;
using Biosplit.Ini;

namespace Biosplit.Game
{

    // Maps configuration values to engine-independent gameplay settings.
    public static class WeaponConfiguration
    {
        public static void Load(string executableDirectory, GameSettings game, Action<string> warning = null)
        {
            string directory = Path.Combine(executableDirectory, "cfg");
            if (!Directory.Exists(directory)) return;
            string path = Path.Combine(directory, "weapon.ini");
            if (!System.IO.File.Exists(path)) path = Path.Combine(directory, "weapons.ini");
            if (!System.IO.File.Exists(path)) return;

            try
            {
                var ini = IniDocument.Load(path);
                game.PunchDamage = ini.GetInt("knife", "damage", game.PunchDamage);
                game.RagePerPunch = ini.GetInt("knife", "ragebonus", game.RagePerPunch);
                int knifeRage = ini.GetInt("knife", "rage", game.PunchCost);
                if (knifeRage >= 0) game.PunchCost = knifeRage;
                decimal knifeStamina = ini.GetDecimal("knife", "stamina", game.PunchStaminaCost);
                if (knifeStamina >= 0m) game.PunchStaminaCost = knifeStamina;
                decimal handgunStamina = ini.GetDecimal("handgun", "stamina", game.ShotStaminaCost);
                if (handgunStamina >= 0m) game.ShotStaminaCost = handgunStamina;
                game.ShotDamage = ini.GetInt("handgun", "damage", game.ShotDamage);
                game.ShotCost = ini.GetInt("handgun", "rage", game.ShotCost);
                int knifeCooldown = ini.GetInt("knife", "cooldown", game.PunchCooldownMs);
                if (knifeCooldown >= 0) game.PunchCooldownMs = knifeCooldown;
                int handgunCooldown = ini.GetInt("handgun", "cooldown", game.ShotCooldownMs);
                if (handgunCooldown >= 0) game.ShotCooldownMs = handgunCooldown;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                warning?.Invoke("Cannot read " + Path.GetFileName(path) + ": " + error.Message);
            }
        }
    }
}
