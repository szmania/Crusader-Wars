using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.unit_mapper
{
    public class WeatherEnvironment
    {
        public string Key { get; set; }
        public string Ck3Type { get; set; }
    }

    public class WeatherDefinition
    {
        public string Ck3Type { get; set; }
        public List<WeatherEnvironment> Environments { get; set; } = new List<WeatherEnvironment>();
    }

    public class WeatherData
    {
        private static readonly Lazy<WeatherData> _instance = new Lazy<WeatherData>(() => new WeatherData());

        public static WeatherData Instance => _instance.Value;

        private Dictionary<string, List<WeatherDefinition>> _weatherByType = new Dictionary<string, List<WeatherDefinition>>();

        private WeatherData() { }

        public void LoadFromMapper(string mapperPath)
        {
            // Clear previous mapper's definitions so switching playthroughs never keeps stale weather.
            _weatherByType.Clear();

            var weatherPath = Path.Combine(mapperPath, "Weather");
            if (!Directory.Exists(weatherPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(weatherPath, "*.xml"))
            {
                try
                {
                    var doc = XDocument.Load(file);
                    // The document root IS <Weather> per the weather.xsd spec; a child lookup
                    // (doc.Root?.Element("Weather")) would always return null and skip every file.
                    var weatherElement = doc.Root;
                    if (weatherElement == null || weatherElement.Name.LocalName != "Weather") continue;

                    // Process Land/Day (battles we support)
                    var landElement = weatherElement.Element("Land");
                    if (landElement != null)
                    {
                        var dayElement = landElement.Element("Day");
                        if (dayElement != null)
                        {
                            foreach (var weatherTypeElement in dayElement.Elements("WeatherType"))
                            {
                                var ck3Type = weatherTypeElement.Attribute("ck3_type")?.Value;
                                if (string.IsNullOrEmpty(ck3Type)) continue;

                                var environments = new List<WeatherEnvironment>();
                                foreach (var envElement in weatherTypeElement.Elements("Environment"))
                                {
                                    var key = envElement.Attribute("key")?.Value;
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        environments.Add(new WeatherEnvironment { Key = key, Ck3Type = ck3Type });
                                    }
                                }

                                if (environments.Any())
                                {
                                    var def = new WeatherDefinition { Ck3Type = ck3Type, Environments = environments };
                                    if (!_weatherByType.ContainsKey(ck3Type))
                                    {
                                        _weatherByType[ck3Type] = new List<WeatherDefinition>();
                                    }
                                    _weatherByType[ck3Type].Add(def);
                                }
                            }
                        }
                    }

                    // Also process Night (for future support)
                    var nightElement = landElement?.Element("Night");
                    // ... night processing can be added later
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error loading weather from {file}: {ex.Message}");
                }
            }
        }

        public List<WeatherEnvironment> GetEnvironments(string ck3Type)
        {
            if (_weatherByType.TryGetValue(ck3Type, out var definitions))
            {
                // Return first definition's environments
                return definitions.FirstOrDefault()?.Environments ?? new List<WeatherEnvironment>();
            }
            return new List<WeatherEnvironment>();
        }
    }
}
