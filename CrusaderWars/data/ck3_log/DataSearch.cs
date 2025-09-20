using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CrusaderWars.armies;
using CrusaderWars.client;
using CrusaderWars.locs;
using CrusaderWars.terrain;


namespace CrusaderWars
{
    internal enum DataSearchSides
    {
        LeftSide,
        RightSide
    }

    public class PlayerChar
    {
        string ID { get; set; }
        string CultureID { get; set; }

        public PlayerChar(string iD,string culture_id)
        {
            ID = iD;;
            CultureID = culture_id;
        }

        public string GetID() { return ID; }
        public string GetCultureID() { return CultureID; }
    }


    public static class CK3LogData
    {
        // MAIN PARTICIPANTS
        static (string id, string culture_id) LeftSide_MainParticipant { get; set; }
        static (string id, string culture_id) RightSide_MainParticipant { get; set; }

        // COMMANDERS
        static (string name, string id, int prowess, int martial, int rank, string culture_id) LeftSide_Commander { get; set; }
        static (string name, string id, int prowess, int martial, int rank, string culture_id) RightSide_Commander { get; set; }

        // MAIN REALM NAMES
        static string LeftSide_RealmName { get; set; } = string.Empty;
        static string RightSide_RealmName { get; set; } = string.Empty;

        // MODIFIERS
        static Modifiers? LeftSide_Modifiers { get; set; }
        static Modifiers? RightSide_Modifiers { get; set; }

        // KNIGHTS
        static List<(string id, string prowess, string name, int effectiveness)> LeftSide_Knights { get; set; } = new();
        static List<(string id, string prowess, string name, int effectiveness)> RightSide_Knights { get; set; } = new();




        public struct LeftSide
        {
            internal static void SetMainParticipant((string id, string culture_id) data) { LeftSide_MainParticipant = data; }
            internal static void SetCommander((string name, string id, int prowess, int martial, int rank, string culture_id) data) { LeftSide_Commander = data; }
            internal static void  SetRealmName(string name) { LeftSide_RealmName = name; }
            internal static void SetModifiers(Modifiers t) { LeftSide_Modifiers = t; }
            internal static void SetKnights(List<(string id, string prowess, string name, int effectiveness)> t) { LeftSide_Knights = t; }

