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

        public static (string? FirstName, string? Nickname) GetCharacterFirstNameAndNickname(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return (null, null);

            string? firstName = null;
            string? nickname = null;
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
                            if (line.Trim().StartsWith("first_name="))
                            {
                                var match = Regex.Match(line, @"""(.+)""");
                                if (match.Success)
                                {
                                    firstName = match.Groups[1].Value;
                                }
                            }
                            else if (line.Trim().StartsWith("nickname_text="))
                            {
                                var match = Regex.Match(line, @"nickname_text=""(.+)""");
                                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value) && match.Groups[1].Value != "\"\"")
                                {
                                    nickname = match.Groups[1].Value;
                                }
                            }

                            if (firstName != null && nickname != null) break; // Optimization

                            if (line.Trim() == "}")
                            {
                                break; // End of character block
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Program.Logger.Debug($"Error reading Living.txt for first name/nickname: {ex.Message}");
            }
            return (firstName, nickname);
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
                            if (line.Trim().StartsWith("dynasty_house="))
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
                                    string dynastyName = match.Groups[1].Value;
                                    if (dynastyName.StartsWith("dynn_"))
                                    {
                                        return dynastyName.Substring(5); // Remove "dynn_" prefix
                                    }
                                    return dynastyName;
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
