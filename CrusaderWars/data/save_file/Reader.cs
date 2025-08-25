using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using CrusaderWars.data.save_file;
using CrusaderWars.twbattle;

namespace CrusaderWars
{
    /*
     * IMPORTANT NOTE
     * ----------------------------
     * The writter gives some extra new lines '\n'
     * might remove them later
     */
    internal static class Reader
    {

        static void ClearFilesData()
        {
            Program.Logger.Debug("Clearing all temporary save file data...");
            //Clear Battle Results File
            File.WriteAllText(Writter.DataFilesPaths.CombatResults_Path(), "");
            //Clear Battle Results TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.CombatResults_Path(), "");

            //Clear Combats File
            File.WriteAllText(Writter.DataFilesPaths.Combats_Path(), "");
            //Clear Combats TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.Combats_Path(), "");

            //Clear Regiments File
            File.WriteAllText(Writter.DataFilesPaths.Regiments_Path(), "");
            //Clear Regiments TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.Regiments_Path(), "");

            //Clear Army Regiments File
            File.WriteAllText(Writter.DataFilesPaths.ArmyRegiments_Path(), "");
            //Clear Army Regiments TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.ArmyRegiments_Path(), "");

            //Clear Living Regiments File
            File.WriteAllText(Writter.DataFilesPaths.Living_Path(), "");
            //Clear Living Regiments TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.Living_Path(), "");

            //Clear Armies File
            File.WriteAllText(Writter.DataFilesPaths.Armies_Path(), "");

            //Clear Counties File
            File.WriteAllText(Writter.DataFilesPaths.Counties_Path(), "");

            //Clear Cultures File
            File.WriteAllText(Writter.DataFilesPaths.Cultures_Path(), "");

            //Clear Mercenaries File
            File.WriteAllText(Writter.DataFilesPaths.Mercenaries_Path(), "");

            //Clear Traits File
            File.WriteAllText(Writter.DataFilesPaths.Traits_Path(), "");

            //Clear Units File
            File.WriteAllText(Writter.DataFilesPaths.Units_Path(), "");

            //Clear Court Positions File
            File.WriteAllText(Writter.DataFilesPaths.CourtPositions_Path(), "");

            //Clear Landed Titles File
            File.WriteAllText(Writter.DataFilesPaths.LandedTitles(), "");

            //Clear Accolades File
            File.WriteAllText(Writter.DataFilesPaths.Accolades(), "");
            Program.Logger.Debug("Finished clearing all temporary save file data.");
        }

        /// <summary>  
        /// Reads the ck3 save file for all the needed data.  
        /// </summary>  
        /// <param name="savePath">Path to the ck3 save file</param>  
        public static void ReadFile(string savePath)
        {
            Program.Logger.Debug($"Starting to read save file: {Path.GetFullPath(savePath)}");
            //Clean all data in save file data files
            Program.Logger.Debug("Clearing previous save data from temp files...");
            ClearFilesData();
            Program.Logger.Debug("Previous save data cleared.");
            long startMemoryt = GC.GetTotalMemory(false);

            using (FileStream saveFile = File.Open(savePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(saveFile))
            {
                string line = reader.ReadLine();
                int lineCount = 0;
                while (line != null && !reader.EndOfStream)
                {
                    lineCount++;
                    if (lineCount % 500000 == 0)
                    {
                        Program.Logger.Debug($"... Read {lineCount} lines from save file ...");
                    }
                    line = reader.ReadLine();
                    //GetterKeys.ReadProvinceBuildings(line, "5984");


                    SearchKeys.TraitsList(line);
                    
                    SearchKeys.BattleResults(line);
                    SearchKeys.Combats(line);
                    SearchKeys.Regiments(line);
                    SearchKeys.ArmyRegiments(line);
                    SearchKeys.Living(line);


                    SearchKeys.Armies(line);
                    SearchKeys.Counties(line);
                    SearchKeys.Cultures(line);
                    SearchKeys.Mercenaries(line);
                    SearchKeys.Units(line);
                    SearchKeys.CourtPositions(line);
                    SearchKeys.LandedTitles(line);
                    SearchKeys.Accolades(line);
                    
                }
                long endMemoryt = GC.GetTotalMemory(false);
                long memoryUsaget = endMemoryt - startMemoryt;
                Program.Logger.Debug($"Finished reading save file. Memory Usage: {memoryUsaget / 1048576} megabytes");
                reader.Close();
                saveFile.Close();
            }


            GC.Collect();

        }
    }




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



        //Sieges
        public static List<string> Province_Buildings = new List<string>();


