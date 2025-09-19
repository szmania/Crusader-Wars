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
require "lua_scripts.Battle_Script_Header";

-- declare battlemanager object
bm = battle_manager:new(empire_battle:new());

-- get battle name from folder, and print header
battle_name, battle_shortform = get_folder_name_and_shortform();

-- load in other script files associated with this battle
package.path = package.path .. ";data/Script/" .. battle_name .. "/?.lua";

require (battle_shortform .. "_Declarations");

----------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------
--
--	HISTORICAL BATTLE CUTSCENE AND UNIT POSITION SCRIPT
--
----------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

dev = require("lua_scripts.dev");

require("lua_scripts.logging_callbacks");

local date = os.date("\A, %c");

dev.log("\n" .. date);
dev.log"\n Script Loaded";


bm:setup_victory_callback(function() file_debug() end);
bm:register_phase_change_callback("Complete", function() file_debug() end);	

 
function Deployment_Phase()
	bm:out("Battle is in deployment phase");
end;

function Start_Battle()
	bm:out("Battle is Starting");
	
    -- Crusader Conflicts: Siege Effects Script
    -- This script reads siege-specific variables declared in tut_declarations.lua
    -- and applies effects to the defending army based on their supply status.

    -- Check if the siege variable exists. If not, this is not a siege battle, so do nothing.
    if DEFENDER_SUPPLY_LEVEL == nil then
        bm:out("Siege Script: DEFENDER_SUPPLY_LEVEL not found. Assuming field battle. No siege effects applied.")
        return
    end

    -- The defending alliance is Alliance_Bolton (the enemy side)
    local defending_alliance = Alliance_Bolton
    if not defending_alliance then
        script_error("Siege Script Error: Could not find defending alliance (Alliance_Bolton).")
        return
    end

    -- Log the supply level for debugging
    bm:out("Siege Script: Defender Supply Level is " .. tostring(DEFENDER_SUPPLY_LEVEL))

    -- Apply effects based on supply level
    if DEFENDER_SUPPLY_LEVEL == "Running Low" then
        bm:out("Siege Script: Applying 'Running Low' penalties to defenders.")
        for i = 1, defending_alliance:armies():count() do
            local army = defending_alliance:armies():item(i)
            for j = 1, army:units():count() do
                local unit = army:units():item(j)
                -- Apply a moderate fatigue penalty
                unit:apply_fatigue(2) -- Corresponds to "Tired"
            end
        end
    elseif DEFENDER_SUPPLY_LEVEL == "Starvation" then
        bm:out("Siege Script: Applying 'Starvation' penalties to defenders.")
        for i = 1, defending_alliance:armies():count() do
            local army = defending_alliance:armies():item(i)
            for j = 1, army:units():count() do
                local unit = army:units():item(j)
                -- Apply a severe fatigue penalty
                unit:apply_fatigue(3) -- Corresponds to "Very Tired"
                -- Apply a morale penalty (e.g., -8 morale)
                unit:set_morale_impetus(-8)
            end
        end
    else -- "Fully Stocked" or any other value
        bm:out("Siege Script: Defenders are fully stocked. No penalties applied.")
    end
end;

local scripting = require "lua_scripts.episodicscripting"
-- Callbacks
function EndBattle(context)
   
    if context.string == "button_end_battle" then
        dev.log("Battle has finished")
    end;
    
    if context.string == "button_dismiss_results" then
        dev.log("Battle has finished")
    end;

end;
scripting.AddEventCallBack("ComponentLClickUp", EndBattle);



--Crusader Conflicts Get Winner
function file_debug()

	bm:callback(function() bm:end_battle() end, 1000);

	if is_routing_or_dead(Alliance_Stark) then	
		bm:out("Player has lost, army is routing");
        dev.log("Defeat")
	elseif is_routing_or_dead(Alliance_Bolton) then
		bm:out("Player has won !");
		dev.log("Victory")
	end;

    
	remaining_soldiers();
	kills();
	commander_system();
	dev.log("-----PRINT ENDED-----!!")

end;
