using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CrusaderWars.data.save_file;
using CrusaderWars.twbattle;
using CrusaderWars.data.battle_results;
using CrusaderWars.unit_mapper;

namespace CrusaderWars
{
    /*
     * IMPORTANT NOTE
     * ----------------------------
     * The writter gives some extra new lines '\n'
     * might remove them later
     */
    internal static class Data
    {
        public static List<(string unitName, string declarationName)> units_scripts = new List<(string unitName, string declarationName)>();


        public static List<string> PlayerIDsAccolades = new List<string>();
        public static List<string> EnemyIDsAccolades = new List<string>();
        public static List<(string,string,string)> PlayerAccolades = new List<(string,string,string)>();
        public static List<(string,string,string)> EnemysAccolades = new List<(string,string,string)>();


        public static string PlayerCommanderAccoladeID = "";
        public static string EnemyCommanderAccoladeID = "";
        public static (string, string, string) PlayerCommanderAccolade;
        public static (string, string, string) EnemyCommanderAccolade;

        public static StringBuilder SB_Living = new StringBuilder();
        public static StringBuilder SB_Regiments = new StringBuilder();
        public static StringBuilder SB_ArmyRegiments = new StringBuilder();
        public static StringBuilder SB_Armies = new StringBuilder();
        public static StringBuilder SB_CombatResults = new StringBuilder();
        public static StringBuilder SB_Combats = new StringBuilder();
        public static StringBuilder SB_Counties = new StringBuilder();
        public static StringBuilder SB_Cultures = new StringBuilder();
        public static StringBuilder SB_Mercenaries = new StringBuilder();
        public static StringBuilder SB_Units = new StringBuilder();
		public static StringBuilder SB_CourtPositions = new StringBuilder();
        public static StringBuilder SB_LandedTitles = new StringBuilder();
        public static StringBuilder SB_Accolades = new StringBuilder();
        public static StringBuilder SB_Dynasties = new StringBuilder();
		public static StringBuilder SB_Traits = new StringBuilder();
        public static StringBuilder SB_Sieges = new StringBuilder(); // Added StringBuilder for Sieges
        public static StringBuilder SB_PlayedCharacter = new StringBuilder();
        public static StringBuilder SB_CurrentlyPlayedCharacters = new StringBuilder();



        // New properties to store original blocks for replacement
        public static string? Original_PlayedCharacter_Block;
        public static string? Original_CurrentlyPlayedCharacters_Block;


        //Sieges
        public static List<string> Province_Buildings = new List<string>();


        public static void Reset()
        {
            Program.Logger.Debug("Resetting all data containers and extraction flags.");
            // Clear lists and reset values
            Date.Reset();
            twbattle.Sieges.Reset();
            units_scripts.Clear();
            PlayerIDsAccolades = new List<string> ();
            EnemyIDsAccolades = new List<string>();
            PlayerAccolades = new List<(string, string, string)> ();
            EnemysAccolades = new List<(string, string, string)>();

            PlayerCommanderAccoladeID = "";
            EnemyCommanderAccoladeID = "";
            PlayerCommanderAccolade = ("","","");
            EnemyCommanderAccolade = ("","","");

            Original_PlayedCharacter_Block = null;
            Original_CurrentlyPlayedCharacters_Block = null;

            Province_Buildings.Clear();
            twbattle.BattleState.IsSiegeBattle = false;
            twbattle.BattleState.HasReliefArmy = false;
            UnitMappers_BETA.ClearProvinceMapCache();

            BattleResult.SiegeID = null; // Reset SiegeID
            BattleResult.Player_Combat = null; // Reset Player_Combat
            BattleResult.Original_Player_Combat = null; // Reset Original_Player_Combat
            BattleResult.Player_CombatResult = null; // Reset Player_CombatResult
            BattleResult.Original_Player_CombatResult = null; // Reset Original_Player_CombatResult
            BattleResult.WarScoreValue = null;


            // Reset extraction flags
            SearchKeys.HasTraitsExtracted = false;
            SearchKeys.HasCombatsExtracted = false;
            SearchKeys.HasLivingExtracted = false;
            SearchKeys.HasArmyRegimentsExtracted = false;
            SearchKeys.HasRegimentsExtracted = false;
            SearchKeys.HasArmiesExtracted = false;
            SearchKeys.HasBattleResultsExtracted = false;
            SearchKeys.HasCountiesExtracted = false;
            SearchKeys.HasCulturesExtracted = false;
            SearchKeys.HasMercenariesExtracted = false;
            SearchKeys.HasUnitsExtracted = false;
            SearchKeys.HasCourtPositionsExtracted = false;
            SearchKeys.HasLandedTitlesExtracted = false;
            SearchKeys.HasAccoladesExtracted = false;
            SearchKeys.HasDynastiesExtracted = false;
            SearchKeys.HasSiegesExtracted = false; // Added reset for Sieges extraction flag
            SearchKeys.HasPlayedCharacterExtracted = false;
            SearchKeys.HasCurrentlyPlayedCharactersExtracted = false;
        }
    }