        public static void Reset()
        {
            Program.Logger.Debug("Resetting all data containers and extraction flags.");
            units_scripts.Clear();
            PlayerIDsAccolades = new List<string> ();
            EnemyIDsAccolades = new List<string>();
            PlayerAccolades = new List<(string, string, string)> ();
            EnemysAccolades = new List<(string, string, string)>();

            PlayerCommanderAccoladeID = "";
            EnemyCommanderAccoladeID = "";
            PlayerCommanderAccolade = ("","","");
            EnemyCommanderAccolade = ("","","");


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

            if(isSearchBuildingsPermitted && line.Contains("buildings={"))
            {
                isExtractBuildingsPermitted = true;
                Program.Logger.Debug("GetterKeys: building extraction permitted.");
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
                    Sieges.SetFortLevel(level);
                    Program.Logger.Debug($"GetterKeys: Found fort level: {level}");
                }
                else
                {
                    Sieges.SetFortLevel(0);
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
                    else { Start_TraitsFound = false; }
                }

                if (Start_TraitsFound && !End_TraitsFound)
                {
 
                    if (line == "provinces={")
                    {
                        Program.Logger.Debug("Found end of traits_lookup block.");
                        //SaveFile.ReadWoundedTraits();
                        return;
                    }
                    else { End_TraitsFound = false; }

                    using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Traits.txt"))
                    {
                        sw.WriteLine(line);
                    }

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
                    else { Start_CombatsFound = false; }
                }

                if(Start_CombatsFound && !End_CombatsFound)
                {
                    if (line == "pending_character_interactions={") 
                    {
                        Program.Logger.Debug("Found end of combats block.");
                        Program.Logger.Debug($"Writing {Data.SB_Combats.Length} characters to Combats.txt");
                        //Write Combats Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Combats.txt"))
                        {
                            sw.Write(Data.SB_Combats);
                            sw.Close();
                        }
                        Data.SB_Combats = new StringBuilder();
                        GC.Collect();

                        End_CombatsFound = true; 
                        return;
                    }
                    else { End_CombatsFound = false; }

                    Data.SB_Combats.AppendLine(line);

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
                    else { Start_BattleResultsFound = false; }
                }
                


                if (Start_BattleResultsFound && !End_BattleResultsFound)
                {
                    if (line == "\tcombats={")
                    {
                        Program.Logger.Debug("Found end of combat_results block.");
                        Program.Logger.Debug($"Writing {Data.SB_CombatResults.Length} characters to BattleResults.txt");
                        //Write CombatResults Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\BattleResults.txt"))
                        {
                            sw.Write(Data.SB_CombatResults);
                            sw.Close();
                        }
                        Data.SB_CombatResults = new StringBuilder();
                        GC.Collect();

                        End_BattleResultsFound = true;
                        return;
                    }
                    else { End_BattleResultsFound = false; }

                    Data.SB_CombatResults.AppendLine(line);

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
                    else { Start_RegimentsFound = false; }
                }

                if (Start_RegimentsFound && !End_RegimentsFound)
                {
                  
                    if (line == "\tarmy_regiments={") 
                    {
                        Program.Logger.Debug("Found end of regiments block.");
                        Program.Logger.Debug($"Writing {Data.SB_Regiments.Length} characters to Regiments.txt");
                        //Write Regiments Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Regiments.txt"))
                        {
                            sw.Write(Data.SB_Regiments);
                            sw.Close();
                        }
                        Data.SB_Regiments = new StringBuilder();
                        GC.Collect();
                        End_RegimentsFound = true; 
                        return;
                    }
                    else { End_RegimentsFound = false; }

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
                    else { Start_ArmyRegimentsFound = false; }
                }

                if (Start_ArmyRegimentsFound && !End_ArmyRegimentsFound)
                {
                    
                    if (line == "\tarmies={") 
                    {
                        Program.Logger.Debug("Found end of army_regiments block.");
                        Program.Logger.Debug($"Writing {Data.SB_ArmyRegiments.Length} characters to ArmyRegiments.txt");
                        //Write ArmyRegiments Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\ArmyRegiments.txt"))
                        {
                            sw.Write(Data.SB_ArmyRegiments);
                            sw.Close();
                        }
                        Data.SB_ArmyRegiments = new StringBuilder();
                        GC.Collect();
                        End_ArmyRegimentsFound = true; 
                        return;
                    }
                    else { End_ArmyRegimentsFound = false; }

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
                    else { Start_ArmiesFound = false; }
                }

                if (Start_ArmiesFound && !End_ArmiesFound)
                {

                    if (line == "\t}")
                    {
                        Program.Logger.Debug("Found end of armies block.");
                        Program.Logger.Debug($"Writing {Data.SB_Armies.Length} characters to Armies.txt");
                        //Write Armies Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Armies.txt"))
                        {
                            sw.Write(Data.SB_Armies);
                            sw.Close();
                        }
                        Data.SB_Armies = new StringBuilder();
                        GC.Collect();
                        End_ArmiesFound = true;
                        return;
                    }
                    else { End_ArmiesFound = false; }

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
                    else { Start_LivingFound = false; }
                }

                if (Start_LivingFound && !End_LivingFound)
                {
                    if (line == "dead_unprunable={") 
                    {
                        Program.Logger.Debug("Found end of living block.");
                        Program.Logger.Debug($"Writing {Data.SB_Living.Length} characters to Living.txt");
                        //Write Living Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Living.txt"))
                        {
                            sw.Write(Data.SB_Living);
                            sw.Close();
                        }
                        Data.SB_Living = new StringBuilder();
                        GC.Collect();
                        End_LivingFound = true;
                        return;
                    }
                    else { End_LivingFound = false; }
                    
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
                    else { Start_CountiesFound = false; }
                }

                if (Start_CountiesFound && !End_CountiesFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of counties block.");
                        Program.Logger.Debug($"Writing {Data.SB_Counties.Length} characters to Counties.txt");
                        //Write Counties Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Counties.txt"))
                        {
                            sw.Write(Data.SB_Counties);
                            sw.Close();
                        }
                        Data.SB_Counties = new StringBuilder();
                        GC.Collect();
                        End_CountiesFound = true;
                        return;
                    }
                    else { End_CountiesFound = false; }

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
                    else { Start_UnitsFound = false; }
                }

