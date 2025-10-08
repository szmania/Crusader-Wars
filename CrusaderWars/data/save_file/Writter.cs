using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.data.battle_results; // Added for BattleResult class

namespace CrusaderWars.data.save_file
{
    public static class Writter
    {
        static bool NeedSkiping { get; set; }
        static bool CombatResults_NeedsSkiping { get; set; }
        static bool Combats_NeedsSkiping { get; set; }
        static bool Sieges_NeedsSkiping { get; set; }
        public static void SendDataToFile(string savePath)
        {
            // Reset static state variables
            NeedSkiping = false;
            CombatResults_NeedsSkiping = false;
            Combats_NeedsSkiping = false;
            Sieges_NeedsSkiping = false;

            Program.Logger.Debug($"Starting to write data back to save file: {Path.GetFullPath(savePath)}");
            // Removed resultsFound and combatsFound boolean variables.

            //string tempFilePath = Directory.GetCurrentDirectory() + "\\CrusaderWars_Battle.ck3";
            string tempFilePath = @".\data\save_file_data\gamestate";
            Program.Logger.Debug($"Writing to temporary file: {Path.GetFullPath(tempFilePath)}");

            using (var inputFileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var streamReader = new StreamReader(inputFileStream))
            using (StreamWriter streamWriter = new StreamWriter(outputFileStream))
            {
                streamWriter.NewLine = "\n";
                string? line;
                int lineCount = 0;
                while ((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {
                    lineCount++;
                    if (lineCount % 500000 == 0)
                    {
                        Program.Logger.Debug($"... Processed {lineCount} lines for writing ...");
                    }

                    //Line Skipper
                    if (NeedSkiping && line == "pending_character_interactions={")
                    {
                        Program.Logger.Debug("Skipping block until 'pending_character_interactions={' is found.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (CombatResults_NeedsSkiping && line == "}") // Changed from "\t\t}" to "}"
                    {
                        Program.Logger.Debug("Finished skipping CombatResults block.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        CombatResults_NeedsSkiping = false;
                    }
                    else if (Combats_NeedsSkiping && line == "}") // Changed from "\t\t}" to "}"
                    {
                        Program.Logger.Debug("Finished skipping Combats block.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        Combats_NeedsSkiping = false;
                    }
                    // Corrected condition to match the top-level closing brace for the entire sieges block
                    else if (Sieges_NeedsSkiping && line == "}")
                    {
                        Program.Logger.Debug("Finished skipping Sieges block.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        Sieges_NeedsSkiping = false;
                        // siegesFound = false; // No longer needed
                    }
                    else if (NeedSkiping && line == "\tarmy_regiments={")
                    {
                        Program.Logger.Debug("Skipping block until '\tarmy_regiments={' is found.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "\tarmies={")
                    {
                        Program.Logger.Debug("Skipping block until '\tarmies={' is found.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "dead_unprunable={")
                    {
                        Program.Logger.Debug("Skipping block until 'dead_unprunable={' is found.");
                        Program.Logger.Debug($"Stopped skipping at line: {line}");
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
                        Program.Logger.Debug("Writing modified CombatResults block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.CombatResults_Path(), FileType.CombatResults);
                        Program.Logger.Debug("EDITED BATTLE RESULTS SENT!");
                        CombatResults_NeedsSkiping = true;
                    }
                    
                    //Combat START
                    else if (line == "\tcombats={")
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
                    // NEW BLOCK: Replace the entire sieges block when "sieges={" is encountered
                    else if (line == "sieges={" && !Sieges_NeedsSkiping)
                    {
                        string tempSiegesPath = DataTEMPFilesPaths.Sieges_Path();
                        if (File.Exists(tempSiegesPath))
                        {
                            Program.Logger.Debug("Writing modified Sieges block from temporary file.");
                            WriteDataToSaveFile(streamWriter, tempSiegesPath, FileType.Sieges);
                            Program.Logger.Debug("EDITED SIEGES SENT!");
                            Sieges_NeedsSkiping = true;
                        }
                        else
                        {
                            // If the temporary Sieges.txt doesn't exist, it means no siege battle occurred
                            // or the data wasn't processed. Preserve the original block.
                            Program.Logger.Debug($"Temporary Sieges file not found at {Path.GetFullPath(tempSiegesPath)}. Preserving original sieges block.");
                            streamWriter.WriteLine(line); // Write the "sieges={" line
                            // DO NOT set Sieges_NeedsSkiping = true, so the original content is copied.
                        }
                    }
                    else if (!NeedSkiping && !CombatResults_NeedsSkiping && !Combats_NeedsSkiping && !Sieges_NeedsSkiping)
                    {
                        streamWriter.WriteLine(line);
                    }

                }
                Program.Logger.Debug("Finished writing to temporary file. All blocks processed.");
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
            Program.Logger.Debug($"Reading content from {data_file_path} to write into save file stream.");
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(data_file_path))
            {
                while (true)
                {
                    string? l = sr.ReadLine();
                    if (l is null) break;
                    // Removed the switch statement as it's no longer needed.
                    // The closing brace for CombatResults and Combats is now handled by the extraction logic
                    // and should be included in the temporary file.
                    sb.AppendLine(l);
                }
            }

            Program.Logger.Debug($"Writing {sb.Length} characters of {fileType} data to save file stream.");
            streamWriter.WriteLine(sb.ToString());
        }

        enum FileType
        {
            Living,
            CombatResults,
            Combats,
            ArmyRegiments,
            Regiments,
            Sieges
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
            public static string Sieges_Path() { return @".\data\save_file_data\Sieges.txt"; }

        }

        public struct DataTEMPFilesPaths
        {
            public static string CombatResults_Path() { return @".\data\save_file_data\temp\BattleResults.txt"; }
            public static string Combats_Path() { return @".\data\save_file_data\temp\Combats.txt"; }
            public static string Regiments_Path() { return @".\data\save_file_data\temp\Regiments.txt"; }
            public static string ArmyRegiments_Path() { return @".\data\save_file_data\temp\ArmyRegiments.txt"; }
            public static string Living_Path() { return @".\data\save_file_data\temp\Living.txt"; }
            public static string Sieges_Path() { return @".\data\save_file_data\temp\Sieges.txt"; }

        }

    }
}