    struct GetterKeys
    {
        static bool isSearchPermitted = false;
        static bool isSearchBuildingsPermitted = false;
        static bool isExtractBuildingsPermitted = false;
        public static void ReadProvinceBuildings(string line, string province_id)
        {
            
            if(line.Contains("provinces={"))
            {
                isSearchPermitted = true;
                Program.Logger.Debug("GetterKeys: province search permitted.");
            }

            
            if(isSearchPermitted && line.Contains($"\t{province_id}={{"))
            {
                isSearchBuildingsPermitted = true;
                Program.Logger.Debug($"GetterKeys: province buildings search permitted for province {province_id}.");
            }

            if(isSearchBuildingsPermitted)
            {
                if (line.Contains("culture="))
                {
                    string culture = Regex.Match(line, @"culture=(.+)").Groups[1].Value.Trim('"');
                    twbattle.Sieges.SetHoldingCulture(culture);
                    Program.Logger.Debug($"GetterKeys: Found culture: {culture}");
                }

                if (line.Contains("buildings={"))
                {
                    isExtractBuildingsPermitted = true;
                    Program.Logger.Debug("GetterKeys: building extraction permitted.");
                }
            }

            if(isExtractBuildingsPermitted)
            {
                if (line.Contains("type="))
                {

                    string building_key = Regex.Match(line, @"=(.+)").Groups[1].Value.Trim('"').Trim('/');
                    Data.Province_Buildings.Add(building_key);
                    Program.Logger.Debug($"GetterKeys: Found building key: {building_key}");
                }


            }


            //last line of the province data
            //stop searching
            if( isSearchBuildingsPermitted && line.Contains("fort_level="))
            {
                string fort_level = Regex.Match(line, @"=(.+)").Groups[1].Value.Trim('"').Trim('/');
                if(int.TryParse(fort_level, out int level))
                {
                    twbattle.Sieges.SetFortLevel(level);
                    Program.Logger.Debug($"GetterKeys: Found fort level: {level}");
                }
                else
                {
                    twbattle.Sieges.SetFortLevel(0);
                    Program.Logger.Debug("GetterKeys: Could not parse fort level, setting to 0.");
                }
                
                isExtractBuildingsPermitted = false;
                isSearchBuildingsPermitted = false;
                isSearchPermitted = false;
                Program.Logger.Debug("GetterKeys: Finished province search, resetting flags.");
                return;

            }
        } 
    };

    struct SearchKeys
    {
        private static bool Start_TraitsFound { get; set; }
        private static bool End_TraitsFound { get; set; }
        public static bool HasTraitsExtracted { get; set; }

