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
                string key = "strike" + index.ToString(CultureInfo.InvariantCulture);
                string value = ini.GetString(section, key);
                if (value == null) break;
                string[] parts = value.Split(',');
                if (parts.Length != 3
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windup)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int recovery)
                    || windup < 0 || recovery < 0 || string.IsNullOrWhiteSpace(parts[2]))
                {
                    warning?.Invoke("Invalid [" + section + "] " + key + ": expected windupMs,recoveryMs,name. Strike sequence stopped.");
                    break;
                }
                strikes.Add(new StrikeSettings
                {
                    WindupMs = windup,
                    RecoveryMs = recovery,
                    Name = parts[2].Trim(),
                    DamageDurationMs = 25,
                    DamageModifier = 1m
                });
            }
            return strikes.ToArray();
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
