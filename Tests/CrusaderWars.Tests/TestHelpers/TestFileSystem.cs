using System;
using System.IO;
using System.Text;

namespace CrusaderWars.Tests.TestHelpers
{
    /// <summary>
    /// Provides temporary file system scaffolding for integration tests.
    /// Creates disposable directory structures that mimic the application's
    /// expected layout (settings/, unit mappers/, data/, etc.).
    /// </summary>
    public class TempTestEnvironment : IDisposable
    {
        public string RootPath { get; }
        public string SettingsPath { get; }
        public string UnitMappersPath { get; }
        public string DataPath { get; }
        public string UnitsCardsNamesPath { get; }

        public TempTestEnvironment()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"CC_Tests_{Guid.NewGuid():N}");
            SettingsPath = Path.Combine(RootPath, "settings");
            UnitMappersPath = Path.Combine(RootPath, "unit mappers");
            DataPath = Path.Combine(RootPath, "data");
            UnitsCardsNamesPath = Path.Combine(DataPath, "units_cards_names");

            Directory.CreateDirectory(SettingsPath);
            Directory.CreateDirectory(UnitMappersPath);
            Directory.CreateDirectory(UnitsCardsNamesPath);
        }

        /// <summary>
        /// Creates a UnitMappers.xml file with the specified playthrough states.
        /// </summary>
        public void CreateUnitMappersXml(
            bool defaultCK3 = false,
            bool theFallenEagle = false,
            bool realmsInExile = false,
            bool agot = false,
            bool bookmarksPlus = false,
            bool custom = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<UMOptions>");
            sb.AppendLine($"  <UnitMappers name=\"DefaultCK3\">{defaultCK3}</UnitMappers>");
            sb.AppendLine($"  <UnitMappers name=\"TheFallenEagle\">{theFallenEagle}</UnitMappers>");
            sb.AppendLine($"  <UnitMappers name=\"RealmsInExile\">{realmsInExile}</UnitMappers>");
            sb.AppendLine($"  <UnitMappers name=\"AGOT\">{agot}</UnitMappers>");
            sb.AppendLine($"  <UnitMappers name=\"BookmarksPlus\">{bookmarksPlus}</UnitMappers>");
            sb.AppendLine($"  <UnitMappers name=\"Custom\">{custom}</UnitMappers>");
            sb.AppendLine("</UMOptions>");

            File.WriteAllText(Path.Combine(SettingsPath, "UnitMappers.xml"), sb.ToString());
        }

        /// <summary>
        /// Creates a dlc_load.json file with the specified enabled mods.
        /// </summary>
        public void CreateDlcLoadJson(string[] enabledMods)
        {
            var modEntries = string.Join(",\n", 
                enabledMods.Select(m => $"    \"mod/{m}\""));
            var json = "{\n  \"enabled_mods\": [\n" + modEntries + "\n  ]\n}";
            File.WriteAllText(Path.Combine(RootPath, "dlc_load.json"), json);
        }

        /// <summary>
        /// Creates a minimal unit mapper directory structure for Bookmarks+.
        /// </summary>
        public string CreateBookmarksPlusMapperDir()
        {
            var mapperDir = Path.Combine(UnitMappersPath, "OfficialCC_BookmarksPlus_FireforgedEmpire");
            Directory.CreateDirectory(mapperDir);
            Directory.CreateDirectory(Path.Combine(mapperDir, "Factions"));
            Directory.CreateDirectory(Path.Combine(mapperDir, "Titles"));
            Directory.CreateDirectory(Path.Combine(mapperDir, "Cultures"));
            Directory.CreateDirectory(Path.Combine(mapperDir, "terrains"));

            // tag.txt
            File.WriteAllText(Path.Combine(mapperDir, "tag.txt"), "BookmarksPlus");

            // Time Period.xml
            File.WriteAllText(Path.Combine(mapperDir, "Time Period.xml"),
                "<?xml version=\"1.0\"?><TimePeriod><StartDate>1</StartDate><EndDate>768</EndDate></TimePeriod>");

            // Mods.xml
            File.WriteAllText(Path.Combine(mapperDir, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods><Mod>ufc_2933252806.mod</Mod></Mods>");

            return mapperDir;
        }

        /// <summary>
        /// Creates a mock unit cards localization directory for "fireforged empire".
        /// </summary>
        public string CreateFireforgedEmpireLocDir()
        {
            var locDir = Path.Combine(UnitsCardsNamesPath, "fireforged empire");
            Directory.CreateDirectory(locDir);

            // Sample .loc file with unit card names
            File.WriteAllText(Path.Combine(locDir, "unit_cards.loc"),
                "land_units_onscreen_name_att_unit_infantry_swordsmen\tFireforged Swordsmen\t\n" +
                "land_units_onscreen_name_att_unit_cavalry_knights\tFireforged Knights\t\n" +
                "land_units_onscreen_name_att_unit_archers_longbow\tFireforged Longbowmen\t\n");

            return locDir;
        }

        /// <summary>
        /// Creates a corrupted (invalid XML) UnitMappers.xml for edge-case testing.
        /// </summary>
        public void CreateCorruptedUnitMappersXml()
        {
            File.WriteAllText(Path.Combine(SettingsPath, "UnitMappers.xml"),
                "<?xml version=\"1.0\"?><UMOptions><UnitMappers name=\"DefaultCK3\">True</UnitMappers><!-- Missing closing tag");
        }

        /// <summary>
        /// Creates an Options.xml with default values.
        /// </summary>
        public void CreateOptionsXml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Options>");
            sb.AppendLine("  <Option name=\"CloseCK3\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"CloseAttila\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"FullArmies\">Disabled</Option>");
            sb.AppendLine("  <Option name=\"TimeLimit\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"BattleMapsSize\">Dynamic</Option>");
            sb.AppendLine("  <Option name=\"DefensiveDeployables\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"UnitCards\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"SeparateArmies\">Friendly Only</Option>");
            sb.AppendLine("  <Option name=\"SiegeEnginesInFieldBattles\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"ShowPostBattleReport\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"LeviesMax\">10</Option>");
            sb.AppendLine("  <Option name=\"RangedMax\">4</Option>");
            sb.AppendLine("  <Option name=\"InfantryMax\">8</Option>");
            sb.AppendLine("  <Option name=\"CavalryMax\">4</Option>");
            sb.AppendLine("  <Option name=\"BattleScale\">100%</Option>");
            sb.AppendLine("  <Option name=\"AutoScaleUnits\">Enabled</Option>");
            sb.AppendLine("  <Option name=\"CommanderWoundedChance\">65</Option>");
            sb.AppendLine("  <Option name=\"CommanderSeverelyInjuredChance\">10</Option>");
            sb.AppendLine("  <Option name=\"CommanderBrutallyMauledChance\">5</Option>");
            sb.AppendLine("  <Option name=\"CommanderMaimedChance\">5</Option>");
            sb.AppendLine("  <Option name=\"CommanderOneLeggedChance\">2</Option>");
            sb.AppendLine("  <Option name=\"CommanderOneEyedChance\">3</Option>");
            sb.AppendLine("  <Option name=\"CommanderDisfiguredChance\">2</Option>");
            sb.AppendLine("  <Option name=\"CommanderSlainChance\">8</Option>");
            sb.AppendLine("  <Option name=\"CommanderPrisonerChance\">60</Option>");
            sb.AppendLine("  <Option name=\"KnightWoundedChance\">65</Option>");
            sb.AppendLine("  <Option name=\"KnightSeverelyInjuredChance\">10</Option>");
            sb.AppendLine("  <Option name=\"KnightBrutallyMauledChance\">5</Option>");
            sb.AppendLine("  <Option name=\"KnightMaimedChance\">5</Option>");
            sb.AppendLine("  <Option name=\"KnightOneLeggedChance\">2</Option>");
            sb.AppendLine("  <Option name=\"KnightOneEyedChance\">3</Option>");
            sb.AppendLine("  <Option name=\"KnightDisfiguredChance\">2</Option>");
            sb.AppendLine("  <Option name=\"KnightSlainChance\">8</Option>");
            sb.AppendLine("  <Option name=\"KnightPrisonerChance\">60</Option>");
            sb.AppendLine("  <Option name=\"OptInPreReleases\">False</Option>");
            sb.AppendLine("  <Option name=\"SelectedCustomMapper\"></Option>");
            sb.AppendLine("  <Option name=\"CombineKnights\">Disabled</Option>");
            sb.AppendLine("  <Option name=\"LinuxSetupCompleted\">False</Option>");
            sb.AppendLine("</Options>");

            File.WriteAllText(Path.Combine(SettingsPath, "Options.xml"), sb.ToString());
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, true);
            }
            catch
            {
                // Best-effort cleanup; temp files will be cleaned by OS eventually
            }
        }
    }
}