        public static void TraitsList(string line)
        {
            if (!HasTraitsExtracted)
            {
                if (!Start_TraitsFound)
                {
                    if (line.Contains("traits_lookup={"))
                    {
                        Program.Logger.Debug("Found start of traits_lookup block.");
                        Start_TraitsFound = true;
                    }
                }

                if (Start_TraitsFound && !End_TraitsFound)
                {
                    if (line == "provinces={")
                    {
                        Program.Logger.Debug("Found end of traits_lookup block.");
                        Program.Logger.Debug($"Writing {Data.SB_Traits.Length} characters to Traits.txt");
                        File.WriteAllText(@".\data\save_file_data\Traits.txt", Data.SB_Traits.ToString());
                        Data.SB_Traits = new StringBuilder();
                        GC.Collect();
                        End_TraitsFound = true;
                        return;
                    }

                    Data.SB_Traits.AppendLine(line);
                }

                if (End_TraitsFound)
                {
                    HasTraitsExtracted = true;
                    Start_TraitsFound = false;
                    End_TraitsFound = false;
                }
            }
        }

        private static bool Start_CombatsFound { get; set; }
        private static bool End_CombatsFound { get; set; }
        public static bool HasCombatsExtracted { get; set; }

        public static void Combats(string line)
        {
            if(!HasCombatsExtracted)
            {
                if (!Start_CombatsFound)
                {
                    if (line == "\tcombats={") {
                        Program.Logger.Debug("Found start of combats block.");
                        Start_CombatsFound = true; 
                    }
                }

                if(Start_CombatsFound && !End_CombatsFound)
                {
                    Data.SB_Combats.AppendLine(line); // Moved to the beginning of the block
                    if (line == "\t}") // Modified: Match specific indentation
                    {
                        Program.Logger.Debug("Found end of combats block.");
                        Program.Logger.Debug($"Writing {Data.SB_Combats.Length} characters to Combats.txt");
                        //Write Combats Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Combats.txt", Data.SB_Combats.ToString());
                        Data.SB_Combats = new StringBuilder();
                        GC.Collect();

                        End_CombatsFound = true; 
                        // Removed: return;
                    }
                }

                if(End_CombatsFound)
                {
                    HasCombatsExtracted = true;
                    Start_CombatsFound = false; 
                    End_CombatsFound = false;
                }
            }
        }

        private static bool Start_BattleResultsFound { get; set; }
        private static bool End_BattleResultsFound { get; set; }
        public static bool HasBattleResultsExtracted { get; set; }

        public static void BattleResults(string line)
        {
            if (!HasBattleResultsExtracted)
            {
                if (!Start_BattleResultsFound)
                {
                    if (line == "\tcombat_results={")
                    {
                        Program.Logger.Debug("Found start of combat_results block.");
                        Start_BattleResultsFound = true;
                    }
                }
                


                if (Start_BattleResultsFound && !End_BattleResultsFound)
                {
                    Data.SB_CombatResults.AppendLine(line); // Moved to the beginning of the block
                    if (line == "\t}") // Modified: Match specific indentation
                    {
                        Program.Logger.Debug("Found end of combat_results block.");
                        Program.Logger.Debug($"Writing {Data.SB_CombatResults.Length} characters to CombatResults.txt");
                        //Write CombatResults Data to txt file
                        File.WriteAllText(@".\data\save_file_data\CombatResults.txt", Data.SB_CombatResults.ToString());
                        Data.SB_CombatResults = new StringBuilder();
                        GC.Collect();

                        End_BattleResultsFound = true;
                        // Removed: return;
                    }
                }

                if (End_BattleResultsFound)
                {
                    HasBattleResultsExtracted = true;
                    Start_BattleResultsFound = false;
                    End_BattleResultsFound = false;
                }
            }
        }

        private static bool Start_RegimentsFound { get; set; }
        private static bool End_RegimentsFound { get; set; }
        public static bool HasRegimentsExtracted { get; set; }
        public static void Regiments(string line)
        {
            if (!HasRegimentsExtracted)
            {
                if (!Start_RegimentsFound)
                {
                    if (line == "\tregiments={") {
                        Program.Logger.Debug("Found start of regiments block.");
                        Start_RegimentsFound = true;
                    }
                }

                if (Start_RegimentsFound && !End_RegimentsFound)
                {
                  
                    if (line == "\tarmy_regiments={") 
                    {
                        Program.Logger.Debug("Found end of regiments block.");
                        Program.Logger.Debug($"Writing {Data.SB_Regiments.Length} characters to Regiments.txt");
                        //Write Regiments Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Regiments.txt", Data.SB_Regiments.ToString());
                        Data.SB_Regiments = new StringBuilder();
                        GC.Collect();
                        End_RegimentsFound = true; 
                        return;
                    }

                    Data.SB_Regiments.AppendLine(line);
                }

                if (End_RegimentsFound)
                {
                    HasRegimentsExtracted = true;
                    Start_RegimentsFound = false; 
                    End_RegimentsFound = false;
                }
            }
        }

