using System;
using System.IO;
using System.Xml;
using CrusaderWars.client;

namespace CrusaderWars.games
{
    public static class GameServiceFactory
    {
        private static readonly Lazy<AttilaGameService> _attilaService = new Lazy<AttilaGameService>(() => new AttilaGameService());
        private static readonly Lazy<Rome2GameService> _rome2Service = new Lazy<Rome2GameService>(() => new Rome2GameService());

        // Currently resolved game service (set during startup/playthrough changes)
        private static GameService? _currentService;

        public static GameService GetGameService(string gameType)
        {
            return gameType switch
            {
                "Attila" => _attilaService.Value,
                "Rome2" => _rome2Service.Value,
                _ => throw new ArgumentException($"Unknown game type: {gameType}", nameof(gameType))
            };
        }

        public static GameService GetCurrentGameService()
        {
            if (_currentService != null)
                return _currentService;

            // Resolve from settings
            string activeGameType = ModOptions.GetActiveGameType();
            _currentService = GetGameService(activeGameType);
            return _currentService;
        }

        public static void SetCurrentGameService(GameService service)
        {
            _currentService = service;
        }

        public static string ResolveGameTypeFromPlaythrough(string playthroughTag)
        {
            if (string.IsNullOrEmpty(playthroughTag))
                return "Attila"; // Default

// Fast-path for the known legacy tags (avoids a filesystem scan).
            switch (playthroughTag)
            {
                case "DefaultCK3":
                case "TheFallenEagle":
                case "RealmsInExile":
                case "AGOT":
                case "BookmarksPlus":
                    return "Attila";
                case "BronzeAgeReborn":
                    return "Rome2";
                case "Custom":
                    return ResolveCustomPlaythroughGameType();
            }

            // Unknown tag: resolve from the mapper folders so dynamically discovered
            // Rome 2-only playthroughs are not misclassified as Attila.
            return ResolveGameTypeFromMapperFolders(playthroughTag);
        }

        /// <summary>
        /// Resolves the game type for a playthrough tag by scanning the unit mapper
        /// subdirectories. Returns "Rome2" when the tag exists only under rome_2/,
        /// "Attila" when it exists only under attila/, "Attila" when it exists in
        /// both (blended playthroughs default to Attila), and "Attila"
        /// as the legacy default when the tag is found in neither.
        /// </summary>
        private static string ResolveGameTypeFromMapperFolders(string playthroughTag)
        {
            bool inAttila = TagExistsInMapperSubDir(playthroughTag, "attila");
            bool inRome2 = TagExistsInMapperSubDir(playthroughTag, "rome_2");

            // Blended (present in both) and Attila-only both resolve to Attila; only a
            // Rome 2-only tag resolves to Rome2. Unknown tags fall through to Attila.
            if (inRome2 && !inAttila)
                return "Rome2";
            return "Attila";
        }

        /// <summary>
        /// True when a mapper folder directly under the given unit mapper subdirectory
        /// has a tag.txt whose trimmed content matches the playthrough tag
        /// (case-insensitive).
        /// </summary>
        private static bool TagExistsInMapperSubDir(string playthroughTag, string subDirName)
        {
            string subDir = Path.Combine(CrusaderWars.unit_mapper.UnitMappers_BETA.UnitMappersRootDir, subDirName);
            if (!Directory.Exists(subDir))
                return false;

            foreach (var dir in Directory.GetDirectories(subDir))
            {
                string tagFile = Path.Combine(dir, "tag.txt");
                if (File.Exists(tagFile) &&
                    File.ReadAllText(tagFile).Trim().Equals(playthroughTag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string ResolveCustomPlaythroughGameType()
        {
            string customMapperTag = ModOptions.GetSelectedCustomMapper();
            if (string.IsNullOrEmpty(customMapperTag))
                return "Attila"; // Default

            // Search in both attila/ and rome_2/ directories
            string unitMappersBaseDir = CrusaderWars.unit_mapper.UnitMappers_BETA.UnitMappersRootDir;
            
            // Check Rome 2 first
            string rome2Dir = Path.Combine(unitMappersBaseDir, "rome_2");
            if (Directory.Exists(rome2Dir))
            {
                foreach (var dir in Directory.GetDirectories(rome2Dir))
                {
                    string tagFile = Path.Combine(dir, "tag.txt");
                    if (File.Exists(tagFile) && 
                        File.ReadAllText(tagFile).Trim().Equals(customMapperTag, StringComparison.OrdinalIgnoreCase))
                        return "Rome2";
                }
            }

            // Check Attila
            string attilaDir = Path.Combine(unitMappersBaseDir, "attila");
            if (Directory.Exists(attilaDir))
            {
                foreach (var dir in Directory.GetDirectories(attilaDir))
                {
                    string tagFile = Path.Combine(dir, "tag.txt");
                    if (File.Exists(tagFile) && 
                        File.ReadAllText(tagFile).Trim().Equals(customMapperTag, StringComparison.OrdinalIgnoreCase))
                        return "Attila";
                }
            }

            // Legacy flat structure
            if (Directory.Exists(unitMappersBaseDir))
            {
                foreach (var dir in Directory.GetDirectories(unitMappersBaseDir))
                {
                    string tagFile = Path.Combine(dir, "tag.txt");
                    if (File.Exists(tagFile) && 
                        File.ReadAllText(tagFile).Trim().Equals(customMapperTag, StringComparison.OrdinalIgnoreCase))
                        return "Attila"; // Legacy is Attila
                }
            }

            return "Attila"; // Default
        }
    }
}