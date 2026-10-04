using System;
using System.IO;
using Biosplit.Ini;

public static class EnemyConfiguration
{
    public static void Load(string executableDirectory, Main game)
    {
        string directory = Path.Combine(executableDirectory, "cfg");
        if (!Directory.Exists(directory)) return;
        string path = Path.Combine(directory, "enemy.ini");
        if (!System.IO.File.Exists(path)) return;
        try
        {
            var ini = IniDocument.Load(path);
            int damage = ini.GetInt("zombie", "damage", game.EnemyDamage);
            if (damage >= 0) game.EnemyDamage = damage;
            int health = ini.GetInt("zombie", "health", game.EnemyHealth);
            if (health > 0) game.EnemyHealth = health;
            if (ini.TryGetInt("zombie", "cooldown", out int milliseconds) && milliseconds > 0)
                game.EnemyAttackInterval = milliseconds / 1000f;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Godot.GD.PushWarning("Cannot read enemy.ini: " + error.Message);
        }
    }
}
