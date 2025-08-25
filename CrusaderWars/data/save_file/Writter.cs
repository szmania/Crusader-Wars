using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.data.save_file
{
    public static class Writter
    {
        static bool NeedSkiping { get; set; }
        static bool CombatResults_NeedsSkiping { get; set; }
        static bool Combats_NeedsSkiping { get; set; }
        public static void SendDataToFile(string savePath)
        {
            Program.Logger.Debug($"Starting to write data back to save file: {savePath}");
            bool resultsFound = false;
            bool combatsFound = false;

            //string tempFilePath = Directory.GetCurrentDirectory() + "\\CrusaderWars_Battle.ck3";
            string tempFilePath = @".\data\save_file_data\gamestate";
            Program.Logger.Debug($"Writing to temporary file: {Path.GetFullPath(tempFilePath)}");

            using (var inputFileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var streamReader = new StreamReader(inputFileStream))
            using (StreamWriter streamWriter = new StreamWriter(outputFileStream))
            {
                streamWriter.NewLine = "\n";
                string line;
                while ((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {

                    //Line Skipper
                    if (NeedSkiping && line == "pending_character_interactions={")
                    {
                        Program.Logger.Debug("Skipping block until 'pending_character_interactions={' is found.");
                        NeedSkiping = false;
                    }
                    else if (CombatResults_NeedsSkiping && line == "\t\t}")
                    {
                        Program.Logger.Debug("Finished skipping CombatResults block.");
                        CombatResults_NeedsSkiping = false;
                        resultsFound = false;
                    }
                    else if (Combats_NeedsSkiping && line == "\t\t}")
                    {
                        Program.Logger.Debug("Finished skipping Combats block.");
                        Combats_NeedsSkiping = false;
                        combatsFound = false;
                    }
                    else if (NeedSkiping && line == "\tarmy_regiments={")
                    {
                        Program.Logger.Debug("Skipping block until '\tarmy_regiments={' is found.");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "\tarmies={")
                    {
                        Program.Logger.Debug("Skipping block until '\tarmies={' is found.");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "dead_unprunable={")
                    {
                        Program.Logger.Debug("Skipping block until 'dead_unprunable={' is found.");
                        NeedSkiping = false;
                    }

                    //Achievements
                    if(line == "\tcan_get_achievements=no")
                    {
                        streamWriter.WriteLine("\tcan_get_achievements=yes");
                        continue;
                    }

                    //Combat Result START
                    else if (line == "\tcombat_results={")
                    {
                        resultsFound = true;
                        streamWriter.WriteLine(line);
                        continue;
                    }

                    //Combat Result
                    else if (line == $"\t\t{BattleResult.ResultID}={{" && resultsFound && !CombatResults_NeedsSkiping)
                    {
                        Program.Logger.Debug("Writing modified CombatResults block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.CombatResults_Path(), FileType.CombatResults);
                        Program.Logger.Debug("EDITED BATTLE RESULTS SENT!");
                        CombatResults_NeedsSkiping = true;
                    }
                    
                    //Combat START
                    else if (line == "\tcombats={")
                    {
                        combatsFound = true;
                        streamWriter.WriteLine(line);
                    }
                    //Combat
                    else if (line == $"\t\t{BattleResult.CombatID}={{" && combatsFound && !Combats_NeedsSkiping)
                    {
                        Program.Logger.Debug("Writing modified Combats block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Combats_Path(), FileType.Combats);
                        Program.Logger.Debug("EDITED COMBATS SENT!");
                        Combats_NeedsSkiping = true;
                    }
                    else if (line == "\tregiments={" && !NeedSkiping)
                    {
                        Program.Logger.Debug("Writing modified Regiments block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Regiments_Path(), FileType.Regiments);
                        Program.Logger.Debug("EDITED REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "\tarmy_regiments={" && !NeedSkiping)
                    {
                        Program.Logger.Debug("Writing modified ArmyRegiments block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.ArmyRegiments_Path(), FileType.ArmyRegiments);
                        Program.Logger.Debug("EDITED ARMY REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "living={" && !NeedSkiping)
                    {
                        Program.Logger.Debug("Writing modified Living block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Living_Path(), FileType.Living);
                        Program.Logger.Debug("EDITED LIVING SENT!");
                        NeedSkiping = true;
                    }
                    else if (!NeedSkiping && !CombatResults_NeedsSkiping && !Combats_NeedsSkiping)
                    {
                        streamWriter.WriteLine(line);
                    }

                }

                streamWriter.Close();
                streamReader.Close();
                outputFileStream.Close();
                inputFileStream.Close();
            }
            Program.Logger.Debug($"Finished writing to temporary file: {Path.GetFullPath(tempFilePath)}");

            //string save_games_path = Properties.Settings.Default.VAR_dir_save;
            //string editedSavePath = save_games_path + @"\CrusaderWars_Battle.ck3";
            //File.Delete(savePath);
            //File.Move(tempFilePath, editedSavePath);
        }


        static void WriteDataToSaveFile(StreamWriter streamWriter, string data_file_path, FileType fileType)
        {
            Program.Logger.Debug($"Writing content from {data_file_path} into save file stream.");
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(data_file_path))
            {
                while (true)
                {
                    string l = sr.ReadLine();
                    if (l is null) break;
                    switch(fileType)
                    {
                        case FileType.Combats:
                        case FileType.CombatResults:
                            if (l == "\t\t}") continue;
                            break;
                    }
                    
                    sb.AppendLine(l);
                }
            }

            Program.Logger.Debug($"Read {sb.Length} characters from {data_file_path} to be written.");
            streamWriter.WriteLine(sb.ToString());
        }

        enum FileType
        {
            Living,
            CombatResults,
            Combats,
            ArmyRegiments,
            Regiments
        }

        public struct DataFilesPaths
        {
            public static string CombatResults_Path() { return @".\data\save_file_data\BattleResults.txt"; }
            public static string Combats_Path() { return @".\data\save_file_data\Combats.txt"; }
            public static string Regiments_Path() { return @".\data\save_file_data\Regiments.txt"; }
            public static string ArmyRegiments_Path() { return @".\data\save_file_data\ArmyRegiments.txt"; }
            public static string Living_Path() { return @".\data\save_file_data\Living.txt"; }
            public static string Cultures_Path() { return @".\data\save_file_data\Cultures.txt"; }
            public static string Mercenaries_Path() { return @".\data\save_file_data\Mercenaries.txt"; }
            public static string Armies_Path() { return @".\data\save_file_data\Armies.txt"; }
            public static string Counties_Path() { return @".\data\save_file_data\Counties.txt"; }
            public static string Traits_Path() { return @".\data\save_file_data\Traits.txt"; }
            public static string Units_Path() { return @".\data\save_file_data\Units.txt"; }
            public static string CourtPositions_Path() { return @".\data\save_file_data\CourtPositions.txt"; }
            public static string LandedTitles() { return @".\data\save_file_data\LandedTitles.txt"; }
            public static string Accolades() { return @".\data\save_file_data\Accolades.txt"; }

        }

        public struct DataTEMPFilesPaths
        {
            public static string CombatResults_Path() { return @".\data\save_file_data\temp\BattleResults.txt"; }
            public static string Combats_Path() { return @".\data\save_file_data\temp\Combats.txt"; }
            public static string Regiments_Path() { return @".\data\save_file_data\temp\Regiments.txt"; }
            public static string ArmyRegiments_Path() { return @".\data\save_file_data\temp\ArmyRegiments.txt"; }
            public static string Living_Path() { return @".\data\save_file_data\temp\Living.txt"; }

        }

    }
}
