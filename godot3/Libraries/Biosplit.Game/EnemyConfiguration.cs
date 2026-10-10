using System;
using System.IO;
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
                foreach (string section in ini.SectionNames)
                {
                    if (string.IsNullOrWhiteSpace(section)) continue;
                    var enemy = game.Enemies.TryGetValue(section, out var existing) ? existing.Copy() : new EnemySettings(section);
                    int damage = ini.GetInt(section, "damage", enemy.Damage);
                    if (damage >= 0) enemy.Damage = damage;
                    int health = ini.GetInt(section, "health", enemy.Health);
                    if (health > 0) enemy.Health = health;
                    if (ini.TryGetInt(section, "cooldown", out int milliseconds) && milliseconds > 0)
                        enemy.CooldownMs = milliseconds;
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
