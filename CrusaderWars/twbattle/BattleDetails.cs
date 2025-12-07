namespace CrusaderWars.twbattle {
    public static class BattleDetails {
        public static string? BattleName { get; private set; }
        public static void SetBattleName(string name) {
            BattleName = name;
        }
    }
}
