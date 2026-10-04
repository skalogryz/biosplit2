using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Biosplit.Ini
{
    /// <summary>INI reader independent of Godot and game-specific settings.</summary>
    public sealed class IniDocument
    {
        private readonly Dictionary<string, Dictionary<string, string>> sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static IniDocument Load(string filePath)
        {
            using (var reader = new StreamReader(filePath))
                return Parse(reader);
        }

        public static IniDocument Parse(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var document = new IniDocument();
            string section = "";
            string rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }
                int separator = line.IndexOf('=');
                if (separator < 0) continue;
                string key = line.Substring(0, separator).Trim();
                if (key.Length == 0) continue;
                string value = line.Substring(separator + 1).Trim();
                int comment = value.IndexOfAny(new[] { ';', '#' });
                if (comment >= 0) value = value.Substring(0, comment).Trim();
                if (!document.sections.TryGetValue(section, out var values))
                {
                    values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    document.sections.Add(section, values);
                }
                values[key] = value;
            }
            return document;
        }

        public string GetString(string section, string key, string fallback = null)
        {
            return sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value)
                ? value : fallback;
        }

        public bool TryGetInt(string section, string key, out int value)
        {
            return int.TryParse(GetString(section, key), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
        }

        public int GetInt(string section, string key, int fallback)
        {
            return TryGetInt(section, key, out int value) ? value : fallback;
        }
    }
}
