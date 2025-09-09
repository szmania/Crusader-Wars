using System.Collections.Generic;
using System.IO;
using System.Text;
using CrusaderWars.data.save_file; // Assuming Army is in this namespace
using CrusaderWars.client; // For Program.Logger

namespace CrusaderWars
{
    public static class DeclarationsFile
    {
        static string filePath = Path.Combine("data", "battle files", "script", "tut_declarations.lua"); // Assuming this path

        // Store armies for BattleScript.SetCommandersLocals
        private static List<Army>? _leftSideArmies;
        private static List<Army>? _rightSideArmies;

        public static void Erase()
        {
            Program.Logger.Debug("Erasing and resetting declarations script.");
            File.WriteAllText(filePath, ""); // Clear the file
        }

        public static void CreateAlliances(List<Army> attackerArmies, List<Army> defenderArmies)
        {
            // This is a simplified version. The actual logic in BattleFile.cs has more overloads.
            // For now, just ensure the file is created and the armies are stored.
            Program.Logger.Debug("Creating alliances in declarations file (simplified).");
            _leftSideArmies = attackerArmies; // Assuming attacker is left for simplicity, will be refined if needed
            _rightSideArmies = defenderArmies; // Assuming defender is right for simplicity, will be refined if needed

            // Write initial content to the file
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("--");
            sb.AppendLine("--	DECLARATIONS SCRIPT");
            sb.AppendLine("--");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("");
            sb.AppendLine("local battle_manager = require \"lua_scripts.Battle_Script_Header\"");
            sb.AppendLine("local empire_battle = require \"lua_scripts.empire_battle\"");
            sb.AppendLine("local dev = require \"lua_scripts.dev\"");
            sb.AppendLine("");

            // Add alliance declarations
            sb.AppendLine("Alliance_Stark = battle_manager:new_alliance(\"stark\");");
            sb.AppendLine("Alliance_Bolton = battle_manager:new_alliance(\"bolton\");");
            sb.AppendLine("");

            // Add army declarations
            int armyCounter = 1;
            foreach (var army in attackerArmies)
            {
                sb.AppendLine($"Stark_Army{armyCounter} = Alliance_Stark:new_army(\"army_{army.ID}\");");
                armyCounter++;
            }
            armyCounter = 1;
            foreach (var army in defenderArmies)
            {
                sb.AppendLine($"Bolton_Army{armyCounter} = Alliance_Bolton:new_army(\"army_{army.ID}\");");
                armyCounter++;
            }
            sb.AppendLine("");

            File.AppendAllText(filePath, sb.ToString());
        }

        // Overload for CreateAlliances with player_main_army and enemy_main_army
        public static void CreateAlliances(List<Army> attackerArmies, List<Army> defenderArmies, Army playerMainArmy, Army enemyMainArmy)
        {
            Program.Logger.Debug("Creating alliances in declarations file (with main armies).");
            // Determine which side is player's and which is enemy's
            List<Army> playerSideArmies;
            List<Army> enemySideArmies;

            if (playerMainArmy.CombatSide == "attacker")
            {
                playerSideArmies = attackerArmies;
                enemySideArmies = defenderArmies;
            }
            else
            {
                playerSideArmies = defenderArmies;
                enemySideArmies = attackerArmies;
            }

            _leftSideArmies = playerSideArmies; // Assuming player is left
            _rightSideArmies = enemySideArmies; // Assuming enemy is right

            // Clear existing content
            Erase();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("--");
            sb.AppendLine("--	DECLARATIONS SCRIPT");
            sb.AppendLine("--");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("");
            sb.AppendLine("local battle_manager = require \"lua_scripts.Battle_Script_Header\"");
            sb.AppendLine("local empire_battle = require \"lua_scripts.empire_battle\"");
            sb.AppendLine("local dev = require \"lua_scripts.dev\"");
            sb.AppendLine("");

            // Add alliance declarations
            sb.AppendLine("Alliance_Stark = battle_manager:new_alliance(\"stark\");");
            sb.AppendLine("Alliance_Bolton = battle_manager:new_alliance(\"bolton\");");
            sb.AppendLine("");

            // Add army declarations for player side
            int playerArmyCounter = 1;
            foreach (var army in playerSideArmies)
            {
                sb.AppendLine($"Stark_Army{playerArmyCounter} = Alliance_Stark:new_army(\"army_{army.ID}\");");
                playerArmyCounter++;
            }
            sb.AppendLine("");

            // Add army declarations for enemy side
            int enemyArmyCounter = 1;
            foreach (var army in enemySideArmies)
            {
                sb.AppendLine($"Bolton_Army{enemyArmyCounter} = Alliance_Bolton:new_army(\"army_{army.ID}\");");
                enemyArmyCounter++;
            }
            sb.AppendLine("");

            File.AppendAllText(filePath, sb.ToString());
        }


        public static void AddUnitDeclaration(string declarationName, string scriptName)
        {
            Program.Logger.Debug($"Adding unit declaration: {declarationName} for script name {scriptName}");
            string declaration = $"{declarationName} = bm:get_unit_by_script_id(\"{scriptName}\");";
            File.AppendAllText(filePath, declaration + "\n");
        }

        public static List<Army>? GetLeftSideArmies()
        {
            return _leftSideArmies;
        }

        public static List<Army>? GetRightSideArmies()
        {
            return _rightSideArmies;
        }
    }
}
