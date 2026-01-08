using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public class BattleState
{
    public string Attacker { get; set; }
    public string Defender { get; set; }
    public string BattleTerrain { get; set; }
    public int BattleTerrainRotation { get; set; }
    public Vector2 BattleTerrainPosition { get; set; }
    public float BattleTerrainScale { get; set; }
    public Dictionary<string, string> BattleModifiers { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> UnitReplacements { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> DeploymentZones { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> ManualUnitReplacements { get; set; } = new Dictionary<string, string>();
    public string DeploymentZoneOverrideAttacker { get; set; }
    public string DeploymentZoneOverrideDefender { get; set; }

    public void ClearManualUnitReplacements()
    {
        ManualUnitReplacements.Clear();
        SavePersistentBattleSettings();
    }

    public void ClearDeploymentZoneOverrides()
    {
        DeploymentZoneOverrideAttacker = null;
        DeploymentZoneOverrideDefender = null;
        SavePersistentBattleSettings();
    }

    public void ClearBattleState()
    {
        // Clear all battle state data except persistent settings
        Attacker = null;
        Defender = null;
        BattleTerrain = null;
        BattleTerrainRotation = 0;
        BattleTerrainPosition = Vector2.Zero;
        BattleTerrainScale = 1.0f;
        BattleModifiers.Clear();
        UnitReplacements.Clear();
        DeploymentZones.Clear();
    }

    public void SavePersistentBattleSettings()
    {
        // Implementation for saving persistent settings
        // This would typically serialize ManualUnitReplacements, DeploymentZoneOverrideAttacker, and DeploymentZoneOverrideDefender to a file
    }

    public void LoadPersistentBattleSettings()
    {
        // Implementation for loading persistent settings
        // This would typically deserialize ManualUnitReplacements, DeploymentZoneOverrideAttacker, and DeploymentZoneOverrideDefender from a file
    }
}
