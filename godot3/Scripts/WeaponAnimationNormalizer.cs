using System;
using Biosplit.Game;
using Godot;

// Animation-dependent settings are resolved by Godot before GameSession snapshots them.
public static class WeaponAnimationNormalizer
{
    public static void Normalize(GameSettings settings, AnimatedSprite player)
    {
        foreach (var weapon in settings.Weapons.Values)
        {
            var strikes = weapon.Striker;
            for (int i = 0; i < strikes.Length; i++)
            {
                var strike = strikes[i] ?? (strikes[i] = new StrikeSettings());
                if (strike.RecoveryMs == -1)
                {
                    decimal durationMs = AnimationDurationMs(player, strike.Name);
                    decimal recoveryMs = Math.Max(0m, durationMs - Math.Max(0, strike.WindupMs) - Math.Max(0, strike.DamageDurationMs));
                    strike.RecoveryMs = (int)Math.Min(int.MaxValue, Math.Ceiling(recoveryMs));
                }
                else strike.RecoveryMs = Math.Max(0, strike.RecoveryMs);
            }
        }
    }

    private static decimal AnimationDurationMs(AnimatedSprite player, string name)
    {
        if (!Godot.Object.IsInstanceValid(player) || !Godot.Object.IsInstanceValid(player.Frames)
            || string.IsNullOrEmpty(name) || !player.Frames.HasAnimation(name)) return 0m;
        double speed = (double)player.Frames.GetAnimationSpeed(name) * player.SpeedScale;
        if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed)) return 0m;
        double duration = player.Frames.GetFrameCount(name) * 1000d / speed;
        return (decimal)Math.Min(int.MaxValue, duration);
    }
}
