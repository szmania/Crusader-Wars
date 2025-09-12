using CrusaderWars.armies;
using CrusaderWars.terrain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.packfile
{
    public static class BattleFile
    {
        private static string BattleFilePath => @".\data\battle.xml";
        private static StringBuilder _battleXmlContent = new StringBuilder();

        public static void ClearFile()
        {
            Program.Logger.Debug("Clearing BattleFile content.");
            _battleXmlContent.Clear();
            if (File.Exists(BattleFilePath))
            {
                File.Delete(BattleFilePath);
            }
        }

        public static void SetArmiesSides(List<Army> attackerArmies, List<Army> defenderArmies)
        {
            Program.Logger.Debug("Setting armies sides for BattleFile.");
            int totalAttackerSoldiers = attackerArmies.Sum(a => a.GetTotalSoldiers());
            int totalDefenderSoldiers = defenderArmies.Sum(a => a.GetTotalSoldiers());

            // Determine deployment directions based on terrain and total soldiers
            TerrainGenerator.SetBattleMap();
            Deployments.beta_SetSidesDirections(totalAttackerSoldiers + totalDefenderSoldiers, TerrainGenerator.BattleMap, TerrainGenerator.ShouldRotateDeployment());

            // This method is called early in ArmiesReader, before units are fully processed.
            // The actual deployment XML will be generated later in BETA_CreateBattle.
        }

        public static void BETA_CreateBattle(List<Army> attackerArmies, List<Army> defenderArmies)
        {
            Program.Logger.Debug("BETA_CreateBattle: Generating battle XML content.");
            _battleXmlContent.Clear();

            _battleXmlContent.AppendLine("<battle>");
            _battleXmlContent.AppendLine("  <version>1</version>");
            _battleXmlContent.AppendLine("  <type>land</type>");
            _battleXmlContent.AppendLine("  <map_name>battle_map_1</map_name>"); // Placeholder, actual map name should come from TerrainGenerator

            // Attacker Deployment
            _battleXmlContent.AppendLine("  <attacker>");
            _battleXmlContent.AppendLine(Deployments.beta_GetDeployment("attacker"));
            _battleXmlContent.AppendLine("  </attacker>");

            // Defender Deployment
            _battleXmlContent.AppendLine("  <defender>");
            _battleXmlContent.AppendLine(Deployments.beta_GetDeployment("defender"));
            _battleXmlContent.AppendLine("  </defender>");

            // Units will be added by subsequent calls to AddUnit, AddGeneralUnit, AddKnightUnit
            // The file will be written to disk at the end of the process in PackFile.PackFileCreator
            Program.Logger.Debug("BETA_CreateBattle: Initial battle XML structure generated.");
        }

        public static void AddUnit(string attilaUnitKey, int unitSoldiers, int unitNum, int soldiersRest, string scriptName, string xp, string direction)
        {
            Program.Logger.Debug($"Adding unit to battle XML: {attilaUnitKey}, Soldiers: {unitSoldiers}, Num: {unitNum}, XP: {xp}");
            // This is a simplified representation. Actual implementation would involve more details.
            _battleXmlContent.AppendLine($"  <unit faction=\"{attilaUnitKey.Split('_')[0]}\" type=\"{attilaUnitKey}\" soldiers=\"{unitSoldiers}\" num_units=\"{unitNum}\" xp=\"{xp}\" script_name=\"{scriptName}\" direction=\"{direction}\"/>");
        }

        public static void AddGeneralUnit(CommanderSystem commander, string attilaUnitKey, string scriptName, int xp, string direction)
        {
            Program.Logger.Debug($"Adding general unit to battle XML: {commander.Name}, Type: {attilaUnitKey}, XP: {xp}");
            // This is a simplified representation. Actual implementation would involve more details.
            _battleXmlContent.AppendLine($"  <general_unit faction=\"{attilaUnitKey.Split('_')[0]}\" type=\"{attilaUnitKey}\" soldiers=\"{commander.GetUnitSoldiers()}\" num_units=\"1\" xp=\"{xp}\" script_name=\"{scriptName}\" direction=\"{direction}\"/>");
        }

        public static void AddKnightUnit(KnightSystem knights, string attilaUnitKey, string scriptName, int xp, string direction)
        {
            Program.Logger.Debug($"Adding knight unit to battle XML: Type: {attilaUnitKey}, Soldiers: {knights.GetKnightsSoldiers()}, XP: {xp}");
            // This is a simplified representation. Actual implementation would involve more details.
            _battleXmlContent.AppendLine($"  <knight_unit faction=\"{attilaUnitKey.Split('_')[0]}\" type=\"{attilaUnitKey}\" soldiers=\"{knights.GetKnightsSoldiers()}\" num_units=\"1\" xp=\"{xp}\" script_name=\"{scriptName}\" direction=\"{direction}\"/>");
        }

        public static string GetBattleXmlContent()
        {
            _battleXmlContent.AppendLine("</battle>");
            return _battleXmlContent.ToString();
        }

        // Placeholder for writing the final XML to a file, likely called by PackFile.PackFileCreator
        public static void WriteBattleFile()
        {
            Program.Logger.Debug($"Writing final battle XML to {BattleFilePath}");
            File.WriteAllText(BattleFilePath, GetBattleXmlContent());
        }
    }
}
