-- Crusader Conflicts: Siege Effects Script

-- This script reads siege-specific variables declared in tut_declarations.lua
-- and applies effects to the defending army based on their supply status.

-- Wait for the battle to start
bm:wait_for_battle_to_start()

-- Get the defending alliance. In Crusader Conflicts, the defender is always alliance 1.
local defending_alliance = bm:alliances():item(1)
if not defending_alliance then
    script_error("Siege Script Error: Could not find defending alliance (index 1).")
    return
end

-- Log the supply level for debugging
bm:out("Siege Script: Defender Supply Level is " .. tostring(DEFENDER_SUPPLY_LEVEL))

-- Apply effects based on supply level
if DEFENDER_SUPPLY_LEVEL == "Running Low" then
    bm:out("Siege Script: Applying 'Running Low' penalties to defenders.")
    for i = 0, defending_alliance:armies():count() - 1 do
        local army = defending_alliance:armies():item(i)
        for j = 0, army:units():count() - 1 do
            local unit = army:units():item(j)
            -- Apply a moderate fatigue penalty
            unit:apply_fatigue(2) -- Corresponds to "Tired"
        end
    end
elseif DEFENDER_SUPPLY_LEVEL == "Starvation" then
    bm:out("Siege Script: Applying 'Starvation' penalties to defenders.")
    for i = 0, defending_alliance:armies():count() - 1 do
        local army = defending_alliance:armies():item(i)
        for j = 0, army:units():count() - 1 do
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
