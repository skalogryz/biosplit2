using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
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
                foreach (string section in ini.SectionNames)
                {
                    if (string.IsNullOrWhiteSpace(section)) continue;
                    var weapon = game.Weapons.TryGetValue(section, out var existing)
                        ? existing.Copy() : new WeaponSettings(section);
                    weapon.Type = ReadType(ini, section, weapon.Type);
                    int damage = ini.GetInt(section, "damage", weapon.Damage);
                    if (damage >= 0) weapon.Damage = damage;
                    int rageBonus = ini.GetInt(section, "ragebonus", weapon.RageBonus);
                    if (rageBonus >= 0) weapon.RageBonus = rageBonus;
                    int rageCost = ini.GetInt(section, "rage", weapon.RageCost);
                    if (rageCost >= 0) weapon.RageCost = rageCost;
                    decimal stamina = ini.GetDecimal(section, "stamina", weapon.StaminaCost);
                    if (stamina >= 0m) weapon.StaminaCost = stamina;
                    int cooldown = ini.GetInt(section, "cooldown", weapon.CooldownMs);
                    if (cooldown >= 0) weapon.CooldownMs = cooldown;
                    int attackDuration = ini.GetInt(section, "attackduration", weapon.AttackDurationMs);
                    if (attackDuration >= 0) weapon.AttackDurationMs = attackDuration;
                    weapon.Striker = ReadStrikes(ini, section, warning);
                    game.RegisterWeapon(weapon);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                warning?.Invoke("Cannot read " + Path.GetFileName(path) + ": " + error.Message);
            }
        }
        private static StrikeSettings[] ReadStrikes(IniDocument ini, string section, Action<string> warning)
        {
            var strikes = new List<StrikeSettings>();
            for (int index = 0; ; index++)
            {
                string number = index.ToString(CultureInfo.InvariantCulture);
                string key = "strike" + number;
                string value = ini.GetString(section, key);
                string framedKey = "fstrike" + number;
                string framedValue = ini.GetString(section, framedKey);
                if (value == null && framedValue == null) break;
                if (value != null && framedValue != null)
                {
                    warning?.Invoke("Invalid [" + section + "]: " + key + " and " + framedKey + " use the same strike index. Strike sequence stopped.");
                    break;
                }
                if (framedValue != null)
                {
                    var framed = ReadFramedStrike(framedValue);
                    if (framed == null)
                    {
                        warning?.Invoke("Invalid [" + section + "] " + framedKey + ": expected optional positive FPS, frame names and exactly one * marker. Strike sequence stopped.");
                        break;
                    }
                    strikes.Add(framed);
                    continue;
                }
                string[] parts = value.Split(',');
                int recovery = -1;
                if ((parts.Length != 2 && parts.Length != 3)
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windup)
                    || windup < 0
                    || (parts.Length == 3 && (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out recovery) || recovery < 0))
                    || string.IsNullOrWhiteSpace(parts[parts.Length - 1]))
                {
                    warning?.Invoke("Invalid [" + section + "] " + key + ": expected windupMs,name or windupMs,recoveryMs,name. Strike sequence stopped.");
                    break;
                }
                strikes.Add(new StrikeSettings
                {
                    WindupMs = windup,
                    RecoveryMs = recovery,
                    Name = parts[parts.Length - 1].Trim(),
                    DamageDurationMs = 25,
                    DamageModifier = 1m
                });
            }
            return strikes.ToArray();
        }

        private static StrikeSettings ReadFramedStrike(string value)
        {
            string[] parts = value.Split(',');
            decimal fps = 10m;
            int start = 0;
            if (decimal.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal explicitFps))
            {
                if (explicitFps <= 0m) return null;
                fps = explicitFps;
                start = 1;
            }
            var frames = new string[parts.Length - start];
            int markers = 0;
            for (int i = start; i < parts.Length; i++)
            {
                string frame = parts[i].Trim();
                if (frame.Length == 0) return null;
                if (frame == "*") markers++;
                frames[i - start] = frame;
            }
            if (markers != 1) return null;
            // Animation creation and phase timing are left to a future Godot normalization step.
            return new StrikeSettings { FrameNames = frames, FramesPerSecond = fps, DamageDurationMs = 25 };
        }

        private static WeaponType ReadType(IniDocument ini, string section, WeaponType fallback)
        {
            string value = ini.GetString(section, "type");
            if (value == null) return fallback;
            switch (value.Trim().ToLowerInvariant())
            {
                case "gun":
                case "firearm":
                    return WeaponType.Firearm;
                case "melee":
                    return WeaponType.Melee;
                default:
                    return WeaponType.None;
            }
        }
    }
}
