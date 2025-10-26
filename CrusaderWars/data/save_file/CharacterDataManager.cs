using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CrusaderWars.data.save_file
{
    public static class CharacterDataManager
    {
        private static Dictionary<string, string?> characterDynastyNameCache = new Dictionary<string, string?>();

        public static void ClearCache()
        {
            characterDynastyNameCache.Clear();
        }

        public static string? GetCharacterDynastyName(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            if (characterDynastyNameCache.TryGetValue(characterId, out var cachedName))
            {
                return cachedName;
            }

            string? dynastyId = GetDynastyIdForCharacter(characterId);
            if (dynastyId == null)
            {
                characterDynastyNameCache[characterId] = null; // Cache failure
                return null;
            }

            string? dynastyName = GetDynastyNameForId(dynastyId);
            characterDynastyNameCache[characterId] = dynastyName;
            return dynastyName;
        }

        private static string? GetDynastyIdForCharacter(string characterId)
        {
            try
            {
                using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Living_Path()))
                {
                    string? line;
                    bool inCharacterBlock = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!inCharacterBlock && line.Trim() == $"{characterId}={{")
                        {
                            inCharacterBlock = true;
                            continue;
                        }

                        if (inCharacterBlock)
                        {
                            if (line.Trim().StartsWith("dynasty="))
                            {
                                return Regex.Match(line, @"\d+").Value;
                            }
                            if (line.Trim() == "}")
                            {
                                return null; // End of character block
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Program.Logger.Debug($"Error reading Living.txt: {ex.Message}");
            }
            return null;
        }

        private static string? GetDynastyNameForId(string dynastyId)
        {
            try
            {
                using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Dynasties_Path()))
                {
                    string? line;
                    bool inDynastyBlock = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!inDynastyBlock && line.Trim() == $"{dynastyId}={{")
                        {
                            inDynastyBlock = true;
                            continue;
                        }

                        if (inDynastyBlock)
                        {
                            if (line.Trim().StartsWith("name="))
                            {
                                var match = Regex.Match(line, @"""(.+)""");
                                if (match.Success)
                                {
                                    return match.Groups[1].Value;
                                }
                            }
                            if (line.Trim() == "}")
                            {
                                return null; // End of dynasty block
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Program.Logger.Debug($"Error reading Dynasties.txt: {ex.Message}");
            }
            return null;
        }
    }
}
