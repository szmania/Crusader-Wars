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

            return playthroughTag switch
            {
                "DefaultCK3" => "Attila",
                "TheFallenEagle" => "Attila",
                "RealmsInExile" => "Attila",
                "AGOT" => "Attila",
                "BronzeAgeReborn" => "Rome2",
                "Custom" => ResolveCustomPlaythroughGameType(),
                _ => "Attila" // Default fallback
            };
        }

        private static string ResolveCustomPlaythroughGameType()
        {
            string customMapperTag = ModOptions.GetSelectedCustomMapper();
            if (string.IsNullOrEmpty(customMapperTag))
                return "Attila"; // Default

            // Search in both attila/ and rome_2/ directories
            string unitMappersBaseDir = @".\unit mappers";
            
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