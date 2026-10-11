using System;
using System.Globalization;
using Biosplit.Ini;

namespace Biosplit.Game
{
    public readonly struct DamageRange : IEquatable<DamageRange>
    {
        public int Min { get; }
        public int Max { get; }

        public DamageRange(int min, int max)
        {
            if (min < 0 || max < min) throw new ArgumentOutOfRangeException(nameof(min), "Damage must satisfy 0 <= min <= max.");
            Min = min;
            Max = max;
        }
        public static implicit operator DamageRange(int damage) => new DamageRange(damage, damage);

        public int Roll(Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (Min == Max) return Min;
            long count = (long)Max - Min + 1;
            if (count <= int.MaxValue) return Min + random.Next((int)count);
            // The full nonnegative Int32 range has 2^31 possible values.
            var bytes = new byte[4];
            random.NextBytes(bytes);
            return (int)(BitConverter.ToUInt32(bytes, 0) & 0x7fffffffU);
        }

        public static bool TryParse(string value, out DamageRange damage)
        {
            damage = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            string[] parts = value.Trim().Split(new[] { "..", "-" }, StringSplitOptions.None);
            if (parts.Length < 1 || parts.Length > 2 ||
                !int.TryParse(parts[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int min)) return false;
            int max = min;
            if (parts.Length == 2 && !int.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out max)) return false;
            if (max < min) return false;
            damage = new DamageRange(min, max);
            return true;
        }

        internal static DamageRange Read(IniDocument ini, string section, DamageRange fallback, Action<string> warning)
        {
            string value = ini.GetString(section, "damage");
            if (value == null) return fallback;
            if (TryParse(value, out var damage)) return damage;
            warning?.Invoke("Invalid [" + section + "] damage: expected a nonnegative integer or min-max / min..max range.");
            return fallback;
        }
        public bool Equals(DamageRange other) => Min == other.Min && Max == other.Max;
        public override bool Equals(object obj) => obj is DamageRange other && Equals(other);
        public override int GetHashCode() => unchecked(Min * 397 ^ Max);
        public static bool operator ==(DamageRange left, DamageRange right) => left.Equals(right);
        public static bool operator !=(DamageRange left, DamageRange right) => !left.Equals(right);
        public override string ToString() => Min == Max ? Min.ToString(CultureInfo.InvariantCulture)
            : Min.ToString(CultureInfo.InvariantCulture) + ".." + Max.ToString(CultureInfo.InvariantCulture);
    }
}