        private static bool Start_ArmyRegimentsFound { get; set; }
        private static bool End_ArmyRegimentsFound { get; set; }
        public static bool HasArmyRegimentsExtracted { get; set; }
        public static void ArmyRegiments(string line)
        {
            if (!HasArmyRegimentsExtracted)
            {
                if (!Start_ArmyRegimentsFound)
                {
                    if (line == "\tarmy_regiments={") {
                        Program.Logger.Debug("Found start of army_regiments block.");
                        Start_ArmyRegimentsFound = true;
                    }
                }

                if (Start_ArmyRegimentsFound && !End_ArmyRegimentsFound)
                {
            
                    if (line == "\tarmies={") 
                    {
                        Program.Logger.Debug("Found end of army_regiments block.");
                        Program.Logger.Debug($"Writing {Data.SB_ArmyRegiments.Length} characters to ArmyRegiments.txt");
                        //Write ArmyRegiments Data to txt file
                        File.WriteAllText(@".\data\save_file_data\ArmyRegiments.txt", Data.SB_ArmyRegiments.ToString());
                        Data.SB_ArmyRegiments = new StringBuilder();
                        GC.Collect();
                        End_ArmyRegimentsFound = true; 
                        return;
                    }

                    Data.SB_ArmyRegiments.AppendLine(line);
                }

                if (End_ArmyRegimentsFound)
                {
                    HasArmyRegimentsExtracted = true;
                    Start_ArmyRegimentsFound = false;
                    End_ArmyRegimentsFound = false;
                }
            }
        }

        private static bool Start_ArmiesFound { get; set; }
        private static bool End_ArmiesFound { get; set; }
        public static bool HasArmiesExtracted { get; set; }
        public static void Armies(string line)
        {
            if (!HasArmiesExtracted)
            {
                if (!Start_ArmiesFound)
                {
                    if (line == "\tarmies={") {
                        Program.Logger.Debug("Found start of armies block.");
                        Start_ArmiesFound = true;
                    }
                }

                if (Start_ArmiesFound && !End_ArmiesFound)
                {

                    if (line == "\t}")
                    {
                        Program.Logger.Debug("Found end of armies block.");
                        Program.Logger.Debug($"Writing {Data.SB_Armies.Length} characters to Armies.txt");
                        //Write Armies Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Armies.txt", Data.SB_Armies.ToString());
                        Data.SB_Armies = new StringBuilder();
                        GC.Collect();
                        End_ArmiesFound = true;
                        return;
                    }

                    Data.SB_Armies.AppendLine(line);
                }

                if (End_ArmiesFound)
                {
                    HasArmiesExtracted = true;
                    Start_ArmiesFound = false;
                    End_ArmiesFound = false;
                }
            }
        }

        private static bool Start_LivingFound { get; set; }
        private static bool End_LivingFound { get; set; }
        public static bool HasLivingExtracted { get; set; }
        public static void Living(string line)
        {
            if (!HasLivingExtracted)
            {
                if (!Start_LivingFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "living={") {
                        Program.Logger.Debug("Found start of living block.");
                        Start_LivingFound = true;
                    }
                }

                if (Start_LivingFound && !End_LivingFound)
                {
                    if (line == "dead_unprunable={") 
                    {
                        Program.Logger.Debug("Found end of living block.");
                        Program.Logger.Debug($"Writing {Data.SB_Living.Length} characters to Living.txt");
                        //Write Living Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Living.txt", Data.SB_Living.ToString());
                        Data.SB_Living = new StringBuilder();
                        GC.Collect();
                        End_LivingFound = true;
                        return;
                    }
                    
                    Data.SB_Living.AppendLine(line);

                }

                if (End_LivingFound)
                {
                    HasLivingExtracted = true;
                    Start_LivingFound = false;
                    End_LivingFound = false;
                }
            }
        }

