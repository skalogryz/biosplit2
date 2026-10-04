using System;
using System.IO;
using Biosplit.Ini;

// Godot adapter: path selection and mapping to gameplay parameters.
public static class WeaponConfiguration
{
    public static void Load(string executableDirectory, Main game)
    {
        string directory = Path.Combine(executableDirectory, "cfg");
        if (!Directory.Exists(directory)) return;
        string path = Path.Combine(directory, "weapons.ini");
        if (!System.IO.File.Exists(path)) return;

        try
        {
            var ini = IniDocument.Load(path);
            game.PunchDamage = ini.GetInt("knife", "damage", game.PunchDamage);
            game.RagePerPunch = ini.GetInt("knife", "ragebonus", game.RagePerPunch);
            game.ShotDamage = ini.GetInt("handgun", "damage", game.ShotDamage);
            game.ShotCost = ini.GetInt("handgun", "rage", game.ShotCost);
            int knifeCooldown = ini.GetInt("knife", "cooldown", game.PunchCooldownMs);
            if (knifeCooldown >= 0) game.PunchCooldownMs = knifeCooldown;
            int handgunCooldown = ini.GetInt("handgun", "cooldown", game.ShotCooldownMs);
            if (handgunCooldown >= 0) game.ShotCooldownMs = handgunCooldown;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Godot.GD.PushWarning("Cannot read weapons.ini: " + error.Message);
        }
    }
}

