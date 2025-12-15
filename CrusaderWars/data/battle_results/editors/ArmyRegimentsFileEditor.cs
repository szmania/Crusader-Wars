using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CrusaderWars.data.save_file;
using static CrusaderWars.data.save_file.Writter;
using CrusaderWars.armies; // Added for List<Army>

namespace CrusaderWars.data.battle_results.editors
{
    public static class ArmyRegimentsFileEditor
    {
        public static void EditArmyRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Army Regiments file...");
            bool editStarted = false;
            ArmyRegiment? editArmyRegiment = null;

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.ArmyRegiments_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.ArmyRegiments_Path()))
            {
                streamWriter.NewLine = "\n";

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {

                    //Regiment ID line
                    if (!editStarted && line != null && Regex.IsMatch(line, @"\t\t\d+={"))
                    {
                        string army_regiment_id = Regex.Match(line, @"\d+").Value;


                        var searchingData = SearchArmyRegimentsFile(attacker_armies, army_regiment_id);
                        if (searchingData.editStarted)
                        {
                            editStarted = true;
                            editArmyRegiment = searchingData.editArmyRegiment;
                            Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchArmyRegimentsFile(defender_armies, army_regiment_id);
                            if (searchingData.editStarted)
                            {
                                editStarted = true;
                                editArmyRegiment = searchingData.editArmyRegiment;
                                Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id} for editing (Defender).");
                            }
                        }

                    }

                    else if (editStarted == true && line.Contains("\t\t\t\tcurrent=") && editArmyRegiment != null)
                    {
                        string edited_line = "\t\t\t\tcurrent=" + editArmyRegiment.CurrentNum;
                        streamWriter.WriteLine(edited_line);
                        Program.Logger.Debug(
                            $"Updated ArmyRegiment {editArmyRegiment.ID} current soldiers to {editArmyRegiment.CurrentNum}.");
                        continue;
                    }

                    //End Line
                    else if (editStarted && line == "\t\t}")
                    {
                        editStarted = false;
                        editArmyRegiment = null;
                    }

                    streamWriter.WriteLine(line);
                }
            }

            Program.Logger.Debug("Finished editing Army Regiments file.");
        }

        static (bool editStarted, ArmyRegiment? editArmyRegiment) SearchArmyRegimentsFile(List<Army> armies,
            string army_regiment_id)
        {
            // Program.Logger.Debug($"Searching for ArmyRegiment ID: {army_regiment_id} in ArmyRegiments file.");
            bool editStarted = false;
            ArmyRegiment? editRegiment = null;

            foreach (Army army in armies)
            {
                if (army == null) continue;
                if (army.ArmyRegiments != null)
                {
                    foreach (ArmyRegiment army_regiment in army.ArmyRegiments)
                    {
                        if (army_regiment == null) continue;

                        if (army_regiment.Type == RegimentType.Knight) continue;
                        if (army_regiment.ID == army_regiment_id)
                        {
                            editStarted = true;
                            editRegiment = army_regiment;
                            Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id}.");
                            return (editStarted, editRegiment);
                        }
                    }
                }
            }

            // Program.Logger.Debug($"ArmyRegiment ID: {army_regiment_id} not found in ArmyRegiments file.");
            return (false, null);
        }
    }
}
