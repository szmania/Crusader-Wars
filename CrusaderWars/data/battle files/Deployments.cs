using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.client;
using CrusaderWars.data.save_file;
using CrusaderWars.twbattle;

namespace CrusaderWars.terrain
{
    public static class Deployments
    {
        static string AttackerDirection { get; set; } = "N";
        static string DefenderDirection { get; set; } = "S";

        public static void beta_SetSidesDirections(int total_soldiers, (string X, string Y, string[] attPositions, string[] defPositions) battleMap, bool rotateDeployment)
        {
            // Default directions
            AttackerDirection = "N";
            DefenderDirection = "S";

            if (rotateDeployment)
            {
                // Example: Rotate 90 degrees if conditions met
                // This is a placeholder for actual rotation logic based on commander traits or terrain
                AttackerDirection = "E";
                DefenderDirection = "W";
                Program.Logger.Debug($"Deployment rotated. Attacker: {AttackerDirection}, Defender: {DefenderDirection}");
            }
            else
            {
                Program.Logger.Debug($"Deployment not rotated. Attacker: {AttackerDirection}, Defender: {DefenderDirection}");
            }
        }

        public static void beta_SetSiegeDeployment((string X, string Y, string[] attPositions, string[] defPositions) battleMap, int total_soldiers)
        {
            // In a siege, the attacker typically comes from outside, and the defender is inside.
            // For simplicity, let's assume attacker comes from North, defender is in the South (inside settlement).
            AttackerDirection = "N"; // Besiegers outside
            DefenderDirection = "S"; // Garrison inside
            Program.Logger.Debug($"Siege deployment set. Attacker: {AttackerDirection}, Defender: {DefenderDirection}");
        }

        public static string beta_GetDeployment(string combat_side)
        {
            string deploymentArea = "";
            if (combat_side == "attacker")
            {
                deploymentArea = $"<deployment_area direction=\"{AttackerDirection}\"/>\n";
            }
            else if (combat_side == "defender")
            {
                deploymentArea = $"<deployment_area direction=\"{DefenderDirection}\"/>\n";
            }
            return deploymentArea;
        }

        public static string beta_GeDirection(string combat_side)
        {
            if (combat_side == "attacker")
            {
                return AttackerDirection;
            }
            else if (combat_side == "defender")
            {
                return DefenderDirection;
            }
            return "N"; // Default fallback
        }
    }

    public class UnitsDeploymentsPosition
    {
        public string Direction { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }

        private float _initialX;
        private float _initialY;
        private float _currentX;
        private float _currentY;

        private float _horizontalSpacing = 25.0f; // Spacing between units horizontally
        private float _verticalSpacing = 25.0f;   // Spacing between rows of units

        private int _unitsInRow = 0;
        private int _maxUnitsInRow;

        public UnitsDeploymentsPosition(string direction, ModOptions.DeploymentsZones deploymentZone, int total_soldiers)
        {
            Direction = direction;
            SetInitialPositions(deploymentZone, total_soldiers);
            _currentX = _initialX;
            _currentY = _initialY;
            X = _initialX;
            Y = _initialY;
            _maxUnitsInRow = CalculateMaxUnitsInRow(deploymentZone, total_soldiers);
        }

        private void SetInitialPositions(ModOptions.DeploymentsZones deploymentZone, int total_soldiers)
        {
            // These are placeholder values. Real values would depend on the actual map and deployment zones.
            // For now, let's assume a generic map center (0,0) and adjust based on direction.
            // The actual map coordinates in Attila are typically large, e.g., -1000 to 1000.
            // These values are relative to the deployment zone.

            float baseOffset = 100.0f; // Base offset from the "edge" of the deployment zone
            float zoneWidth = 500.0f;  // Approximate width of a deployment zone
            float zoneHeight = 300.0f; // Approximate height of a deployment zone

            switch (Direction)
            {
                case "N": // Attacker from North, deploy towards South
                    _initialX = 0.0f;
                    _initialY = -baseOffset;
                    break;
                case "S": // Defender from South, deploy towards North
                    _initialX = 0.0f;
                    _initialY = baseOffset;
                    break;
                case "E": // Attacker from East, deploy towards West
                    _initialX = baseOffset;
                    _initialY = 0.0f;
                    break;
                case "W": // Defender from West, deploy towards East
                    _initialX = -baseOffset;
                    _initialY = 0.0f;
                    break;
                default:
                    _initialX = 0.0f;
                    _initialY = 0.0f;
                    break;
            }

            // Adjust initial X to center the first row, assuming units spread out from center
            _initialX -= (_maxUnitsInRow / 2.0f) * _horizontalSpacing; 
            X = _initialX;
            Y = _initialY;
        }

        private int CalculateMaxUnitsInRow(ModOptions.DeploymentsZones deploymentZone, int total_soldiers)
        {
            // This is a very simplified calculation. In a real scenario, this would be more complex,
            // potentially considering map size, unit types, and total army size.
            // For now, let's just return a fixed number or a number based on total soldiers.
            if (total_soldiers > 5000) return 10;
            if (total_soldiers > 2000) return 7;
            return 5;
        }

        public void AddUnitXSpacing(string direction)
        {
            _unitsInRow++;
            if (_unitsInRow >= _maxUnitsInRow)
            {
                // Reset for next row
                _currentX = _initialX;
                _unitsInRow = 0;
            }
            else
            {
                _currentX += _horizontalSpacing;
            }
            X = _currentX;
        }

        public void AddUnitYSpacing(string direction)
        {
            // This is called when a new row starts (after AddUnitXSpacing resets _currentX)
            // or when a new unit is added after a full row.
            switch (direction)
            {
                case "N": // Moving South
                    _currentY += _verticalSpacing;
                    break;
                case "S": // Moving North
                    _currentY -= _verticalSpacing;
                    break;
                case "E": // Moving West
                    _currentX -= _verticalSpacing; // For E/W, vertical spacing means moving along X-axis
                    break;
                case "W": // Moving East
                    _currentX += _verticalSpacing; // For E/W, vertical spacing means moving along X-axis
                    break;
            }
            Y = _currentY;
        }
    }
}
