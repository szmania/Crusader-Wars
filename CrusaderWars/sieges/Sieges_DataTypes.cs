using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace CrusaderWars.sieges
{
    public static class Sieges_DataTypes
    {
        public static void GetEscalation() { }
        public static void GetSuppliesDisavantadges() { }
        public static void GetSicknessDisavantadges() { }

        //Maps variations depend on the buildings
        //Civic buildings get one variation
        //Military buildings get one variation
        //Mixed buildings get one variation
        struct MapsVariations
        {
            //terrain/tiles/battle/assembly_kit/battle_of_the_bastards/battlefield
            //terrain/tiles/battle/settlement_eastern_cities/eastern_city_a/medium/
            struct DefaultAttilaCities
            {
                //Western
                struct Western
                {
                   public string GetCivicVariation() { return "terrain/tiles/battle/settlement_western_cities/western_city_a/medium/"; }
                };
            };
        };

        public struct Escalation
        {
            public static string GetEscalationTileUpgrade(string wall_breach)
            {
                switch(wall_breach) 
                {
                    case "Intact":
                        return "<tile_upgrade>escalation1</tile_upgrade>";
                    case "SmallBreach":
                        return "<tile_upgrade>escalation2</tile_upgrade>";
                    case "LargeBreach":
                        return "<tile_upgrade>escalation3</tile_upgrade>";
                    default:
                        return "";
                }
            }
        };

        public struct Fortification
        {
            public static string GetFortificationDamageTags(string breach_status)
            {
                string wallDamage = "0.0";
                string wallBreaches = "0";
                string buildingDamage = "0.25"; // Default as per plan

                switch (breach_status)
                {
                    case "Intact":
                        wallDamage = "0.0";
                        wallBreaches = "0";
                        break;
                    case "SmallBreach":
                        wallDamage = "0.3";
                        wallBreaches = "2";
                        break;
                    case "LargeBreach":
                        wallDamage = "0.6";
                        wallBreaches = "4";
                        break;
                }

                return $"<fortification_wall_damage>{wallDamage}</fortification_wall_damage>\n" +
                       $"<fortification_building_damage>{buildingDamage}</fortification_building_damage>\n" +
                       $"<fortification_wall_breaches>{wallBreaches}</fortification_wall_breaches>\n";
            }
        }

        public struct Holding
        {
            public static string GetArchitecture(string architecture)
            {
                switch(architecture)
                {
                    case "African": return "muslim";
                    case "Arabic": return "muslim";
                    case "Berber": return "muslim";
                    case "Indian": return "muslim";
                    case "Mediterranean": return "eastern_roman";
                    case "Norse": return "germanic";
                    case "Eurasian Steppe": return "germanic";
                    case "Continental European": return "western_roman";
                    case "Iberian": return "western_roman";
                    default: return "germanic";
                }
            }

            public static string GetVariation(string[] building_keys) 
            {
                //not adding longhouses_01:1 "Gathering Halls"
                //because they will counted as mixed variation

                string[] CivicBuildings = 
                    { "tradeport_",
                      "university_",
                      "pastures_",
                      "orchards_",
                      "farm_estates_",
                      "logging_camps_",
                      "peat_quarries_",
                      "quarries_",
                      "hill_farms_",
                      "plantations_",
                      "quarries_",
                      "hillside_grazing_",
                      "windmills_",
                      "watermills_",
                      "caravanserai_",
                      "cereal_fields_" };


                string[] MilitaryBuildings =
                    { "hall_of_heroes_",
                      "regimental_grounds_",
                      "barracks_",
                      "camel_farms_",
                      "outposts_",
                      "military_camps_",
                      "elephant_pens_",
                      "stables_",
                      "horse_pastures_",
                      "smiths_",
                      "warrior_lodges_",
                      "hunting_grounds_",
                      "workshops_" };

                int civic_points = 0;
                int military_points = 0;

                for (int i = 0; i < building_keys.Length; i++) 
                {
                    if (building_keys[i] == CivicBuildings.FirstOrDefault(x => x.Contains(building_keys[i]))) 
                    {
                        civic_points++;
                    }
                    else if (building_keys[i] == MilitaryBuildings.FirstOrDefault(x => x.Contains(building_keys[i])))
                    {
                        military_points++;
                    }
                }

                //Military Variation
                if(civic_points == 0 && military_points > 0)
                {
                    return "military";
                }
                //Civic Variation
                else if(military_points == 0 && civic_points > 0)
                {
                    return "civic";
                }
                //Default Variation
                else
                {
                    return "default";
                }
            }
            public static int GetLevel(string holding_key, string[] buildings_key, int fortLevel)
            {
                Program.Logger.Debug($"Calculating holding level with key: '{holding_key}', fort level: {fortLevel}, and {buildings_key.Length} buildings.");
                //If there is a holding building, increase wall size.
                int building_wall_boost = 0;
                if (buildings_key.Length > 0)
                {
                    foreach (var building in buildings_key)
                    {
                        if (building.Contains("watchtowers_") ||
                            building.Contains("curtain_walls_") ||
                            building.Contains("hill_forts_") ||
                            building.Contains("forest_forts_") ||
                            building.Contains("palisades_"))
                        {
                            building_wall_boost = 1;
                            Program.Logger.Debug($"  - Found defensive building '{building}'. Applying wall boost of {building_wall_boost}.");
                            break; // Found one, no need to check more
                        }
                    }
                }
                if (building_wall_boost == 0)
                {
                    Program.Logger.Debug("  - No defensive buildings found. Wall boost is 0.");
                }

                int baseLevel;
                switch (holding_key)
                {
                    //Feudal
                    case "castle_01":
                        baseLevel = 1 + building_wall_boost; break;
                    case "castle_02":
                        baseLevel = 3; break;
                    case "castle_03":
                        baseLevel = 4 + building_wall_boost; break;
                    case "castle_04":
                        baseLevel = 6; break;

                    //Tribal
                    case "tribe_01":
                        baseLevel = 1 + building_wall_boost; break;
                    case "tribe_02":
                        baseLevel = 3; break;
                    default:
                        baseLevel = 0; break;
                }
                Program.Logger.Debug($"  - Base level from holding key '{holding_key}' is {baseLevel} (includes wall boost).");

                // Only add fort level if a valid holding was found
                int finalLevel = baseLevel > 0 ? baseLevel + fortLevel : 0;
                Program.Logger.Debug($"  - Final holding level: {finalLevel} (base level + fort level {fortLevel}).");
                return finalLevel;
            }
        };
    }
}
