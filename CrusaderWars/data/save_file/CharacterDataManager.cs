using CrusaderWars; // Changed from CrusaderWars.data.ck3_log
using CrusaderWars.data.save_file;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.data.save_file
{
    public static class CharacterDataManager
    {
        private static Dictionary<string, (string firstName, string nickname)> _characterNamesCache = new Dictionary<string, (string, string)>();
        private static string? _playerRealmNameCache;
        private static string? _enemyRealmNameCache;

        public static (string firstName, string nickname) GetCharacterFirstNameAndNickname(string characterId)
        {
            if (_characterNamesCache.TryGetValue(characterId, out var names))
            {
                return names;
            }

            // If not in cache, return empty strings. The logic to retrieve names from the log
            // would typically reside in DataSearch, but it does not expose this method.
            // Therefore, CharacterDataManager acts purely as a cache for character names.
            return ("", "");
        }

        public static string GetPlayerRealmName()
        {
            if (_playerRealmNameCache != null)
            {
                return _playerRealmNameCache;
            }

            string playerCharId = DataSearch.Player_Character.GetID();
            string playerRealmName = "";

            // Determine if player is on LeftSide or RightSide from CK3LogData
            bool playerIsOnLeftSide = CK3LogData.LeftSide.GetMainParticipant().id == playerCharId ||
                                      CK3LogData.LeftSide.GetCommander().id == playerCharId ||
                                      CK3LogData.LeftSide.CheckIfHasKnight(playerCharId);

            if (playerIsOnLeftSide)
            {
                playerRealmName = CK3LogData.LeftSide.GetRealmName();
            }
            else
            {
                playerRealmName = CK3LogData.RightSide.GetRealmName();
            }

            _playerRealmNameCache = playerRealmName;
            return playerRealmName;
        }

        public static string GetEnemyRealmName()
        {
            if (_enemyRealmNameCache != null)
            {
                return _enemyRealmNameCache;
            }

            string playerCharId = DataSearch.Player_Character.GetID();
            string enemyRealmName = "";

            // Determine if player is on LeftSide or RightSide from CK3LogData
            bool playerIsOnLeftSide = CK3LogData.LeftSide.GetMainParticipant().id == playerCharId ||
                                      CK3LogData.LeftSide.GetCommander().id == playerCharId ||
                                      CK3LogData.LeftSide.CheckIfHasKnight(playerCharId);

            if (playerIsOnLeftSide)
            {
                enemyRealmName = CK3LogData.RightSide.GetRealmName();
            }
            else
            {
                enemyRealmName = CK3LogData.LeftSide.GetRealmName();
            }

            _enemyRealmNameCache = enemyRealmName;
            return enemyRealmName;
        }

        public static void ClearCache()
        {
            _characterNamesCache.Clear();
            _playerRealmNameCache = null;
            _enemyRealmNameCache = null;
        }
    }
}
