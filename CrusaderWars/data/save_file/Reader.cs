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
using CrusaderWars; // Added to access the consolidated Data class
using CrusaderWars.data.battle_results;

namespace CrusaderWars.data.save_file
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

            //Clear Dynasties File
            File.WriteAllText(Writter.DataFilesPaths.Dynasties_Path(), "");

            //Clear Sieges File
            File.WriteAllText(Writter.DataFilesPaths.Sieges_Path(), "");
            //Clear Sieges TEMP File
            File.WriteAllText(Writter.DataTEMPFilesPaths.Sieges_Path(), "");
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
            // Data.Reset(); // Reset all static data containers and extraction flags -- REMOVED as per plan
            Program.Logger.Debug("Previous save data cleared.");
            long startMemoryt = GC.GetTotalMemory(false);

            using (FileStream saveFile = File.Open(savePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(saveFile))
            {
                string? line;
                int lineCount = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineCount++;
                    if (lineCount % 500000 == 0)
                    {
                        Program.Logger.Debug($"... Read {lineCount} lines from save file ...");
                    }
                    
                    if (twbattle.BattleState.IsSiegeBattle && !string.IsNullOrEmpty(BattleResult.ProvinceID))
                    {
                        GetterKeys.ReadProvinceBuildings(line, BattleResult.ProvinceID);
                    }


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
                    SearchKeys.Dynasties(line);
                    SearchKeys.Sieges(line); // Added call to new Sieges search method
                    
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
    // REMOVED redundant Data, GetterKeys, and SearchKeys definitions. They are now in Data.cs
}
