using System;
using System.IO;
using System.Collections.Generic;
using Biosplit.Ini;

namespace Biosplit.Game
{

    public static class EnemyConfiguration
    {
        public static void Load(string executableDirectory, GameSettings game, Action<string> warning = null)
        {
            string directory = Path.Combine(executableDirectory, "cfg");
            if (!Directory.Exists(directory)) return;
            string path = Path.Combine(directory, "enemy.ini");
            if (!System.IO.File.Exists(path)) return;
            try
            {
                var ini = IniDocument.Load(path);
                var sections = new HashSet<string>(ini.SectionNames, StringComparer.OrdinalIgnoreCase);
                var loadedWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string section in ini.SectionNames)
                {
                    if (string.IsNullOrWhiteSpace(section) || section.StartsWith("_", StringComparison.Ordinal)) continue;
                    var enemy = game.Enemies.TryGetValue(section, out var existing) ? existing.Copy() : new EnemySettings(section);
                    int damage = ini.GetInt(section, "damage", enemy.Damage);
                    if (damage >= 0) enemy.Damage = damage;
                    int health = ini.GetInt(section, "health", enemy.Health);
                    if (health > 0) enemy.Health = health;
                    if (ini.TryGetInt(section, "cooldown", out int milliseconds) && milliseconds > 0)
                        enemy.CooldownMs = milliseconds;
                    string weaponName = ini.GetString(section, "weapon0", enemy.Weapon0)?.Trim();
                    enemy.Weapon0 = null;
                    enemy.Weapon0Settings = null;
                    if (!string.IsNullOrEmpty(weaponName))
                    {
                        if (!weaponName.StartsWith("_", StringComparison.Ordinal) || !sections.Contains(weaponName))
                            warning?.Invoke("Invalid [" + section + "] weapon0: expected an existing section starting with _: " + weaponName);
                        else
                        {
                            game.EnemyWeapons.TryGetValue(weaponName, out var existingWeapon);
                            var weapon = loadedWeapons.Contains(weaponName) ? existingWeapon
                                : WeaponConfiguration.ReadSection(ini, weaponName, existingWeapon, warning);
                            game.RegisterEnemyWeapon(weapon);
                            loadedWeapons.Add(weaponName);
                            enemy.Weapon0 = weaponName;
                            enemy.Weapon0Settings = weapon;
                        }
                    }
                    game.RegisterEnemy(enemy);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                warning?.Invoke("Cannot read enemy.ini: " + error.Message);
            }
        }
    }
}
