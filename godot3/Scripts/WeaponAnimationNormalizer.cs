using System;
using System.Collections.Generic;
using Biosplit.Game;
using Godot;

// Animation-dependent settings are resolved by Godot before GameSession snapshots them.
public static class WeaponAnimationNormalizer
{
    public static void Normalize(GameSettings settings, AnimatedSprite player, string executableDirectory = null, Action<string> warning = null)
    {
        FrameLoader loader = null;
        bool copiedFrames = false;
        foreach (var weapon in settings.Weapons.Values)
        {
            var strikes = weapon.Striker;
            for (int i = 0; i < strikes.Length; i++)
            {
                var strike = strikes[i] ?? (strikes[i] = new StrikeSettings());
                if (strike.IsFramed)
                {
                    if (loader == null) loader = new FrameLoader(executableDirectory ?? System.IO.Path.GetDirectoryName(OS.GetExecutablePath()), warning);
                    if (Godot.Object.IsInstanceValid(player))
                    {
                        if (!copiedFrames)
                        {
                            player.Frames = Godot.Object.IsInstanceValid(player.Frames) ? (SpriteFrames)player.Frames.Duplicate() : new SpriteFrames();
                            copiedFrames = true;
                        }
                        CreateFramedAnimation(weapon.Name, i, strike, player, loader, warning);
                    }
                }
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

    private static void CreateFramedAnimation(string weaponName, int index, StrikeSettings strike,
        AnimatedSprite player, FrameLoader loader, Action<string> warning)
    {
        decimal fps = strike.FramesPerSecond > 0m ? strike.FramesPerSecond : 10m;
        int marker = Array.IndexOf(strike.FrameNames, "*");
        if (marker < 0 || Array.LastIndexOf(strike.FrameNames, "*") != marker)
        {
            warning?.Invoke("Invalid framed strike: " + weaponName + " " + index);
            return;
        }
        decimal speed = player.SpeedScale > 0f ? (decimal)player.SpeedScale : 1m;
        double frameMs = 1000d / (double)fps / (double)speed;
        strike.WindupMs = ToMilliseconds(marker * frameMs);
        strike.RecoveryMs = ToMilliseconds((strike.FrameNames.Length - marker - 1) * frameMs);
        var textures = new List<Texture>();
        foreach (string frame in strike.FrameNames)
        {
            if (frame == "*") continue;
            Texture texture = loader.Load(frame);
            if (texture == null)
            {
                warning?.Invoke("Cannot create framed strike " + weaponName + " " + index + ": frame not found or ambiguous: " + frame);
                return;
            }
            textures.Add(texture);
        }
        if (textures.Count == 0)
        {
            warning?.Invoke("Cannot create empty framed animation: " + weaponName + " " + index);
            return;
        }
        string name = weaponName + "_strike_" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (player.Frames.HasAnimation(name)) player.Frames.RemoveAnimation(name);
        player.Frames.AddAnimation(name);
        player.Frames.SetAnimationSpeed(name, (float)fps);
        player.Frames.SetAnimationLoop(name, false);
        foreach (var texture in textures) player.Frames.AddFrame(name, texture);
        strike.Name = name;
    }

    private static int ToMilliseconds(double value) => (int)Math.Min(int.MaxValue, Math.Ceiling(value));

    private sealed class FrameLoader
    {
        private readonly Dictionary<string, List<string>> resources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> files = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Texture> cache = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
        private readonly Action<string> warning;

        public FrameLoader(string executableDirectory, Action<string> warning)
        {
            this.warning = warning;
            IndexResources("res://");
            string root = System.IO.Path.Combine(executableDirectory, "sprites");
            IndexDisk(root);
        }

        private static void Add(Dictionary<string, List<string>> index, string name, string path)
        {
            if (!index.TryGetValue(name, out var matches)) index[name] = matches = new List<string>();
            if (!matches.Contains(path)) matches.Add(path);
        }

        private static bool IsImage(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static void IndexImage(Dictionary<string, List<string>> index, string path)
        {
            if (!IsImage(path)) return;
            string name = System.IO.Path.GetFileName(path);
            Add(index, name, path);
            Add(index, System.IO.Path.GetFileNameWithoutExtension(name), path);
        }

        private void IndexResources(string path)
        {
            using (var directory = new Godot.Directory())
            {
                if (directory.Open(path) != Error.Ok) return;
                directory.ListDirBegin(true, true);
                var subdirectories = new List<string>();
                string entry;
                while ((entry = directory.GetNext()) != "")
                {
                    string full = path.EndsWith("/") ? path + entry : path + "/" + entry;
                    if (directory.CurrentIsDir()) subdirectories.Add(full);
                    else IndexImage(resources, full.EndsWith(".remap", StringComparison.OrdinalIgnoreCase) ? full.Substring(0, full.Length - 6) : full);
                }
                directory.ListDirEnd();
                foreach (string subdirectory in subdirectories) IndexResources(subdirectory);
            }
        }

        private void IndexDisk(string root)
        {
            if (!System.IO.Directory.Exists(root)) return;
            try
            {
                foreach (string file in System.IO.Directory.EnumerateFiles(root)) IndexImage(files, file);
                foreach (string child in System.IO.Directory.EnumerateDirectories(root))
                {
                    if ((System.IO.File.GetAttributes(child) & System.IO.FileAttributes.ReparsePoint) == 0) IndexDisk(child);
                }
            }
            catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException)
            {
                warning?.Invoke("Cannot read sprites directory " + root + ": " + error.Message);
            }
        }

        public Texture Load(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name != System.IO.Path.GetFileName(name)) return null;
            if (cache.TryGetValue(name, out var cached)) return cached;
            Texture texture = LoadResource(name) ?? LoadDisk(name);
            if (texture != null) cache[name] = texture;
            return texture;
        }

        private Texture LoadResource(string name)
        {
            if (!resources.TryGetValue(name, out var matches)) return null;
            if (matches.Count != 1)
            {
                warning?.Invoke("Ambiguous resource frame name: " + name);
                return null;
            }
            return ResourceLoader.Load(matches[0]) as Texture;
        }

        private Texture LoadDisk(string name)
        {
            if (!files.TryGetValue(name, out var matches)) return null;
            if (matches.Count != 1)
            {
                warning?.Invoke("Ambiguous sprites frame name: " + name);
                return null;
            }
            using (var image = new Image())
            {
                if (image.Load(matches[0]) != Error.Ok) return null;
                var texture = new ImageTexture();
                texture.CreateFromImage(image);
                return texture;
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