                if (Start_UnitsFound && !End_UnitsFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of units block.");
                        Program.Logger.Debug($"Writing {Data.SB_Units.Length} characters to Units.txt");
                        //Write Units Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Units.txt"))
                        {
                            sw.Write(Data.SB_Units);
                            sw.Close();
                        }
                        Data.SB_Units = new StringBuilder();
                        GC.Collect();
                        End_UnitsFound = true;
                        return;
                    }
                    else { End_UnitsFound = false; }

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
                    else { Start_CourtPositionsFound = false; }
                }

                if (Start_CourtPositionsFound && !End_CourtPositionsFound)
                {
                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of court_positions block.");
                        Program.Logger.Debug($"Writing {Data.SB_CourtPositions.Length} characters to CourtPositions.txt");
                        //Write Units Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\CourtPositions.txt"))
                        {
                            sw.Write(Data.SB_CourtPositions);
                            sw.Close();
                        }
                        Data.SB_CourtPositions = new StringBuilder();
                        GC.Collect();
                        End_CourtPositionsFound = true;
                        return;
                    }
                    else { End_CourtPositionsFound = false; }

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
                    else { Start_CulturesFound = false; }
                }

                if (Start_CulturesFound && !End_CulturesFound)
                {
                    if (line == "mercenary_company_manager={")
                    {
                        Program.Logger.Debug("Found end of culture_manager block.");
                        Program.Logger.Debug($"Writing {Data.SB_Cultures.Length} characters to Cultures.txt");
                        //Write Cultures Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Cultures.txt"))
                        {
                            sw.Write(Data.SB_Cultures);
                            sw.Close();
                        }
                        Data.SB_Cultures = new StringBuilder();
                        GC.Collect();
                        End_CulturesFound = true;
                        return;
                    }
                    else { End_CulturesFound = false; }

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
                    else { Start_MercenariesFound = false; }
                }

                if (Start_MercenariesFound && !End_MercenariesFound)
                {

                    if (line == "}")
                    {
                        Program.Logger.Debug("Found end of mercenary_company_manager block.");
                        Program.Logger.Debug($"Writing {Data.SB_Mercenaries.Length} characters to Mercenaries.txt");
                        //Write Mercenaries Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Mercenaries.txt"))
                        {
                            sw.Write(Data.SB_Mercenaries);
                            sw.Close();
                        }
                        Data.SB_Mercenaries = new StringBuilder();
                        GC.Collect();
                        End_MercenariesFound = true;
                        return;
                    }
                    else { HasMercenariesExtracted = false; }
                    
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
                    else { Start_LandedTitlesFound = false; }
                }

                if (Start_LandedTitlesFound && !End_LandedTitlesFound)
                {

                    if (line.StartsWith("\tindex="))
                    {
                        Program.Logger.Debug("Found end of landed_titles block.");
                        Program.Logger.Debug($"Writing {Data.SB_LandedTitles.Length} characters to LandedTitles.txt");
                        //Write Landed Titles Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\LandedTitles.txt"))
                        {
                            sw.Write(Data.SB_LandedTitles);
                            sw.Close();
                        }
                        Data.SB_LandedTitles = new StringBuilder();
                        GC.Collect();
                        End_LandedTitlesFound = true;
                        return;
                    }
                    else { HasLandedTitlesExtracted = false; }

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
                    else { Start_AccoladesFound = false; }
                }

                if (Start_AccoladesFound && !End_AccoladesFound)
                {

                    if (line.StartsWith("tax_slot_manager={"))
                    {
                        Program.Logger.Debug("Found end of accolades block.");
                        Program.Logger.Debug($"Writing {Data.SB_Accolades.Length} characters to Accolades.txt");
                        //Write Accolades Data to txt file
                        using (StreamWriter sw = File.AppendText(@".\data\save_file_data\Accolades.txt"))
                        {
                            sw.Write(Data.SB_Accolades);
                            sw.Close();
                        }
                        Data.SB_Accolades = new StringBuilder();
                        GC.Collect();
                        End_AccoladesFound = true;
                        return;
                    }
                    else { HasAccoladesExtracted = false; }

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
    }
}
