using System.Collections.Generic;
using CrusaderWars.armies.commander_traits; // For CommanderTraits
using CrusaderWars.client; // For Program.Logger

namespace CrusaderWars.client
{
    public static class CK3LogData
    {
        public static SideData LeftSide { get; } = new SideData("left");
        public static SideData RightSide { get; } = new SideData("right");
    }

    public class SideData
    {
        private string _sideName;

        public SideData(string sideName)
        {
            _sideName = sideName;
        }

        public CommanderData GetCommander()
        {
            Program.Logger.Debug($"CK3LogData.GetCommander() called for {_sideName} side (placeholder).");
            return new CommanderData($"commander_id_{_sideName}", $"Commander {_sideName}", "10", "20", 5, $"culture_id_{_sideName}");
        }

        public List<KnightData> GetKnights()
        {
            Program.Logger.Debug($"CK3LogData.GetKnights() called for {_sideName} side (placeholder).");
            return new List<KnightData> { new KnightData($"knight_id_1_{_sideName}", $"Knight 1 {_sideName}", "15", 80) };
        }

        public MainParticipantData GetMainParticipant()
        {
            Program.Logger.Debug($"CK3LogData.GetMainParticipant() called for {_sideName} side (placeholder).");
            return new MainParticipantData($"main_participant_culture_id_{_sideName}");
        }

        public ModifiersData GetModifiers()
        {
            Program.Logger.Debug($"CK3LogData.GetModifiers() called for {_sideName} side (placeholder).");
            return new ModifiersData();
        }

        public string GetRealmName()
        {
            Program.Logger.Debug($"CK3LogData.GetRealmName() called for {_sideName} side (placeholder).");
            return $"Realm of {_sideName}";
        }
    }

    public class CommanderData
    {
        public string id;
        public string name;
        public string prowess;
        public string martial;
        public int rank;
        public string culture_id;

        public CommanderData(string id, string name, string prowess, string martial, int rank, string culture_id)
        {
            this.id = id;
            this.name = name;
            this.prowess = prowess;
            this.martial = martial;
            this.rank = rank;
            this.culture_id = culture_id;
        }
    }

    public class KnightData
    {
        public string id;
        public string name;
        public string prowess;
        public int effectiveness;

        public KnightData(string id, string name, string prowess, int effectiveness)
        {
            this.id = id;
            this.name = name;
            this.prowess = prowess;
            this.effectiveness = effectiveness;
        }
    }

    public class MainParticipantData
    {
        public string culture_id;

        public MainParticipantData(string culture_id)
        {
            this.culture_id = culture_id;
        }
    }

    public class ModifiersData
    {
        public int GetXP()
        {
            return 0; // Placeholder
        }
    }
}
