using System;
using System.Globalization;

namespace Biosplit.Game
{
    public sealed class FrameAnimationSettings
    {
        public decimal FramesPerSecond { get; set; } = 10m;
        public string[] FrameNames { get; set; } = Array.Empty<string>();

        internal FrameAnimationSettings Copy() => new FrameAnimationSettings
        {
            FramesPerSecond = FramesPerSecond,
            FrameNames = FrameNames == null ? Array.Empty<string>() : (string[])FrameNames.Clone()
        };

        internal static FrameAnimationSettings Parse(string value)
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
            if (start == parts.Length) return null;
            var frames = new string[parts.Length - start];
            for (int i = start; i < parts.Length; i++)
            {
                string frame = parts[i].Trim();
                if (frame.Length == 0 || frame == "*") return null;
                frames[i - start] = frame;
            }
            return new FrameAnimationSettings { FramesPerSecond = fps, FrameNames = frames };
        }
    }
}
