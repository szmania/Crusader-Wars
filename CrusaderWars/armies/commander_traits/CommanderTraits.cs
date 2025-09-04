using CrusaderWars.data.save_file;
using CrusaderWars.terrain;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace CrusaderWars.armies.commander_traits
{
    public enum TraitsAffectEnum
    {
        Friendlies,
        Enemies,
        None
    }

    public enum TraitsBoolCondition
    {
        None,
        Yes,
        No
    }
    public class Trait
    {
        //  VARIABLES
        string Key { get; set; }
        int Index { get; set; }

        //  MAJOR SETUP
        int XPBoost {  get; set; }
        int DeployablesBoost { get; set; }
        TraitsAffectEnum Affect { get; set; }
        bool DeploymentRotation {  get; set; }

        //  CONDITIONS
        string? CombatSide { get; set; }
        List<string>? Terrains {  get; set; }
        TraitsBoolCondition NeedsRiverCrossing { get; set; }
        TraitsBoolCondition NeedsHostileFaith {  get; set; }
        TraitsBoolCondition NeedsWinter { get;set; }

        public Trait(string key, int index) { 
            Key = key;
            Index = index;
        }

        public string GetKey() { return Key; }
        public int GetIndex() { return Index; }

        public int GetXPBoost() {  return XPBoost; }
        public int GetDeployablesBoost() { return DeployablesBoost; }
        public TraitsAffectEnum GetWhoAffects() { return Affect; }
        public bool IsDeploymentRotation() {  return DeploymentRotation; }
        public string? IsRequiredCombatSide() {  return CombatSide; }
        public List<string>? IsRequiredTerrains() { return Terrains; }
        public TraitsBoolCondition IsRequiredRiverCrossing() {  return NeedsRiverCrossing; }
        public TraitsBoolCondition IsRequiredHostileFaith() { return NeedsHostileFaith; }
        public TraitsBoolCondition IsRequiredWinter() { return NeedsWinter; }

        public void SetupTrait(int XPboost, int deployablesBoost,TraitsAffectEnum affectsWho, bool rotatesDeployment, 
                               string requiredCombatSide, List<string> requiredTerrains, TraitsBoolCondition requiresRiverCrossing, TraitsBoolCondition requiresHostileFaith, TraitsBoolCondition requiresWinter)
        {
            XPBoost = XPboost;
            DeployablesBoost = deployablesBoost;
            Affect = affectsWho;
            DeploymentRotation = rotatesDeployment;
            CombatSide = requiredCombatSide;
            Terrains = requiredTerrains;
            NeedsRiverCrossing = requiresRiverCrossing;
            NeedsHostileFaith = requiresHostileFaith;
            NeedsWinter = requiresWinter;
        }

    }
    public class CommanderTraits
    {
        public List<Trait>? Traits { get; private set; }

        static string traits_folder_path = @".\data\traits\";


        public CommanderTraits(List<(int, string)> main_commander_traits) 
        {
            SearchTraitsFiles(main_commander_traits);
        }

        public bool ShouldRotateDeployment(string combat_side, string terrain)
        {
            if (Traits == null) return false;
            foreach(var trait in Traits) 
            {
                if(trait.IsDeploymentRotation() && combat_side == trait.IsRequiredCombatSide())
                {
                    if (trait.IsRequiredTerrains() != null)
                    {
                        if (trait.IsRequiredTerrains().Exists(x => x == terrain))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public int GetDeployablesBoost()
        {
            if (Traits == null) return 0;

            int deployables_boost = 0;
            foreach (Trait trait in Traits)
            {
                deployables_boost += trait.GetDeployablesBoost();
            }
            return deployables_boost;
        }

        public int GetBenefits(string combatSide, string terrainType, bool isRiverCrossing, bool isHostileFaith, bool isWinter)
        {
            if (Traits == null) return 0;

            int xp_boost = 0;
            foreach(Trait trait in Traits)
            {

                //  COMBAT SIDE
                if (trait.IsRequiredCombatSide() == "no")
                {
                    //ok
                }
                else if(combatSide != trait.IsRequiredCombatSide())
                {
                    continue;
                }

                //  TERRAINS
                if(trait.IsRequiredTerrains() != null)
                {
                    if(!trait.IsRequiredTerrains().Exists(x => x == terrainType))
                    {
                        continue;
                    }
                }

                //  RIVER CROSSING
                switch (trait.IsRequiredRiverCrossing())
                {
                    case TraitsBoolCondition.None: 
                        break;
                    case TraitsBoolCondition.No:
                        if (isRiverCrossing)
                            continue;
                        break;
                    case TraitsBoolCondition.Yes: 
                        if(!isRiverCrossing)
                        {
                            continue;
                        }
                        break;
                }

                //  WINTER
                switch (trait.IsRequiredWinter())
                {
                    case TraitsBoolCondition.None:
                        break;
                    case TraitsBoolCondition.No:
                        if (isWinter)
                            continue;
                        break;
                    case TraitsBoolCondition.Yes:
                        if (!isWinter)
                        {
                            continue;
                        }
                        break;
                }


                xp_boost += trait.GetXPBoost();
                Program.Logger.Debug($"TRAIT {trait.GetKey()} increased +{xp_boost}XP");
            }

            return xp_boost;
        }

        void SearchTraitsFiles(List<(int Index, string Key)> main_commander_traits)
        {
            foreach (var file_path in Directory.GetFiles(traits_folder_path))
            {
                if (Path.GetExtension(file_path) == ".xml")
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(file_path);

                    foreach (XmlNode traitElement in xmlDocument.DocumentElement.ChildNodes)
                    {
                        if (traitElement is XmlComment) continue;
                        string trait_key = traitElement.Attributes["name"].Value;

                        if (main_commander_traits.Exists(x => x.Key == trait_key))
                        {
                            Trait commanderTrait = new Trait(trait_key, main_commander_traits.Find(x => x.Key == trait_key).Index);

                            int xp_boost = 0;
                            int deployables_boost = 0;
                            TraitsAffectEnum whoAffects = TraitsAffectEnum.None;
                            bool rotatesDeployment = false;

                            string combatSide = "no";
                            List<string>? Terrains = null;
                            var riverCrossing = TraitsBoolCondition.None;
                            var hostileFaith = TraitsBoolCondition.None;
                            var winter = TraitsBoolCondition.None;

                            foreach (XmlNode traitNode in traitElement.ChildNodes)
                            {
                                if (traitNode is XmlComment) continue;

                                switch (traitNode.Name)
                                {
                                    case "XpBoost":
                                        bool parsed = Int32.TryParse(traitNode.InnerText, out xp_boost);
                                        if (!parsed) xp_boost = 0;
                                        break;

                                    case "Affects":
                                        if (traitNode.InnerText == "friendlies")
                                            whoAffects = TraitsAffectEnum.Friendlies;
                                        else if (traitNode.InnerText == "enemies")
                                            whoAffects = TraitsAffectEnum.Enemies;
                                        else
                                        {
                                            Program.Logger.Debug("WRONG VALUE IN AFFECT XML NODE");
                                            whoAffects = TraitsAffectEnum.Friendlies;
                                        }
                                        break;
                                    case "DeployablesBoost":
                                        bool parsedDeployables = Int32.TryParse(traitNode.InnerText, out deployables_boost);
                                        if (!parsedDeployables) deployables_boost = 0;
                                        break;

                                    case "DeploymentRotation":
                                        if (traitNode.InnerText == "yes")
                                            rotatesDeployment = true;
                                        else
                                            rotatesDeployment = false;
                                        break;

                                    case "CombatSide":
                                        if (traitNode.InnerText == "attacker")
                                            combatSide = "attacker";
                                        else if (traitNode.InnerText == "defender")
                                            combatSide = "defender";
                                        else
                                        {
                                            Program.Logger.Debug("WRONG VALUE IN COMBAT SIDE XML NODE");
                                            combatSide = "attacker";
                                        }
                                        break;

                                    case "Terrain":
                                        if (Terrains == null)
                                        {
                                            Terrains = new List<string>
                                            {
                                                traitNode.InnerText
                                            };
                                        }
                                        else
                                        {
                                            Terrains.Add(traitNode.InnerText);
                                        }
                                        break;

                                    case "RiverCrossing":
                                        if (traitNode.InnerText == "yes")
                                            riverCrossing = TraitsBoolCondition.Yes;
                                        else
                                            riverCrossing = TraitsBoolCondition.No;
                                        break;

                                    case "HostileFaith":
                                        if (traitNode.InnerText == "yes")
                                            riverCrossing = TraitsBoolCondition.Yes;
                                        else
                                            riverCrossing = TraitsBoolCondition.No;
                                        break;

                                    case "Winter":
                                        if (traitNode.InnerText == "yes")
                                            riverCrossing = TraitsBoolCondition.Yes;
                                        else
                                            riverCrossing = TraitsBoolCondition.No;
                                        break;
                                }
                            }

                            commanderTrait.SetupTrait(xp_boost, deployables_boost, whoAffects, rotatesDeployment, combatSide, Terrains, riverCrossing, hostileFaith, winter);
                            if (Traits == null)
                            {
                                Traits = new List<Trait>
                                {
                                    commanderTrait
                                };
                            }
                            else
                            {
                                Traits.Add(commanderTrait);
                            }

                        }
                    }
                }
            }
        }
    }
}
