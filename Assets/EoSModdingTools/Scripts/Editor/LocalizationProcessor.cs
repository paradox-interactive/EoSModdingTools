using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RomeroGames
{
    /// <summary>
    /// Generates localization string files
    /// </summary>
    public static class LocalizationProcessor
    {
        private static readonly string[] _caseSensitiveFormatNames = { "{player:", "{you:", "{them:", ":name", ":firstName", ":lastName", ":nickName", "_firstName", "_lastName", "_nickName" };

        private static readonly StringBuilder _escapeBuilder = new StringBuilder();

        // Based on the Escape method in SimpleJSON
        // https://wiki.unity3d.com/index.php/SimpleJSON
        private static string EscapeJSON(string text)
        {
            _escapeBuilder.Length = 0;
            if (_escapeBuilder.Capacity < text.Length + text.Length / 10)
            {
                _escapeBuilder.Capacity = text.Length + text.Length / 10;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Don't escape backslash if the next character is n, r, t, b or f
                if (c == '\\' &&  i < text.Length - 1)
                {
                    char n = text[i + 1];
                    switch (n)
                    {
                        case 'n':
                        case 'r':
                        case 't':
                        case 'b':
                        case 'f':
                            _escapeBuilder.Append("\\");
                            continue;
                    }
                }

                switch (c)
                {
                    case '\\':
                        _escapeBuilder.Append("\\\\");
                        break;
                    case '\"':
                        _escapeBuilder.Append("\\\"");
                        break;
                    case '\n':
                        _escapeBuilder.Append("\\n");
                        break;
                    case '\r':
                        _escapeBuilder.Append("\\r");
                        break;
                    case '\t':
                        _escapeBuilder.Append("\\t");
                        break;
                    case '\b':
                        _escapeBuilder.Append("\\b");
                        break;
                    case '\f':
                        _escapeBuilder.Append("\\f");
                        break;
                    default:
                        _escapeBuilder.Append(c);
                        break;
                }
            }

            string result = _escapeBuilder.ToString();
            _escapeBuilder.Length = 0;
            return result;
        }

        private static void WriteStringFile(List<LocalizationItem> localizationItems, string stringFile)
        {
            // Don't write a string file if there are no strings
            if (localizationItems.Count == 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            int count = 0;
            foreach (LocalizationItem localizationItem in localizationItems)
            {
                string key = localizationItem.stringKey;
                string value = EscapeJSON(localizationItem.source);
                string format;
                if (count < localizationItems.Count - 1)
                {
                    format = "  \"{0}\":\"{1}\",\n";
                }
                else
                {
                    format = "  \"{0}\":\"{1}\"\n";
                }
                string line = string.Format(format, key, value);
                sb.Append(line);
                count++;
            }
            sb.Append("}\n");

            // Write new strings file to disk
            try
            {
                // Ensure the required directories exist
                FileInfo stringFileInfo = new FileInfo(stringFile);
                stringFileInfo.Directory.Create();

                File.WriteAllText(stringFile, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat(e.Message);
            }
        }

        /// <summary>
        /// Generates a string file for the mod by parsing all Lua files for localized string definitions.
        /// Any existing string file is replaced.
        /// </summary>
        private static void GenerateStringFile(string modName, bool silent, HashSet<string> excludeStrings, HashSet<string> existingKeys)
        {
            double timer = EditorApplication.timeSinceStartup;

            string stringFile = ModUtils.GetStringFilePath(modName);

            bool fileExists = File.Exists(stringFile);

            List<LocalizationItem> localizationItems = new List<LocalizationItem>();
            List<FileComment> fileComments = new List<FileComment>();
            if (!GenerateLocalizationItems(modName, localizationItems, excludeStrings, existingKeys, fileComments))
            {
                return;
            }

            WriteStringFile(localizationItems, stringFile);

            if (!fileExists)
            {
                // Handle when a new file is created
                AssetDatabase.Refresh();
            }

            if (!silent)
            {
                Debug.LogFormat("Wrote string file: {0} ({1:F3}s).", stringFile, EditorApplication.timeSinceStartup - timer);
            }
        }

        private static bool IsValidKeyChar(int i, char c)
        {
            if (i >= 1 && c == '_')
            {
                return true;
            }

            if (c >= 'A' && c <= 'Z')
            {
                return true;
            }

            if (c >= 'a' && c <= 'z')
            {
                return true;
            }

            if (i > 1 && c >= '0' && c <= '9')
            {
                return true;
            }

            return false;
        }

        private static int CountOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        #region Public members

        /// <summary>
        /// Returns true if the key has valid syntax for a string key.
        /// </summary>
        public static bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 2)
            {
                return false;
            }

            if (key[0] != '$')
            {
                return false;
            }

            bool hasLetter = false; // A valid string key must contain at least one letter character
            for (int i = 1; i < key.Length; i++)
            {
                char c = key[i];

                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    hasLetter = true;
                }

                if (!IsValidKeyChar(i, c))
                {
                    return false;
                }
            }

            return hasLetter;
        }

        // Check formatting macros only contain ASCII characters
        // Check special identifiers for incorrect case
        public static bool ValidateFormatItems(string text)
        {
            // {0} {0:name}
            // {0:gender?A|B|C}

            bool inBraces = false;
            bool inGenderOptions = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                switch (c)
                {
                    case '{':
                        if (inBraces)
                        {
                            // Debug.LogError($"Mismatched braces found in format text: {text}");
                            return false;
                        }

                        inBraces = true;
                        break;

                    case '?':
                        if (inBraces)
                        {
                            if (!inGenderOptions)
                            {
                                if (i < 9 || text.Substring(i - 7, 7) != ":gender")
                                {
                                    // Debug.LogError($"Invalid gender macro: {text}");
                                    return false;
                                }
                            }

                            inGenderOptions = true;
                        }

                        break;

                    case '}':
                        if (!inBraces)
                        {
                            // Debug.LogError($"Mismatched braces found in format text: {text}");
                            return false;
                        }

                        inBraces = false;
                        inGenderOptions = false;
                        break;

                    default:
                        if (inBraces && !inGenderOptions && c > 127)
                        {
                            // Debug.LogError($"Non-ascii identifier found at {i} in format item: {text}");
                            return false;
                        }

                        break;
                }
            }

            if (text.IndexOf(':') != -1)
            {
                foreach (string formatName in _caseSensitiveFormatNames)
                {
                    if (text.IndexOf(formatName, StringComparison.OrdinalIgnoreCase) != -1 &&
                        text.IndexOf(formatName, StringComparison.Ordinal) == -1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool ValidateEscapeCharacters(string lineText)
        {
            return !lineText.Contains("＼ｎ");
        }

        public static bool ValidateGenderMacro(string lineText)
        {
            int startIndex = 0;

            int optionStart;
            int optionEnd;

            while (startIndex < lineText.Length)
            {
                optionStart = lineText.IndexOf(":gender?", startIndex, StringComparison.Ordinal);
                if (optionStart == -1)
                {
                    return true;
                }

                optionEnd = lineText.IndexOf("}", optionStart, StringComparison.Ordinal);
                if (optionEnd == -1)
                {
                    // Macro does not have a closing brace
                    return false;
                }

                int count = 0;
                for (int i = optionStart; i < optionEnd; i++)
                {
                    if (lineText[i] == '|')
                    {
                        count++;
                    }
                }

                // Must be exactly 1 or 3 options
                if (count != 0 && count != 2)
                {
                    return false;
                }

                startIndex = optionEnd + 1;
            }

            return true;
        }

        /// <summary>
        /// Parses a line of Lua text looking for a localizable string.
        /// Returns true if the localizable string has no syntax errors.
        /// localizationItem will be null if the line does not contain a localizable string.
        /// Valid format is:
        /// "$StringKey" --$ Some text == Comment
        /// </summary>
        public static bool ParseLine(string line, int stringKeyStart, out LocalizationItem localizationItem)
        {
            localizationItem = null;

            int stringKeyEnd = line.IndexOf("\"", stringKeyStart + 2);
            if (stringKeyEnd == -1)
            {
                return true;
            }

            int sourceTextStart = line.IndexOf("--$", stringKeyEnd + 1);
            if (sourceTextStart == -1)
            {
                return true;
            }

            sourceTextStart += 3;
            while (sourceTextStart < line.Length &&
                   Char.IsWhiteSpace(line[sourceTextStart]))
            {
                sourceTextStart++;
            }

            string key = line.Substring(stringKeyStart + 1, stringKeyEnd - stringKeyStart - 1);

            localizationItem = new LocalizationItem()
            {
                stringKey = key
            };

            if (sourceTextStart < line.Length)
            {
                int commentStart = line.IndexOf("==", sourceTextStart);
                if (commentStart == -1)
                {
                    localizationItem.source = line.Substring(sourceTextStart).Trim();
                }
                else
                {
                    localizationItem.source = line.Substring(sourceTextStart, commentStart - sourceTextStart).Trim();
                    localizationItem.comment = line.Substring(commentStart + 2).Trim();
                }
            }
            else
            {
                localizationItem.source = string.Empty;
            }

            if (!ValidateFormatItems(localizationItem.source) ||
                !ValidateGenderMacro(localizationItem.source))
            {
                localizationItem = null;
                return false;
            }

            return true;
        }

        public static bool GenerateLocalizationItems(string modName, List<LocalizationItem> localizationItems, HashSet<string> excludeStrings, HashSet<string> existingKeys, List<FileComment> fileComments = null)
        {
            HashSet<string> stringKeys = new HashSet<string>();
            Dictionary<string, string> stringReferences = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder(1024);

            // Find all Lua files in the mod folder
            string modFolder =  ModUtils.GetModPath(modName);
            string luaPath = EoSPathUtils.CombinePath(modFolder, "Lua");

            bool valid = true;

            var guids = AssetDatabase.FindAssets("*", new string[] { luaPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetExtension(path) != ".lua")
                {
                    continue;
                }

                string category = string.Empty;
                sb.Length = 0;

                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    int lineNumber = -1;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNumber++;

                        // The aim here is to early out as quickly as possible for lines that don't contain
                        // a localizable string, so the order of checks is important.

                        if (line.Length < 5)
                        {
                            continue;
                        }

                        int stringKeyStart = line.IndexOf('$');
                        if (stringKeyStart < 1)
                        {
                            continue;
                        }

                        if (line[0] == '-' &&
                            line[1] == '-')
                        {
                            if (line[2] == '$' && line[3] == '$')
                            {
                                // Category definition
                                category = line.Substring(4).Trim();
                                continue;
                            }
                            if (fileComments != null && line[2] == '$' && line[3] == '=' && line[4] == '=')
                            {
                                // File comment
                                if (sb.Length != 0)
                                {
                                    sb.Append("\n");
                                }
                                sb.Append(line.Substring(5).Trim());
                                continue;
                            }
                            if (line[2] != ' ' || line[3] != '"' || line[4] != '$')
                            {
                                // Commented out line that isn't a valid string definition
                                continue;
                            }
                        }

                        stringKeyStart = line.IndexOf("\"$", stringKeyStart - 1);
                        if (stringKeyStart == -1)
                        {
                            continue;
                        }

                        if (!ParseLine(line, stringKeyStart, out LocalizationItem localizationItem))
                        {
                            Debug.LogErrorFormat("Invalid localization item: {0}:{1}\n{2}", path, lineNumber + 1, line);
                            valid = false;
                        }

                        if (localizationItem == null)
                        {
                            continue;
                        }

                        if (!ValidateKey(localizationItem.stringKey))
                        {
                            Debug.LogErrorFormat("Invalid string key: {0}", localizationItem.stringKey);
                            valid = false;
                        }

                        if (stringKeys.Contains(localizationItem.stringKey))
                        {
                            Debug.LogErrorFormat("Duplicate string key: {0} ({1}:{2})", localizationItem.stringKey, path, (lineNumber+1));
                            valid = false;
                        }

                        if (valid)
                        {
                            if (ValidateKey(localizationItem.source))
                            {
                                // Strings that only reference another string key (e.g. from datasheets) never need to be localized
                                localizationItem.category = "DoNotLocalize";
                                stringReferences[localizationItem.stringKey] = localizationItem.source;
                            }
                            else if (excludeStrings != null && excludeStrings.Contains(localizationItem.stringKey))
                            {
                                localizationItem.category = "DoNotLocalize";
                            }
                            else
                            {
                                localizationItem.category = category;
                            }

                            localizationItem.filename = $"{modName}:{path.Substring(luaPath.Length + 1)}";
                            localizationItem.lineNumber = lineNumber;

                            stringKeys.Add(localizationItem.stringKey);
                            localizationItems.Add(localizationItem);
                        }
                    }
                }

                if (fileComments != null && sb.Length > 0)
                {
                    fileComments.Add(new FileComment
                    {
                        filename = path.Substring(luaPath.Length + 1),
                        comment = sb.ToString()
                    });
                }
            }
            existingKeys.UnionWith(stringKeys);

#if EXCLUDE_MODDING_SUPPORT
            // Verify that simple string references refer to a valid string
            foreach (var kv in stringReferences)
            {
                string sourceStringKey = kv.Key;
                string referenceStringKey = kv.Value;
                if (!existingKeys.Contains(referenceStringKey))
                {
                    Debug.LogError($"The string '{sourceStringKey}' references another string '{referenceStringKey}' which does not exist");
                    valid = false;
                }
            }
#endif

            return valid;
        }

        /// <summary>
        /// Generates the localization strings file for each mod in the list.
        /// </summary>
        public static void GenerateLocalizationFiles(List<string> modNames, bool silent, HashSet<string> excludeStrings)
        {
            //Removing and adding back in so GameData comes first in order to validate string references.
            if (modNames.Contains("GameData"))
            {
                modNames.Remove("GameData");
                modNames.Insert(0, "GameData");
            }
            HashSet<string> existingKeys = new HashSet<string>();
            foreach (string modName in modNames)
            {
                GenerateStringFile(modName, silent, excludeStrings, existingKeys);
            }
        }

        #endregion
    }
}
