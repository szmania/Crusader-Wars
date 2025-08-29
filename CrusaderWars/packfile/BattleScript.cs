using System.Collections.Generic;
using System.IO;
using System.Text;


namespace CrusaderWars
{
    public static class BattleScript
    {
        static string filePath = Directory.GetFiles("data\\battle files\\script", "tut_start.lua", SearchOption.AllDirectories)[0];



        public static void CreateScript()
        {
            Program.Logger.Debug("Creating battle script...");
            string script_base = @"

function remaining_soldiers()
    dev.log(""-----REMAINING SOLDIERS-----!!"")";
            File.AppendAllText(filePath, script_base);
        }

        public static void SetCommandersLocals()
        {
            var left_side = DeclarationsFile.GetLeftSideArmies();
            var right_side = DeclarationsFile.GetRightSideArmies();
            
            // Add null checks for the lists to prevent CS8602 warnings
            if (left_side == null) left_side = new List<Army>();
            if (right_side == null) right_side = new List<Army>();

            Program.Logger.Debug($"Setting commander locals for {left_side.Count} left side armies and {right_side.Count} right side armies.");

            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("function commander_system()");


            for (int i = 1; i <= left_side.Count; i++) {
                if (left_side[i-1].Commander != null)
                {
                    Program.Logger.Debug($"Adding commander death check for left side army {left_side[i - 1].ID}, commander {left_side[i - 1].Commander.ID}");
                    scriptBuilder.AppendLine($"	if(not Stark_Army{i}:is_commander_alive()) then");
                    scriptBuilder.AppendLine($"		dev.log(\"Commander{left_side[i - 1].Commander.ID} from Army{left_side[i - 1].ID} has fallen\")");
                    scriptBuilder.AppendLine("	end;");
                    scriptBuilder.AppendLine();
                }

            }
            for (int i = 1; i <= right_side.Count; i++)
            {
                if (right_side[i-1].Commander != null)
                {
                    Program.Logger.Debug($"Adding commander death check for right side army {right_side[i - 1].ID}, commander {right_side[i - 1].Commander.ID}");
                    scriptBuilder.AppendLine($"	if(not Bolton_Army{i}:is_commander_alive()) then");
                    scriptBuilder.AppendLine($"		dev.log(\"Commander{right_side[i - 1].Commander.ID} from Army{right_side[i - 1].ID} has fallen\")");
                    scriptBuilder.AppendLine("	end;");
                    scriptBuilder.AppendLine();
                }
            }

            scriptBuilder.Append("end;");

            File.AppendAllText(filePath, scriptBuilder.ToString());

        }

        public static void SetLocals(string unitName, string declarationName)
        {
            string local = $"\n\tdev.log(\"{unitName}-\".. {declarationName}.unit:number_of_men_alive())";
            File.AppendAllText(filePath, local);
        }

        public static void SetLocalsKills(List<(string unitName, string declarationName)> units_scripts_list)
        {
            // Add null check for the list parameter to prevent CS8602 warning
            if (units_scripts_list == null)
            {
                Program.Logger.Debug("Warning: units_scripts_list is null. Skipping kill tracking locals.");
                return; // Exit the method if the list is null
            }

            Program.Logger.Debug($"Setting kill tracking locals for {units_scripts_list.Count} units.");
            //Units Script Start
            string start = @"

function kills()
    dev.log(""-----NUMBERS OF KILLS-----!!"")";
            File.AppendAllText(filePath, start);

            //Units Locals Kills
            foreach (var unit in units_scripts_list)
            {
                string locals = $"\n\tdev.log(\"kills_{unit.unitName}-\".. {unit.declarationName}.unit:number_of_enemies_killed())";
                File.AppendAllText(filePath, locals);
            }

        }


        public static void CloseScript()
        {
            Program.Logger.Debug("Closing script function block.");
            string close = "\nend;";
            File.AppendAllText(filePath, close);
        }

        //Add
        public static void EraseScript()
        {
            Program.Logger.Debug("Erasing and resetting battle script to default.");
            string original = @"
-----------------------------------------------------------------------------------
-----------------------------------------------------------------------------------
--
--	INITIAL SCRIPT SETUP
--
-----------------------------------------------------------------------------------
-----------------------------------------------------------------------------------

-- clear out loaded files
system.ClearRequiredFiles();

local logging_enabled = true;

-- load in battle script library
require ""lua_scripts.Battle_Script_Header"";

-- declare battlemanager object
bm = battle_manager:new(empire_battle:new());

-- get battle name from folder, and print header
battle_name, battle_shortform = get_folder_name_and_shortform();

-- load in other script files associated with this battle
package.path = package.path .. "";data/Script/"" .. battle_name .. ""/?.lua"";

require (battle_shortform .. ""_Declarations"");

----------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------
--
--	HISTORICAL BATTLE CUTSCENE AND UNIT POSITION SCRIPT
--
----------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

dev = require(""lua_scripts.dev"");

require(""lua_scripts.logging_callbacks"");

local date = os.date(""\A, %c"");

dev.log(""\n"" .. date);
dev.log""\n Script Loaded"";


bm:setup_victory_callback(function() file_debug() end);
bm:register_phase_change_callback(""Complete"", function() file_debug() end);	

 
function Deployment_Phase()
	bm:out(""Battle is in deployment phase"");
end;

function Start_Battle()
	bm:out(""Battle is Starting"");
	
end;

local scripting = require ""lua_scripts.episodicscripting""
-- Callbacks
function EndBattle(context)
   
    if context.string == ""button_end_battle"" then
        dev.log(""Battle has finished"")
    end;
    
    if context.string == ""button_dismiss_results"" then
        dev.log(""Battle has finished"")
    end;

end;
scripting.AddEventCallBack(""ComponentLClickUp"", EndBattle);



--Crusader Wars Get Winner
function file_debug()

	bm:callback(function() bm:end_battle() end, 1000);

	if is_routing_or_dead(Alliance_Stark) then	
		bm:out(""Player has lost, army is routing"");
        dev.log(""Defeat"")
	elseif is_routing_or_dead(Alliance_Bolton) then
		bm:out(""Player has won !"");
		dev.log(""Victory"")
	end;

    
	remaining_soldiers();
	kills();
	commander_system();
	dev.log(""-----PRINT ENDED-----!!"")

end;";
            File.WriteAllText(filePath, original);
        }
    }
}
