using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.terrain
{
    public static class Straits
    {
        struct BattleMaps
        {
            public static (string X, string Y, string[] attackerPositions, string[] defenderPositions)[] Greece_Straits = new[]
                {
                ("0.574", "0.609", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.592", "0.601", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.504", "0.599", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.490", "0.516", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.446", "0.610", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.480", "0.485", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.529", "0.485", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.623", "0.386", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.610", "0.387", new string[]{"E", "E"}, new string[]{"W", "W"}),
                ("0.448", "0.500", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.352", "0.406", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.361", "0.421", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.378", "0.438", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.400", "0.518", new string[]{"W", "W"}, new string[]{"E", "E"})
                };

            public static (string X, string Y, string[] attackerPositions, string[] defenderPositions)[] Italy_Straits = new[]
                {
                ("0.259", "0.491", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.259", "0.503", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.337", "0.567", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.328", "0.572", new string[]{"N", "N"}, new string[]{"S", "S"})
                };

            public static (string X, string Y, string[] attackerPositions, string[] defenderPositions)[] Northern_Straits = new[]
                {
                ("0.101", "0.141", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.278", "0.116", new string[]{"E", "E"}, new string[]{"W", "W"}),
                ("0.285", "0.121", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.291", "0.125", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.300", "0.114", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.307", "0.127", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.319", "0.100", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.289", "0.151", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.173", "0.231", new string[]{"S", "S"}, new string[]{"N", "N"})
                };

            public static (string X, string Y, string[] attackerPositions, string[] defenderPositions)[] Europe_Straits = new[]
                {
                ("0.225", "0.184", new string[]{"W", "W"}, new string[]{"E", "E"}),
                ("0.216", "0.208", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.139", "0.398", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.125", "0.331", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.141", "0.243", new string[]{"N", "N"}, new string[]{"S", "S"})
                };

            public static(string X, string Y, string[] attackerPositions, string[] defenderPositions)[] Desert_Straits = new[]
                {
                ("0.822", "0.472", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.823", "0.496", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.702", "0.946", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.817", "0.781", new string[]{"N", "N"}, new string[]{"S", "S"}),
                ("0.820", "0.767", new string[]{"S", "S"}, new string[]{"N", "N"}),
                ("0.764", "0.718", new string[]{"W", "W"}, new string[]{"E", "E"})
                };


        };


        public static (string X, string Y, string[] attackerPositions, string[] defenderPositions) GetBattleMap(string region, string terrain)
        {
            Random random = new Random();
            string[] attPositions;
            string[] defPositions;
            (string X, string Y) Coordinates;

            switch (region)
            {
                case "Britannia":
                case "Northern Europe":
                    int index = random.Next(0, BattleMaps.Northern_Straits.Length);
                    Coordinates.X = BattleMaps.Northern_Straits[index].X;
                    Coordinates.Y = BattleMaps.Northern_Straits[index].Y;
                    attPositions = BattleMaps.Northern_Straits[index].attackerPositions;
                    defPositions = BattleMaps.Northern_Straits[index].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                
                case "Italia":
                    int i = random.Next(0, BattleMaps.Italy_Straits.Length);
                    Coordinates.X = BattleMaps.Italy_Straits[i].X;
                    Coordinates.Y = BattleMaps.Italy_Straits[i].Y;
                    attPositions = BattleMaps.Italy_Straits[i].attackerPositions;
                    defPositions = BattleMaps.Italy_Straits[i].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);

                case "South-Eastern Europe":
                    int y = random.Next(0, BattleMaps.Greece_Straits.Length);
                    Coordinates.X = BattleMaps.Greece_Straits[y].X;
                    Coordinates.Y = BattleMaps.Greece_Straits[y].Y;
                    attPositions = BattleMaps.Greece_Straits[y].attackerPositions;
                    defPositions = BattleMaps.Greece_Straits[y].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                case "Europe":
                    int l = random.Next(0, BattleMaps.Europe_Straits.Length);
                    Coordinates.X = BattleMaps.Europe_Straits[l].X;
                    Coordinates.Y = BattleMaps.Europe_Straits[l].Y;
                    attPositions = BattleMaps.Europe_Straits[l].attackerPositions;
                    defPositions = BattleMaps.Europe_Straits[l].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                case "Middle East":
                case "Africa":
                    int t = random.Next(0, BattleMaps.Desert_Straits.Length);
                    Coordinates.X = BattleMaps.Desert_Straits[t].X;
                    Coordinates.Y = BattleMaps.Desert_Straits[t].Y;
                    attPositions = BattleMaps.Desert_Straits[t].attackerPositions;
                    defPositions = BattleMaps.Desert_Straits[t].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
            }

            switch(terrain)
            {
                //desert
                case "Desert":
                case "Desierto":
                case "Désert":
                case "Wüste":
                case "Пустыня":
                case "사막":
                case "沙漠":
                //desert mountains
                case "Desert Mountains":
                case "Montaña desértica":
                case "Montagnes désertiques":
                case "Bergwüste":
                case "Пустынные горы":
                case "사막 산악":
                case "沙漠山地":
                //drylands
                case "Drylands":
                case "Tierras áridas":
                case "Terres arides":
                case "Trockengebiet":
                case "Засушливые земли":
                case "건조지":
                case "旱地":
                //oasis
                case "Oasis":
                //case "Oasis": spanish
                //case "Oasis": french
                case "Oase":
                case "Оазис":
                case "오아시스":
                case "绿洲":
                //floodplains
                case "Floodplains":
                case "Llanura aluvial":
                case "Plaine inondable":
                case "Auen":
                case "Поймы рек":
                case "범람원":
                case "洪泛平原":
                //jungle
                case "Jungle":
                case "Selva":
                // case "Jungle": french
                case "Dschungel":
                case "Джунгли":
                case "밀림":
                case "丛林":
                    int t = random.Next(0, BattleMaps.Desert_Straits.Length);
                    Coordinates.X = BattleMaps.Desert_Straits[t].X;
                    Coordinates.Y = BattleMaps.Desert_Straits[t].Y;
                    attPositions = BattleMaps.Desert_Straits[t].attackerPositions;
                    defPositions = BattleMaps.Desert_Straits[t].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                //farmlands
                case "Farmlands":
                case "Tierra de cultivo":
                case "Terres arables":
                case "Ackerland":
                case "Пахотные земли":
                case "농지":
                case "农田":
                //forest
                case "Forest":
                case "Bosque":
                case "Forêt":
                case "Wald":
                case "Лес":
                case "삼림":
                case "森林":
                //hills
                case "Hills":
                case "Colina":
                case "Collines":
                case "Hügel":
                case "Холмы":
                case "구릉지":
                case "丘陵":
                //mountains
                case "Mountains":
                case "Montaña":
                case "Montagnes":
                case "Berge":
                case "Горы":
                case "산악":
                case "山地":
                //plains
                case "Plains":
                case "Llanura":
                case "Plaines":
                case "Ebenen":
                case "Равнины":
                case "평야":
                case "平原":
                    int i = random.Next(0, BattleMaps.Europe_Straits.Length);
                    Coordinates.X = BattleMaps.Europe_Straits[i].X;
                    Coordinates.Y = BattleMaps.Europe_Straits[i].Y;
                    attPositions = BattleMaps.Europe_Straits[i].attackerPositions;
                    defPositions = BattleMaps.Europe_Straits[i].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);
                //steppe
                case "Steppe":
                case "Estepa":
                //case "Steppe":
                //case "Steppe":
                case "Степь":
                case "초원":
                case "草原":
                //taiga
                case "Taiga":
                //case "Taiga":
                case "Taïga":
                //case "Taiga":
                case "Тайга":
                case "침엽수림":
                case "针叶林":
                //wetlands
                case "Wetlands":
                case "Pantano":
                case "Marécages":
                case "Feuchtgebiet":
                case "Болота":
                case "습지대":
                case "湿地":
                    int x = random.Next(0, BattleMaps.Northern_Straits.Length);
                    Coordinates.X = BattleMaps.Northern_Straits[x].X;
                    Coordinates.Y = BattleMaps.Northern_Straits[x].Y;
                    attPositions = BattleMaps.Northern_Straits[x].attackerPositions;
                    defPositions = BattleMaps.Northern_Straits[x].defenderPositions;
                    return (Coordinates.X, Coordinates.Y, attPositions, defPositions);

            }

            return ("0.574", "0.609", new string[] { "N", "N" }, new string[] { "S", "S" });
        }


    }
}
