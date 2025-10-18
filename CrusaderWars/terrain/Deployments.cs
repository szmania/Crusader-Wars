using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization; // Added for CultureInfo
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.client;
using CrusaderWars.twbattle;

namespace CrusaderWars.terrain
{
    public static class Deployments
    {
        private static readonly Random _random = new Random();

        //Radius
        static string ROTATION_0º = "0.00";
        static string ROTATION_180º = "3.14";
        static string ROTATION_360º = "6.28";

        // Siege center coordinates in meters
        private static string siege_center_x = "0.00";
        private static string siege_center_y = "0.00";

        public static string GetSiegeCenterX() { return siege_center_x; }
        public static string GetSiegeCenterY() { return siege_center_y; }


        struct Directions
        {
            static string SetSouth(DeploymentArea SOUTH_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{SOUTH_DEPLOYMENT_AREA.X}\" y =\"{SOUTH_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{SOUTH_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{SOUTH_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_0º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetWest(DeploymentArea WEST_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{WEST_DEPLOYMENT_AREA.X}\" y =\"{WEST_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{WEST_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{WEST_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_180º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetEast(DeploymentArea EAST_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{EAST_DEPLOYMENT_AREA.X}\" y =\"{EAST_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{EAST_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{EAST_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_360º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetNorth(DeploymentArea NORTH_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{NORTH_DEPLOYMENT_AREA.X}\" y =\"{NORTH_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{NORTH_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{NORTH_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_180º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }

            public static string? SetOppositeDirection(string direction, int total_soldiers)
            {
                DeploymentArea DEPLOYMENT_AREA;
                switch (direction)
                {
                    case "N":
                        DEPLOYMENT_AREA = new DeploymentArea("S", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetSouth(DEPLOYMENT_AREA);
                    case "S":
                        DEPLOYMENT_AREA = new DeploymentArea("N", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetNorth(DEPLOYMENT_AREA);
                    case "E":
                        DEPLOYMENT_AREA = new DeploymentArea("W", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetWest(DEPLOYMENT_AREA);
                    case "W":
                        DEPLOYMENT_AREA = new DeploymentArea("E", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetEast(DEPLOYMENT_AREA);
                    default:
                        throw new ArgumentException($"Invalid direction provided: {direction}", nameof(direction));
                }
            }

            public static string? SetDirection(string direction, int total_soldiers)
            {
                DeploymentArea DEPLOYMENT_AREA = new DeploymentArea(direction, ModOptions.DeploymentsZones(), total_soldiers);
                switch (direction)
                {
                    case "N":
                        return Directions.SetNorth(DEPLOYMENT_AREA);
                    case "S":
                        return Directions.SetSouth(DEPLOYMENT_AREA);
                    case "E":
                        return Directions.SetEast(DEPLOYMENT_AREA);
                    case "W":
                        return Directions.SetWest(DEPLOYMENT_AREA);
                    default:
                        throw new ArgumentException($"Invalid direction provided: {direction}", nameof(direction));
                }
            }

            public static string GetOppositeDirection(string direction)
            {
                switch (direction)
                {
                    case "N":
                        return "S";
                    case "S":
                        return "N";
                    case "E":
                        return "W";
                    case "W":
                        return "E";
                    default:
                        throw new ArgumentException($"Invalid direction provided: {direction}", nameof(direction));
                }
            }
        }
 
        static string attacker_direction = "", defender_direction = "";
        static string? attacker_deployment, defender_deployment = "";
        public static void beta_SetSidesDirections(int total_soldiers, (string x, string y, string[] attacker_dir, string[] defender_dir) battle_map, bool shouldRotateDeployment)
        {
            bool useRotatedDeployment = BattleState.AutofixDeploymentRotationOverride ?? shouldRotateDeployment;
            //All directions battle maps
            if (battle_map.attacker_dir[0] == "All")
            {
                string[] coords = { "N", "S", "E", "W" };
                int index = _random.Next(0, 4);
                attacker_direction = coords[index];
                defender_direction = Directions.GetOppositeDirection(attacker_direction);
                attacker_deployment =  Directions.SetDirection(attacker_direction, total_soldiers);
                defender_deployment = Directions.SetOppositeDirection(attacker_direction, total_soldiers);


            }
            //Defined directions battle maps
            else
            {
                if(useRotatedDeployment)
                {
                    int defender_index = _random.Next(0, 2);
                    defender_direction = battle_map.attacker_dir[defender_index];
                    attacker_direction = Directions.GetOppositeDirection(defender_direction);
                    defender_deployment = Directions.SetDirection(defender_direction, total_soldiers);
                    attacker_deployment = Directions.SetOppositeDirection(defender_direction, total_soldiers);
                }
                else
                {
                    int defender_index = _random.Next(0, 2);
                    defender_direction = battle_map.defender_dir[defender_index];
                    attacker_direction = Directions.GetOppositeDirection(defender_direction);
                    defender_deployment = Directions.SetDirection(defender_direction, total_soldiers);
                    attacker_deployment = Directions.SetOppositeDirection(defender_direction, total_soldiers);
                }
            }

        }

        public static void beta_SetSiegeDeployment((string x, string y, string[] attacker_dir, string[] defender_dir) battle_map, int total_soldiers, List<string>? besiegerOrientations)
        {
            siege_center_x = battle_map.x;
            siege_center_y = battle_map.y;

            // Determine map size to scale defender deployment area appropriately
            string mapSize;
            string optionMapSize = BattleState.AutofixDeploymentSizeOverride ?? ModOptions.DeploymentsZones();
            if (optionMapSize == "Dynamic")
            {
                int holdingLevel = Sieges.GetHoldingLevel();
                if (holdingLevel <= 2) { mapSize = "Medium"; }
                else if (holdingLevel <= 4) { mapSize = "Big"; }
                else { mapSize = "Huge"; }
            }
            else
            {
                mapSize = optionMapSize;
            }

            string width, height;
            switch (mapSize)
            {
                case "Medium":
                    width = "1200";
                    height = "1200";
                    break;
                case "Big":
                    width = "1300";
                    height = "1300";
                    break;
                case "Huge":
                    width = "1750";
                    height = "1750";
                    break;
                default: // Fallback to original size if map size is unexpected
                    width = "300";
                    height = "300";
                    break;
            }

            // Defender is at the center of the settlement.
            defender_direction = "S"; // Default direction, provides a forward-facing orientation.
            defender_deployment = "<deployment_area>\n" +
                                  $"<centre x =\"{siege_center_x}\" y =\"{siege_center_y}\"/>\n" +
                                  $"<width metres =\"{width}\"/>\n" +
                                  $"<height metres =\"{height}\"/>\n" +
                                  $"<orientation radians =\"{ROTATION_0º}\"/>\n" +
                                  "</deployment_area>\n\n";

            // Attacker gets a random direction from the edges of the map.
            if (BattleState.AutofixAttackerDirectionOverride != null)
            {
                attacker_direction = BattleState.AutofixAttackerDirectionOverride;
                Program.Logger.Debug($"Autofix override: Attacker direction set to '{attacker_direction}'.");
            }
            else
            {
                string[] coords;
                if (besiegerOrientations != null && besiegerOrientations.Any())
                {
                    Program.Logger.Debug($"Using map-defined besieger orientations: [{string.Join(", ", besiegerOrientations)}]");
                    coords = besiegerOrientations.ToArray();
                }
                else
                {
                    Program.Logger.Debug("No map-defined besieger orientations. Using all directions.");
                    coords = new string[] { "N", "S", "E", "W" };
                }
                int index = _random.Next(0, coords.Length);
                attacker_direction = coords[index];
                BattleState.OriginalSiegeAttackerDirection = attacker_direction; // Store the initial direction
                Program.Logger.Debug($"Initial siege attacker direction set to '{attacker_direction}'.");
            }
            
            // Use existing logic to place the attacker at the map edge.
            attacker_deployment = Directions.SetDirection(attacker_direction, total_soldiers);
        }

        public static string? beta_GetDeployment(string combat_side)
        {

            switch(combat_side)
            {
                case "attacker":
                    return attacker_deployment;
                case "defender":
                    return defender_deployment;
            }

            return attacker_deployment;
        }
        public static string beta_GeDirection(string combat_side)
        {
            switch (combat_side)
            {
                case "attacker":
                    return attacker_direction;
                case "defender":
                    return defender_direction;
            }

            return attacker_direction;
        }

        public static string GetOppositeDirection(string direction)
        {
            return Directions.GetOppositeDirection(direction);
        }

        public static string? beta_GetReinforcementDeployment(string direction, int total_soldiers)
        {
            return Directions.SetDirection(direction, total_soldiers);
        }
    }

    class DeploymentArea
    {
        //CENTER POSITIONS
        public string X { get; private set; }
        public string Y { get; private set; }

        //AREA DIAMETER
        public string Width { get; private set; }
        public string Height { get; private set; }

        //MAP SIZE OPTION
        string MapSize { get; set; }

        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }

        public DeploymentArea(string direction, string option_map_size, int total_soldiers)
        {
            // Determine MapSize category ("Medium", "Big", "Huge")
            string map_size_source = BattleState.AutofixDeploymentSizeOverride ?? option_map_size;
            if (map_size_source == "Dynamic")
            {
                if (BattleState.IsSiegeBattle)
                {
                    int holdingLevel = Sieges.GetHoldingLevel();
                    if (holdingLevel <= 2) { MapSize = "Medium"; }
                    else if (holdingLevel <= 4) { MapSize = "Big"; }
                    else { MapSize = "Huge"; }
                }
                else // Field battle
                {
                    if (total_soldiers <= 5000) { MapSize = "Medium"; }
                    else if (total_soldiers > 5000 && total_soldiers < 20000) { MapSize = "Big"; }
                    else if (total_soldiers >= 20000) { MapSize = "Huge"; }
                    else { MapSize = "Medium"; }
                }
            }
            else
            {
                MapSize = map_size_source;
            }

            // Determine playable area boundary
            string map_dimension_str = ModOptions.SetMapSize(total_soldiers, BattleState.IsSiegeBattle);
            float map_dimension = float.Parse(map_dimension_str, CultureInfo.InvariantCulture);
            float playable_boundary = map_dimension / 2f;
            float buffer = 50f;

            // Determine deployment zone dimensions and position
            float centerX, centerY, width, height;

            if (direction == "N" || direction == "S")
            {
                // Horizontal deployment (along top or bottom edge)
                width = (playable_boundary - buffer) * 2f;
                height = GetDeploymentDepth(playable_boundary, buffer);
                centerX = 0f;
                centerY = playable_boundary - buffer - (height / 2f);
                if (direction == "S")
                {
                    centerY = -centerY;
                }
            }
            else // "E" or "W"
            {
                // Vertical deployment (along left or right edge)
                height = (playable_boundary - buffer) * 2f;
                width = GetDeploymentDepth(playable_boundary, buffer);
                centerY = 0f;
                centerX = playable_boundary - buffer - (width / 2f);
                if (direction == "W")
                {
                    centerX = -centerX;
                }
            }

            // Assign final string values
            this.X = centerX.ToString("F2", CultureInfo.InvariantCulture);
            this.Y = centerY.ToString("F2", CultureInfo.InvariantCulture);
            this.Width = width.ToString("F2", CultureInfo.InvariantCulture);
            this.Height = height.ToString("F2", CultureInfo.InvariantCulture);

            // Set Min/Max for unit placement clamping
            this.MinX = centerX - (width / 2f);
            this.MaxX = centerX + (width / 2f);
            this.MinY = centerY - (height / 2f);
            this.MaxY = centerY + (height / 2f);
        }

        private float GetDeploymentDepth(float playable_boundary, float buffer)
        {
            if (BattleState.IsSiegeBattle)
            {
                // For siege attacker, depth is the space between defender zone and map edge.
                float defender_radius = 0;
                // We need to determine the defender's deployment size to calculate the radius.
                string defender_map_size;
                string optionMapSize = BattleState.AutofixDeploymentSizeOverride ?? ModOptions.DeploymentsZones();
                if (optionMapSize == "Dynamic")
                {
                    int holdingLevel = Sieges.GetHoldingLevel();
                    if (holdingLevel <= 2) { defender_map_size = "Medium"; }
                    else if (holdingLevel <= 4) { defender_map_size = "Big"; }
                    else { defender_map_size = "Huge"; }
                }
                else
                {
                    defender_map_size = optionMapSize;
                }

                switch (defender_map_size)
                {
                    case "Medium": defender_radius = 1200f / 2f; break; // 600
                    case "Big": defender_radius = 1300f / 2f; break; // 650
                    case "Huge": defender_radius = 1750f / 2f; break; // 875
                    default: defender_radius = 450f; break; // Fallback
                }
                
                float depth = playable_boundary - defender_radius - buffer;
                return depth < 100f ? 100f : depth; // ensure minimum depth
            }
            else // Field battle
            {
                switch (MapSize)
                {
                    case "Medium": return 200f;
                    case "Big": return 300f;
                    case "Huge": return 400f;
                    default: return 250f;
                }
            }
        }
    }

    class UnitsDeploymentsPosition
    {
        //UNITS DEPLOYMENT AREA DEFAULT POSITION
        public int X {  get; private set; }
        public int Y { get; private set; }

        //DEPLOYMENT AREA DIRECTION
        public string Direction { get; private set; }

        //MAP SIZE USER OPTION
        private string MapSize { get; set; }

        private DeploymentArea _deploymentArea;

        /// <summary>
        /// Dynamic constructor for units positioning
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="option_map_size"></param>
        /// <param name="total_soldiers"></param>
        public UnitsDeploymentsPosition(string direction, string option_map_size, int total_soldiers, bool isReinforcement = false) 
        {
            Direction = direction;
            MapSize = option_map_size;
            _deploymentArea = new DeploymentArea(direction, option_map_size, total_soldiers);

            if (BattleState.IsSiegeBattle)
            {
                // Attacker starts at the edge, as normal.
                if (direction == Deployments.beta_GeDirection("attacker"))
                {
                    BattleMap(option_map_size, total_soldiers);
                    UnitsPositionament();
                }
                // Defender reinforcement (relief army) also starts at the edge.
                else if (isReinforcement)
                {
                    BattleMap(option_map_size, total_soldiers);
                    UnitsPositionament();
                }
                else // Defender (garrison) starts at the center of the settlement.
                {
                    // Use the pre-calculated meter coordinates.
                    float.TryParse(Deployments.GetSiegeCenterX(), NumberStyles.Any, CultureInfo.InvariantCulture, out float x_float);
                    float.TryParse(Deployments.GetSiegeCenterY(), NumberStyles.Any, CultureInfo.InvariantCulture, out float y_float);
                    X = (int)x_float;
                    Y = (int)y_float;
                }
            }
            else // Field battle
            {
                BattleMap(option_map_size, total_soldiers);
                UnitsPositionament();
            }
        }

        /// <summary>
        /// Default values for unit positioning
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>

        public UnitsDeploymentsPosition(int x, int y)
        {
            X = x;
            Y = y;
            Direction = string.Empty; // Initialize Direction
            MapSize = string.Empty;   // Initialize MapSize
            // _deploymentArea is not initialized here, as this constructor is for default values, not dynamic deployment.
            // If this constructor is used for units that need boundary checks, _deploymentArea would need to be passed or initialized differently.
            // For now, assuming this constructor is for fixed positions not requiring dynamic boundary checks.
        }

        public void AddUnitXSpacing(string direction)
        {
            int xSpacing = 15; //old 30
            float newX = this.X; // Use float for calculation

            if (direction is "N" || direction is "S")
            {
                newX -= xSpacing;
            }
            else if (direction is "E")
            {
                xSpacing = 10; //old 15
                newX += xSpacing;
            }
            else if (direction is "W")
            {
                xSpacing = 10; //old 15
                newX -= xSpacing;
            }

            float clampedX = Math.Clamp(newX, _deploymentArea.MinX, _deploymentArea.MaxX);
            if (clampedX != newX)
            {
                Program.Logger.Debug($"WARNING: Unit X coordinate ({newX}) exceeded deployment boundary. Clamped to {clampedX}. Army may be too large for the deployment zone.");
            }
            this.X = (int)clampedX;
        }
        public void AddUnitYSpacing(string direction)
        {
            int ySpacing = 10; //old 15
            float newY = this.Y; // Use float for calculation

            if (direction is "N")
            {
                newY += ySpacing;
            }
            else if (direction is "S")
            {
                newY -= ySpacing;
            }
            else if (direction is "E" || direction is "W")
            {
                ySpacing = 15; //old 30
                newY += ySpacing;
            }

            float clampedY = Math.Clamp(newY, _deploymentArea.MinY, _deploymentArea.MaxY);
            if (clampedY != newY)
            {
                Program.Logger.Debug($"WARNING: Unit Y coordinate ({newY}) exceeded deployment boundary. Clamped to {clampedY}. Army may be too large for the deployment zone.");
            }
            this.Y = (int)clampedY;
        }


        private void BattleMap(string option_map_size, int total_soldiers)
        {
            string map_size_source = BattleState.AutofixDeploymentSizeOverride ?? option_map_size;
            if(map_size_source == "Dynamic")
            {
                if (BattleState.IsSiegeBattle)
                {
                    int holdingLevel = Sieges.GetHoldingLevel();
                    if (holdingLevel <= 2) { MapSize = "Medium"; }
                    else if (holdingLevel <= 4) { MapSize = "Big"; }
                    else { MapSize = "Huge"; }
                }
                else // Field battle
                {
                    if (total_soldiers <= 5000)
                    {
                        MapSize = "Medium";
                    }
                    else if (total_soldiers > 5000 && total_soldiers < 20000)
                    {
                        MapSize = "Big";
                    }
                    else if (total_soldiers >= 20000)
                    {
                        MapSize = "Huge";
                    }
                    else
                    {
                        MapSize = "Medium"; // Default to medium if total_soldiers is outside expected range
                    }
                }
            }
            else
            {
                MapSize = map_size_source;
            }
        }

        public void InverseDirection()
        {
            if (Direction == "N")
            {
                Direction = "S";
            }
            else if (Direction == "S")
            {
                Direction= "N";
            }
            else if (Direction == "E")
            {
                Direction = "W";
            }
            else if (Direction == "W")
            {
                Direction = "E";
            }
        }

        private void UnitsPositionament()
        {
            float centerX = float.Parse(_deploymentArea.X, CultureInfo.InvariantCulture);
            float centerY = float.Parse(_deploymentArea.Y, CultureInfo.InvariantCulture);

            switch (Direction)
            {
                case "N":
                    X = (int)centerX;
                    Y = (int)_deploymentArea.MinY;
                    break;
                case "S":
                    X = (int)centerX;
                    Y = (int)_deploymentArea.MaxY;
                    break;
                case "E":
                    X = (int)_deploymentArea.MinX;
                    Y = (int)centerY;
                    break;
                case "W":
                    X = (int)_deploymentArea.MaxX;
                    Y = (int)centerY;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid direction '{Direction}' encountered during unit positioning.");
            }
        }
    }


}
