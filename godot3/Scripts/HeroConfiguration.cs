using System;
using System.IO;
using Biosplit.Ini;

public static class HeroConfiguration
{
    public static void Load(string executableDirectory, Main game)
    {
        string directory = Path.Combine(executableDirectory, "cfg");
        if (!Directory.Exists(directory)) return;
        string path = Path.Combine(directory, "main.ini");
        if (!System.IO.File.Exists(path)) return;
        try
        {
            var ini = IniDocument.Load(path);
            int health = ini.GetInt("hero", "health", game.MaxHealth);
            if (health > 0) game.MaxHealth = health;
            int rage = ini.GetInt("hero", "rage", game.MaxRage);
            if (rage > 0) game.MaxRage = rage;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Godot.GD.PushWarning("Cannot read main.ini: " + error.Message);
        }
    }
}
