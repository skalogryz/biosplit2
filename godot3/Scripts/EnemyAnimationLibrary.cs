using System;
using System.Collections.Generic;
using Biosplit.Game;
using Godot;

// Each enemy starts from the scene frames, then receives its own animations and weapon strikes.
public static class EnemyAnimationLibrary
{
    public static Dictionary<string, SpriteFrames> Prepare(GameSettings settings, AnimatedSprite actor,
        string executableDirectory = null, Action<string> warning = null)
    {
        var result = new Dictionary<string, SpriteFrames>(StringComparer.OrdinalIgnoreCase);
        var sceneFrames = actor.Frames;
        try
        {
            foreach (var entry in settings.Enemies)
            {
                actor.Frames = Godot.Object.IsInstanceValid(sceneFrames) ? (SpriteFrames)sceneFrames.Duplicate() : new SpriteFrames();
                WeaponAnimationNormalizer.NormalizeEnemyAnimations(entry.Value, actor, executableDirectory, warning);
                if (entry.Value.Weapon0Settings != null)
                    WeaponAnimationNormalizer.Normalize(new[] { entry.Value.Weapon0Settings }, actor, executableDirectory, warning);
                result.Add(entry.Key, actor.Frames);
            }
        }
        finally { actor.Frames = sceneFrames; }
        return result;
    }
}