            public static (string id, string culture_id) GetMainParticipant() { return LeftSide_MainParticipant; }
            public static (string name, string id, int prowess, int martial, int rank, string culture_id) GetCommander() { return LeftSide_Commander; }
            public static string GetRealmName() { return LeftSide_RealmName; }
            public static Modifiers? GetModifiers() { return LeftSide_Modifiers; }
            public static List<(string id, string prowess, string name, int effectiveness)> GetKnights() { return LeftSide_Knights; }
            public static bool CheckIfHasKnight(string character_id)
            {
                foreach(var knight in LeftSide_Knights)
                {
                    if(knight.id == character_id)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public struct RightSide
        {
            internal static void SetMainParticipant((string id, string culture_id)data) { RightSide_MainParticipant = data; }
            internal static void SetCommander((string name, string id, int prowess, int martial, int rank, string culture_id) data) { RightSide_Commander = data; }
            internal static void SetRealmName(string name) { RightSide_RealmName = name; }
            internal static void SetModifiers(Modifiers t) { RightSide_Modifiers = t; }
            internal static void SetKnights(List<(string id, string prowess, string name, int effectiveness)> t) { RightSide_Knights = t; }

            public static (string id, string culture_id) GetMainParticipant() { return RightSide_MainParticipant; }
            public static (string name, string id, int prowess, int martial, int rank, string culture_id) GetCommander() { return RightSide_Commander; }
            public static string GetRealmName() { return RightSide_RealmName; }
            public static Modifiers? GetModifiers() { return RightSide_Modifiers; }
            public static List<(string id, string prowess, string name, int effectiveness)> GetKnights() { return RightSide_Knights; }
            public static bool CheckIfHasKnight(string character_id)
            {
                foreach (var knight in RightSide_Knights)
                {
                    if (knight.id == character_id)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public static class DataSearch
    {


        public static PlayerChar? Player_Character { get; set; }

        static string LogPath = Properties.Settings.Default.VAR_log_ck3;

        private static void IdentifyBattleType(string log)
        {
            if (log.Contains("BattleType:CRUSADERCONFLICTS_SIEGE"))
            {
                Program.Logger.Debug("Siege battle detected.");
                twbattle.BattleState.IsSiegeBattle = true;
            }
            else if (log.Contains("BattleType:CRUSADERCONFLICTS_BATTLE"))
            {
                Program.Logger.Debug("Field battle detected.");
                twbattle.BattleState.IsSiegeBattle = false;
            }
            else
            {
                Program.Logger.Debug("Could not determine battle type from keyword. Defaulting to field battle.");
                twbattle.BattleState.IsSiegeBattle = false;
            }
        }

        private static void SearchSiegeData(string log)
        {
            Program.Logger.Debug("Searching for siege-specific data in log...");

            // Holding type key
            string holdingLevel = Regex.Match(log, @"Holding Level:(.+)").Groups[1].Value.Trim();
            Program.Logger.Debug($"Found Holding Level Key: {holdingLevel}");
            twbattle.Sieges.SetHoldingLevelKey(holdingLevel);

            // BreachFrame
            string breachFrameStr = Regex.Match(log, @"BreachFrame:(\d+)").Groups[1].Value;
            if (int.TryParse(breachFrameStr, out int breachFrame))
            {
                string escalation = "";
                switch (breachFrame)
                {
                    case 0: escalation = "Intact"; break;
                    case 1: escalation = "SmallBreach"; break;
                    case 2: escalation = "LargeBreach"; break;
                }
                Program.Logger.Debug($"Found Breach Status: {escalation}");
                twbattle.Sieges.SetHoldingEscalation(escalation);
            }

            // StarvationFrame
            string starvationFrameStr = Regex.Match(log, @"StarvationFrame:(\d+)").Groups[1].Value;
            if (int.TryParse(starvationFrameStr, out int starvationFrame))
            {
                string supplies = "";
                switch (starvationFrame)
                {
                    case 0: supplies = "Fully Stocked"; break;
                    case 1: supplies = "Running Low"; break;
                    case 2: supplies = "Starvation"; break;
                }
                Program.Logger.Debug($"Found Supply Status: {supplies}");
                twbattle.Sieges.SetHoldingSupplies(supplies);
            }

            // DiseaseFrame
            string diseaseFrameStr = Regex.Match(log, @"DiseaseFrame:(\d+)").Groups[1].Value;
            if (int.TryParse(diseaseFrameStr, out int diseaseFrame))
            {
                string sickness = "";
                switch (diseaseFrame)
                {
                    case 0: sickness = "No Diseases"; break;
                    case 1: sickness = "Sickness Spreading"; break;
                    case 2: sickness = "Rampant Disease"; break;
                }
                Program.Logger.Debug($"Found Sickness Status: {sickness}");
                twbattle.Sieges.SetHoldingSickness(sickness);
            }

            // Garrison culture and heritage
            string garrisonCulture = Regex.Match(log, @"Garrison Culture:(.+)").Groups[1].Value.Trim();
            string garrisonHeritage = Regex.Match(log, @"Garrison Heritage:(.+)").Groups[1].Value.Trim();
            Program.Logger.Debug($"Found Garrison Culture: {garrisonCulture}, Heritage: {garrisonHeritage}");
            twbattle.Sieges.SetGarrisonCulture(garrisonCulture);
            twbattle.Sieges.SetGarrisonHeritage(garrisonHeritage);

            // Garrison size
            string garrisonSizeStr = Regex.Match(log, @"Garrison Size:(\d+)").Groups[1].Value;
            if (int.TryParse(garrisonSizeStr, out int garrisonSize))
            {
                Program.Logger.Debug($"Found Garrison Size: {garrisonSize}");
                twbattle.Sieges.SetGarrisonSize(garrisonSize);
            }

            // Attacker's army composition string.
            string attackerArmyComposition = Regex.Match(log, @"---------Attacker Army---------([\s\S]*?)Attacker Martial:").Groups[1].Value.Trim();
            Program.Logger.Debug($"Found Attacker Army Composition for siege.");
            twbattle.Sieges.SetAttackerArmyComposition(attackerArmyComposition);
        }

        public static void Search(string log)
        {
            Program.Logger.Debug("Starting CK3 log search...");
            /*---------------------------------------------
             * ::::::::::::::::Army Ratio::::::::::::::::::
             ---------------------------------------------*/

            //Get Army Ratio in log file...
            ArmyProportions.SetRatio(ModOptions.GetBattleScale());
            Program.Logger.Debug($"Battle scale set to: {ModOptions.GetBattleScale()}");

            /*---------------------------------------------
             * :::::::::::::::::::Geral Data:::::::::::::::
             ---------------------------------------------*/

            IdentifyBattleType(log);
            DateSearch(log);
            BattleNameSearch(log);

            if (twbattle.BattleState.IsSiegeBattle)
            {
                SearchSiegeData(log);
            }

            /*---------------------------------------------
             * :::::::::::::::::Modifiers::::::::::::::::::
             ---------------------------------------------*/
            var modifiers_texts = GetModifiersText(log);
            CK3LogData.LeftSide.SetModifiers(new Modifiers(modifiers_texts.left));
            CK3LogData.RightSide.SetModifiers(new Modifiers(modifiers_texts.right));
            Program.Logger.Debug("Modifiers searched and set for both sides.");

            /*---------------------------------------------
             * ::::::::::::::Main Participants::::::::::::::
             ---------------------------------------------*/
            string left_side_mainparticipant_id = Regex.Match(log, @"LeftSide_Owner_ID:(.+)\n").Groups[1].Value;
            string left_side_mainparticipant_culture_id = Regex.Match(log, @"LeftSide_Owner_Culture:(.+)\n").Groups[1].Value;
            CK3LogData.LeftSide.SetMainParticipant((left_side_mainparticipant_id, left_side_mainparticipant_culture_id));
            Program.Logger.Debug($"Left side main participant: ID={left_side_mainparticipant_id}, CultureID={left_side_mainparticipant_culture_id}");

            string right_side_mainparticipant_id = Regex.Match(log, @"RightSide_Owner_ID:(.+)\n").Groups[1].Value;
            string right_side_mainparticipant_culture_id = Regex.Match(log, @"RightSide_Owner_Culture:(.+)\n").Groups[1].Value;
            CK3LogData.RightSide.SetMainParticipant((right_side_mainparticipant_id, right_side_mainparticipant_culture_id));
            Program.Logger.Debug($"Right side main participant: ID={right_side_mainparticipant_id}, CultureID={right_side_mainparticipant_culture_id}");
            /*---------------------------------------------
             * ::::::::::::::Player Character::::::::::::::
             ---------------------------------------------*/
            string player_culture_id = Regex.Match(log, @"PlayerCharacter_Culture:(.+)\n").Groups[1].Value;
            string playerID = Regex.Match(log, @"PlayerCharacter_ID:(.+)\n").Groups[1].Value;
            Player_Character = new PlayerChar(playerID, player_culture_id);
            Program.Logger.Debug($"Player character: ID={playerID}, CultureID={player_culture_id}");

            /*---------------------------------------------
             * ::::::::::::Commanders ID's:::::::::::::::::
             ---------------------------------------------*/

            //Search player ID
            string left_side_commander_id = Regex.Match(log, @"PlayerID:(\d+)").Groups[1].Value;
            string left_side_commander_culture_id = Regex.Match(log, @"LeftSide_Commander_Culture:(\d+)").Groups[1].Value;
            Program.Logger.Debug($"Left side commander: ID={left_side_commander_id}, CultureID={left_side_commander_culture_id}");

            //Search enemy ID
            string right_side_commander_id = Regex.Match(log, @"EnemyID:(\d+)").Groups[1].Value;
            string right_side_commander_culture_id = Regex.Match(log, @"RightSide_Commander_Culture:(\d+)").Groups[1].Value;
            Program.Logger.Debug($"Right side commander: ID={right_side_commander_id}, CultureID={right_side_commander_culture_id}");

            /*---------------------------------------------
             * :::::::::::::::::Terrain::::::::::::::::::::
             ---------------------------------------------*/
            SearchForProvinceID(log);

            TerrainSearch(log);

            UniqueMapsSearch(log);

            /*---------------------------------------------
             * ::::::::::::::Player Army:::::::::::::::::::
             ---------------------------------------------*/

            string PlayerArmy = Regex.Match(log, @"---------Player Army---------([\s\S]*?)---------Enemy Army---------").Groups[1].Value;


            /*---------------------------------------------
             * ::::::::::Player Commander System:::::::::::
             ---------------------------------------------*/

            CommanderSearch(log, PlayerArmy, DataSearchSides.LeftSide, left_side_commander_id, left_side_commander_culture_id);

            /*---------------------------------------------
             * :::::::::::Player Knight System:::::::::::::
             ---------------------------------------------*/

            KnightsSearch(PlayerArmy, DataSearchSides.LeftSide);


            /*---------------------------------------------
             * ::::::::::::::::Enemy Army::::::::::::::::::
             ---------------------------------------------*/

            string EnemyArmy = Regex.Match(log, @"---------Enemy Army---------([\s\S]*?)---------Completed---------").Groups[1].Value;

            /*---------------------------------------------
             * ::::::::::Enemy Commander System:::::::::::
             ---------------------------------------------*/

            CommanderSearch(log, EnemyArmy, DataSearchSides.RightSide, right_side_commander_id, right_side_commander_culture_id);

            /*---------------------------------------------
             * :::::::::::Enemy Knight System:::::::::::::
             ---------------------------------------------*/
            
            KnightsSearch(EnemyArmy, DataSearchSides.RightSide);

            /*---------------------------------------------
             * ::::::::::::::::Army Names::::::::::::::::::
             ---------------------------------------------*/

            RealmsNamesSearch(log);
            Program.Logger.Debug("Finished CK3 log search.");
        }

        private static void BattleNameSearch(string log)
        {
            string battle_name = Regex.Match(log, @"BattleName:(?<BattleName>.+)\n").Groups["BattleName"].Value;
            BattleDetails.SetBattleName(battle_name);
            Program.Logger.Debug($"Battle name set to: {battle_name}");
        }

        static void DateSearch(string log)
        {
            string month;
            string year;
            try
            {
                month = Regex.Match(log, @"DateMonth:(\d+)").Groups[1].Value;
                year = Regex.Match(log, @"DateYear:(\d+)").Groups[1].Value;
                Date.Month = Int32.Parse(month);
                Date.Year = Int32.Parse(year);
                Program.Logger.Debug($"Date searched: Month={month}, Year={year}");

                string season = Date.GetSeason();
                Weather.SetSeason(season);
                Program.Logger.Debug($"Season set to: {season}");
            }
            catch
            {
                month = "1"; // Default to January
                year = "1300";
                Date.Month = Int32.Parse(month);
                Date.Year = Int32.Parse(year);
                Weather.SetSeason("random");
            }

        }


        public static (string left, string right) GetModifiersText(string log)
        {
            string left_side_advantages = "";
            string right_side_advantages = "";

            using (StringReader stringReader = new StringReader(log))
            {
                bool searchStarted = false;
                bool leftReadStart = false;
                bool rightReadStart = false;

                string? line;
                while ((line = stringReader.ReadLine()) != null)
                {
                    if (!searchStarted && line == "---------Modifiers---------")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted)
                    {
                        if (line.StartsWith("Keyword:"))
                        {
                            break;
                        }

                        if (!leftReadStart && line == "")
                        {
                            leftReadStart = true;
                        }
                        else if (leftReadStart && line == "")
                        {
                            leftReadStart = false;
                            rightReadStart = true;
                            continue;
                        }
                        else if (leftReadStart)
                        {
                            left_side_advantages += line;
                        }
                        else if(rightReadStart)
                        {
                            right_side_advantages += line;
                        }


                    }
                }
            }

            return (left_side_advantages, right_side_advantages);
        }
        static void TerrainSearch(string log)
        {
            TerrainGenerator.TerrainType = SearchForTerrain(log);
            Program.Logger.Debug($"Terrain type found: {TerrainGenerator.TerrainType}");
            Weather.SetWinterSeverity(SearchForWinter(log));
        }
        static void CommanderSearch(string log, string army_data, DataSearchSides side, string id, string culture_id)
        {
            string name = "";
            int martial = 0;
            int prowess = 0;
            int rank = 0;


            Match martial_match = Regex.Match(army_data, @"positive_value (\d+)");
            if (martial_match.Success)
            {
                string martial_str = martial_match.Groups[1].Value;
                martial = Int32.Parse(martial_str);
            }
            else
            {
                martial = 0;
            }


            string pattern = @"";
            if(side is DataSearchSides.LeftSide) { 
                pattern = @"PlayerProwess:(?<Num>\d+)";
                rank = Int32.Parse(Regex.Match(log, @"PlayerRank:(?<Name>.+)").Groups["Name"].Value);
                name = Regex.Match(log, @"PlayerName:(?<Name>.+)").Groups["Name"].Value;
            }
            else if (side is DataSearchSides.RightSide) { 
                pattern = @"EnemyProwess:(?<Num>\d+)"; 
                rank = Int32.Parse(Regex.Match(log, @"EnemyRank:(?<Name>.+)").Groups["Name"].Value);
                name = Regex.Match(log, @"EnemyName:(?<Name>.+)").Groups["Name"].Value;
            }

            Match prowess_match = Regex.Match(army_data, pattern);
            if (prowess_match.Success)
            {
                string prowess_str = prowess_match.Groups["Num"].Value;
                prowess = Int32.Parse(prowess_str);
            }
            else
            {
                prowess = 0;
            }

            
            

            if (side is DataSearchSides.LeftSide)
            {
                CK3LogData.LeftSide.SetCommander((name, id, prowess, martial, rank, culture_id));
            }
            else if (side is DataSearchSides.RightSide)
            {
                CK3LogData.RightSide.SetCommander((name, id, prowess, martial, rank,culture_id));
            }

        }
        static void RealmsNamesSearch(string log)
        {
            string text = Regex.Match(log, "(Log[\\s\\S]*?)---------Player Army---------[\\s\\S]*?").Groups[1].Value;
            MatchCollection found_armies = Regex.Matches(text, "L (.+)");
            if(found_armies.Count >= 1)
            {
                string player_army = found_armies[0].Groups[1].Value;
                string enemy_army = found_armies[1].Groups[1].Value;

                CK3LogData.LeftSide.SetRealmName(player_army);
                CK3LogData.RightSide.SetRealmName(enemy_army);
                Program.Logger.Debug($"Left side realm name: {player_army}");
                Program.Logger.Debug($"Right side realm name: {enemy_army}");
            }
        }



        static void KnightsSearch(string army_data, DataSearchSides side)
        {
            string Knights = Regex.Match(army_data, @"(?<Knights>ONCLICK:CHARACTER[\s\S]*?)\z[\s\S]*?").Groups["Knights"].Value;
            MatchCollection knights_text_data = Regex.Matches(Knights, @"ONCLICK:CHARACTER(?<ID>\d+).+ (?<Prowess>\d+)");

            List<(string id, string prowess, string name, int effectivenss)> data = new List<(string id, string prowess, string name, int effectivenss)>();
            string names = Knights;
            names = names.Replace("high", "");
            string[] names_arr = new string[knights_text_data.Count];
            int count = 0;
            foreach (Match knight in Regex.Matches(names, @"L  (.+): "))
            {
                string name = knight.Groups[1].Value;
                name = Regex.Replace(name,@"\s+", " ");
                names_arr[count] = name;
                count++;
            }

            MatchCollection knight_effectiveness = Regex.Matches(Knights, @"(?<Effectiveness>\d+)%");
            int effectiveness = 0;
            foreach (Match effect in knight_effectiveness)
            {
                int value = Int32.Parse(effect.Groups["Effectiveness"].Value);
                effectiveness += value;
            }

            for (int i = 0; i< knights_text_data.Count; i++)
            {
                var knight = knights_text_data[i];
                data.Add((knight.Groups["ID"].Value, knight.Groups["Prowess"].Value, names_arr[i], effectiveness));
            }




            if (side == DataSearchSides.LeftSide)
            {
                CK3LogData.LeftSide.SetKnights(data);
            }
            else if (side == DataSearchSides.RightSide)
            {
                CK3LogData.RightSide.SetKnights(data);
            }

        }


        static void UniqueMapsSearch(string log)
        {
            Match match = Regex.Match(log, @"SpecialBuilding:(.+)");
            if(match.Success)
            {
                string building_key = match.Groups[1].Value;
                UniqueMaps.ReadSpecialBuilding(building_key);
            }
        }


        static string SearchForTerrain(string content)
        {
            string terrain_data = Regex.Match(content, "---------Completed---------([\\s\\S]*?)PlayerID").Groups[1].Value;
            terrain_data = HomePage.RemoveASCII(terrain_data);

            string region_data = Regex.Match(terrain_data, @"Region:(.+)").Groups[1].Value;
            TerrainGenerator.SetRegion(region_data);

            string terrain = Regex.Match(terrain_data, @"TOOLTIP:TERRAIN(.+) L").Groups[1].Value;
            return terrain;
        }

        public static void ClearLogFile()
        {
            Program.Logger.Debug($"Clearing CK3 log file at: {LogPath}");
            if (!File.Exists(LogPath)) File.Create(LogPath)?.Close(); // Close the created file stream immediately

            using (var fileStream = new FileStream(LogPath, FileMode.Truncate))
            {
                // Truncate the file, effectively clearing its contents
                fileStream.Close();
            }


        }


        static string SearchForWinter(string content)
        {
            string terrain_data = Regex.Match(content, "---------Completed---------([\\s\\S]*?)PlayerID").Groups[1].Value;

            string[] AllWinter = new string[] {"Mild", "Normal", "Harsh" ,
                                              "suave", "normal", "duro" ,
                                              "Hiver doux", "Hiver normal", "Hiver rude",
                                              "Milder", "Normaler", "Rauer",
                                              "Мягкие", "Обычные", "Суровые",
                                              "温暖的", "普通的", "严酷的"
             };


            for (int i = 0; i < AllWinter.Length; i++)
            {
                Match hasFound = Regex.Match(terrain_data, $"{AllWinter[i]}");

                if (hasFound.Success)
                {
                    string winter = hasFound.Value;
                    return winter;
                }
            }

            return string.Empty;

        }

        static void SearchForProvinceID(string log)
        {
            string provinceID;
            try
            {
                provinceID = Regex.Match(log, @"ProvinceID:(.+)\n").Groups[1].Value;
                Program.Logger.Debug($"Province ID found: {provinceID}");
            }
            catch
            {
                provinceID = "not found";
                Program.Logger.Debug("Province ID not found in log.");
            }

            BattleResult.ProvinceID = provinceID;
        }


       
    }
}