        private static bool Start_CountiesFound { get; set; }
        private static bool End_CountiesFound { get; set; }
        public static bool HasCountiesExtracted { get; set; }
        public static void Counties(string line)
        {
            if (!HasCountiesExtracted)
            {
                if (!Start_CountiesFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "\tcounties={") 
                    {
                        Program.Logger.Debug("Found start of counties block.");
                        Start_CountiesFound = true;
                    }
                }

                if (Start_CountiesFound && !End_CountiesFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of counties block.");
                        Program.Logger.Debug($"Writing {Data.SB_Counties.Length} characters to Counties.txt");
                        //Write Counties Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Counties.txt", Data.SB_Counties.ToString());
                        Data.SB_Counties = new StringBuilder();
                        GC.Collect();
                        End_CountiesFound = true;
                        return;
                    }

                    Data.SB_Counties.AppendLine(line);
                }

                if (End_CountiesFound)
                {
                    HasCountiesExtracted = true;
                    Start_CountiesFound = false;
                    End_CountiesFound = false;
                }
            }
        }

        private static bool Start_UnitsFound { get; set; }
        private static bool End_UnitsFound { get; set; }
        public static bool HasUnitsExtracted { get; set; }
        public static void Units(string line)
        {
            if (!HasUnitsExtracted)
            {
                if (!Start_UnitsFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "units={")
                    {
                        Program.Logger.Debug("Found start of units block.");
                        Start_UnitsFound = true;
                    }
                }

                if (Start_UnitsFound && !End_UnitsFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of units block.");
                        Program.Logger.Debug($"Writing {Data.SB_Units.Length} characters to Units.txt");
                        //Write Units Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Units.txt", Data.SB_Units.ToString());
                        Data.SB_Units = new StringBuilder();
                        GC.Collect();
                        End_UnitsFound = true;
                        return;
                    }

                    Data.SB_Units.AppendLine(line);
                }

                if (End_UnitsFound)
                {
                    HasUnitsExtracted = true;
                    Start_UnitsFound = false;
                    End_UnitsFound = false;
                }
            }
        }

		private static bool Start_CourtPositionsFound { get; set; }
		private static bool End_CourtPositionsFound { get; set; }
		public static bool HasCourtPositionsExtracted { get; set; }
		public static void CourtPositions(string line)
		{
			if (!HasCourtPositionsExtracted)
			{
				if (!Start_CourtPositionsFound)
				{
					//Match start = Regex.Match(line, @"living={");
					if (line == "court_positions={")
					{
						Program.Logger.Debug("Found start of court_positions block.");
						Start_CourtPositionsFound = true;
					}
				}

				if (Start_CourtPositionsFound && !End_CourtPositionsFound)
				{
					if (line == "}")
					{
						Program.Logger.Debug("Found end of court_positions block.");
						Program.Logger.Debug($"Writing {Data.SB_CourtPositions.Length} characters to CourtPositions.txt");
						//Write Units Data to txt file
						File.WriteAllText(@".\data\save_file_data\CourtPositions.txt", Data.SB_CourtPositions.ToString());
						Data.SB_CourtPositions = new StringBuilder();
						GC.Collect();
						End_CourtPositionsFound = true;
						return;
					}

					Data.SB_CourtPositions.AppendLine(line);
				}

				if (End_CourtPositionsFound)
				{
					HasCourtPositionsExtracted = true;
					Start_CourtPositionsFound = false;
					End_CourtPositionsFound = false;
				}
			}
		}

        private static bool Start_CulturesFound { get; set; }
        private static bool End_CulturesFound { get; set; }
        public static bool HasCulturesExtracted { get; set; }
        public static void Cultures(string line)
        {
            if (!HasCulturesExtracted)
            {
                if (!Start_CulturesFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "culture_manager={")
                    {
                        Program.Logger.Debug("Found start of culture_manager block.");
                        Start_CulturesFound = true;
                    }
                }

                if (Start_CulturesFound && !End_CulturesFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of culture_manager block.");
                        Program.Logger.Debug($"Writing {Data.SB_Cultures.Length} characters to Cultures.txt");
                        //Write Cultures Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Cultures.txt", Data.SB_Cultures.ToString());
                        Data.SB_Cultures = new StringBuilder();
                        GC.Collect();
                        End_CulturesFound = true;
                        return;
                    }

                    Data.SB_Cultures.AppendLine(line);
                
                }

                if (End_CulturesFound)
                {
                    HasCulturesExtracted = true;
                    Start_CulturesFound = false;
                    End_CulturesFound = false;
                }
            }
        }

        private static bool Start_MercenariesFound { get; set; }
        private static bool End_MercenariesFound { get; set; }
        public static bool HasMercenariesExtracted { get; set; }
        public static void Mercenaries(string line)
        {

            if (!HasMercenariesExtracted)
            {
                if (!Start_MercenariesFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "mercenary_company_manager={")
                    {
                        Program.Logger.Debug("Found start of mercenary_company_manager block.");
                        Start_MercenariesFound = true;
                    }
                }

                if (Start_MercenariesFound && !End_MercenariesFound)
                {

                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of mercenary_company_manager block.");
                        Program.Logger.Debug($"Writing {Data.SB_Mercenaries.Length} characters to Mercenaries.txt");
                        //Write Mercenaries Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Mercenaries.txt", Data.SB_Mercenaries.ToString());
                        Data.SB_Mercenaries = new StringBuilder();
                        GC.Collect();
                        End_MercenariesFound = true;
                        return;
                    }

                    Data.SB_Mercenaries.AppendLine(line);

                }

                if (End_MercenariesFound)
                {
                    HasMercenariesExtracted = true;
                    Start_MercenariesFound = false;
                    End_MercenariesFound = false;
                }
            }
        }

        private static bool Start_LandedTitlesFound { get; set; }
        private static bool End_LandedTitlesFound { get; set; }
        public static bool HasLandedTitlesExtracted { get; set; }
        public static void LandedTitles(string line)
        {

            if (!HasLandedTitlesExtracted)
            {
                if (!Start_LandedTitlesFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "\tlanded_titles={")
                    {
                        Program.Logger.Debug("Found start of landed_titles block.");
                        Start_LandedTitlesFound = true;
                    }
                }

                if (Start_LandedTitlesFound && !End_LandedTitlesFound)
                {

                    if (line.StartsWith("\tindex="))
                    {
                        Program.Logger.Debug("Found end of landed_titles block.");
                        Program.Logger.Debug($"Writing {Data.SB_LandedTitles.Length} characters to LandedTitles.txt");
                        //Write Landed Titles Data to txt file
                        File.WriteAllText(@".\data\save_file_data\LandedTitles.txt", Data.SB_LandedTitles.ToString());
                        Data.SB_LandedTitles = new StringBuilder();
                        GC.Collect();
                        End_LandedTitlesFound = true;
                        return;
                    }

                    Data.SB_LandedTitles.AppendLine(line);

                }

                if (End_LandedTitlesFound)
                {
                    HasLandedTitlesExtracted = true;
                    Start_LandedTitlesFound = false;
                    End_LandedTitlesFound = false;
                }
            }
        }


        private static bool Start_AccoladesFound { get; set; }
        private static bool End_AccoladesFound { get; set; }
        public static bool HasAccoladesExtracted { get; set; }
        public static void Accolades(string line)
        {

            if (!HasAccoladesExtracted)
            {
                if (!Start_AccoladesFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "accolades={")
                    {
                        Program.Logger.Debug("Found start of accolades block.");
                        Start_AccoladesFound = true;
                    }
                }

                if (Start_AccoladesFound && !End_AccoladesFound)
                {

                    if (line.StartsWith("tax_slot_manager={"))
                    {
                        Program.Logger.Debug("Found end of accolades block.");
                        Program.Logger.Debug($"Writing {Data.SB_Accolades.Length} characters to Accolades.txt");
                        //Write Accolades Data to txt file
                        File.WriteAllText(@".\data\save_file_data\Accolades.txt", Data.SB_Accolades.ToString());
                        Data.SB_Accolades = new StringBuilder();
                        GC.Collect();
                        End_AccoladesFound = true;
                        return;
                    }

                    Data.SB_Accolades.AppendLine(line);

                }

                if (End_AccoladesFound)
                {
                    HasAccoladesExtracted = true;
                    Start_AccoladesFound = false;
                    End_AccoladesFound = false;
                }
            }
        }

        private static bool Start_DynastiesFound { get; set; }
        private static bool End_DynastiesFound { get; set; }
        public static bool HasDynastiesExtracted { get; set; }
        private static int dynastiesBraceCount = 0;
        public static void Dynasties(string line)
        {
            if (!HasDynastiesExtracted)
            {
                if (!Start_DynastiesFound)
                {
                    if (line == "dynasties={")
                    {
                        Program.Logger.Debug("Found start of dynasties block.");
                        Start_DynastiesFound = true;
                        dynastiesBraceCount = 0; // Reset before counting
                    }
                }

                if (Start_DynastiesFound && !End_DynastiesFound)
                {
                    Data.SB_Dynasties.AppendLine(line);

                    // Count braces in the current line to handle multiple braces on one line
                    foreach (char c in line)
                    {
                        if (c == '{')
                        {
                            dynastiesBraceCount++;
                        }
                        else if (c == '}')
                        {
                            dynastiesBraceCount--;
                        }
                    }

                    if (dynastiesBraceCount == 0 && Start_DynastiesFound)
                    {
                        Program.Logger.Debug("Found end of dynasties block by bracket counting.");
                        Program.Logger.Debug($"Writing {Data.SB_Dynasties.Length} characters to Dynasties.txt");
                        File.WriteAllText(@".\data\save_file_data\Dynasties.txt", Data.SB_Dynasties.ToString());
                        Data.SB_Dynasties = new StringBuilder();
                        GC.Collect();
                        End_DynastiesFound = true;
                    }
                }

                if (End_DynastiesFound)
                {
                    HasDynastiesExtracted = true;
                    Start_DynastiesFound = false;
                    End_DynastiesFound = false;
                    dynastiesBraceCount = 0; // Reset for next file read
                }
            }
        }


        private static bool Start_PlayedCharacterFound { get; set; }
        private static bool End_PlayedCharacterFound { get; set; }
        public static bool HasPlayedCharacterExtracted { get; set; }
        private static int playedCharacterBraceCount = 0;
        public static void PlayedCharacter(string line)
        {
            if (!HasPlayedCharacterExtracted)
            {
                if (!Start_PlayedCharacterFound)
                {
                    if (line.StartsWith("played_character={"))
                    {
                        Program.Logger.Debug("Found start of played_character block.");
                        Start_PlayedCharacterFound = true;
                        playedCharacterBraceCount = 0; // Reset
                    }
                }

                if (Start_PlayedCharacterFound && !End_PlayedCharacterFound)
                {
                    Data.SB_PlayedCharacter.AppendLine(line);

                    foreach (char c in line)
                    {
                        if (c == '{') playedCharacterBraceCount++;
                        else if (c == '}') playedCharacterBraceCount--;
                    }

                    if (playedCharacterBraceCount == 0 && Start_PlayedCharacterFound)
                    {
                        Program.Logger.Debug("Found end of played_character block by bracket counting.");
                        Data.Original_PlayedCharacter_Block = Data.SB_PlayedCharacter.ToString();
                        Program.Logger.Debug($"Writing {Data.Original_PlayedCharacter_Block.Length} characters to PlayedCharacter.txt");
                        File.WriteAllText(@".\data\save_file_data\PlayedCharacter.txt", Data.Original_PlayedCharacter_Block);
                        Data.SB_PlayedCharacter = new StringBuilder();
                        GC.Collect();
                        End_PlayedCharacterFound = true;
                    }
                }

                if (End_PlayedCharacterFound)
                {
                    HasPlayedCharacterExtracted = true;
                    Start_PlayedCharacterFound = false;
                    End_PlayedCharacterFound = false;
                    playedCharacterBraceCount = 0;
                }
            }
        }

        private static bool Start_CurrentlyPlayedCharactersFound { get; set; }
        private static bool End_CurrentlyPlayedCharactersFound { get; set; }
        public static bool HasCurrentlyPlayedCharactersExtracted { get; set; }
        public static void CurrentlyPlayedCharacters(string line)
        {
            if (!HasCurrentlyPlayedCharactersExtracted)
            {
                if (!Start_CurrentlyPlayedCharactersFound)
                {
                    if (line.StartsWith("currently_played_characters={"))
                    {
                        Program.Logger.Debug("Found start of currently_played_characters block.");
                        Start_CurrentlyPlayedCharactersFound = true;
                    }
                }

                if (Start_CurrentlyPlayedCharactersFound && !End_CurrentlyPlayedCharactersFound)
                {
                    Data.SB_CurrentlyPlayedCharacters.AppendLine(line);

                    if (line.Contains("}"))
                    {
                        Program.Logger.Debug("Found end of currently_played_characters block.");
                        Data.Original_CurrentlyPlayedCharacters_Block = Data.SB_CurrentlyPlayedCharacters.ToString();
                        Program.Logger.Debug($"Writing {Data.Original_CurrentlyPlayedCharacters_Block.Length} characters to CurrentlyPlayedCharacters.txt");
                        File.WriteAllText(@".\data\save_file_data\CurrentlyPlayedCharacters.txt", Data.Original_CurrentlyPlayedCharacters_Block);
                        Data.SB_CurrentlyPlayedCharacters = new StringBuilder();
                        GC.Collect();
                        End_CurrentlyPlayedCharactersFound = true;
                    }
                }

                if (End_CurrentlyPlayedCharactersFound)
                {
                    HasCurrentlyPlayedCharactersExtracted = true;
                    Start_CurrentlyPlayedCharactersFound = false;
                    End_CurrentlyPlayedCharactersFound = false;
                }
            }
        }


        // New properties for Sieges extraction
        private static bool Start_SiegesFound { get; set; }
        private static bool End_SiegesFound { get; set; }
        public static bool HasSiegesExtracted { get; set; }

        // New method for Sieges extraction
        public static void Sieges(string line)
        {
            if (!HasSiegesExtracted)
            {
                if (!Start_SiegesFound)
                {
                    if (line == "sieges={") // Assuming top-level block, similar to "units={"
                    {
                        Program.Logger.Debug("Found start of sieges block.");
                        Start_SiegesFound = true;
                    }
                }

                if (Start_SiegesFound && !End_SiegesFound)
                {
                    // Move AppendLine to the beginning of the block
                    Data.SB_Sieges.AppendLine(line);

                    // The plan specifies "the closing brace } at the same nesting level".
                    // For top-level blocks like "units={", "counties={", "culture_manager={", "mercenary_company_manager={",
                    // the end condition is a simple "}".
                    if (line == "}") 
                    {
                        Program.Logger.Debug("Found end of sieges block.");
                        Program.Logger.Debug($"Writing {Data.SB_Sieges.Length} characters to Sieges.txt");
                        File.WriteAllText(@".\data\save_file_data\Sieges.txt", Data.SB_Sieges.ToString());
                        Data.SB_Sieges = new StringBuilder();
                        GC.Collect();
                        End_SiegesFound = true;
                        // Removed: return;
                    }
                }

                if (End_SiegesFound)
                {
                    HasSiegesExtracted = true;
                    Start_SiegesFound = false;
                    End_SiegesFound = false;
                }
            }
        }
    }
}
