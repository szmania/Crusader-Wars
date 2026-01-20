using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum RegimentType
{
    Levy,
    HeavyInfantry,
    LightInfantry,
    HeavyCavalry,
    LightCavalry,
    Archer,
    General
}

public class Regiment
{
    public RegimentType Type { get; set; }
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int CurrentNum { get; set; }
    public int MaxNum { get; set; }
}

public class ArmyRegiment
{
    public RegimentType Type { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public int StartingNum { get; set; }
    public int CurrentNum { get; set; }
    public int MaxNum { get; set; }
    public int TemplateId { get; set; }
    public List<Regiment> Regiments { get; set; } = new List<Regiment>();
}

public class ArmiesReader
{
    public List<ArmyRegiment> ReadArmyRegiments(string content)
    {
        var regiments = new List<ArmyRegiment>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        ArmyRegiment armyRegiment = null;
        bool isReadingChunks = false;
        int current = 0, max = 0;
        int regimentId = 0;
        int templateId = 0;
        RegimentType regimentType = RegimentType.Levy;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("army_regiment="))
            {
                armyRegiment = new ArmyRegiment();
                isReadingChunks = false;
                continue;
            }
            
            if (trimmedLine.StartsWith("}"))
            {
                if (isReadingChunks && armyRegiment != null)
                {
                    // This is for normal regiments that have chunks
                    var regiment = new Regiment();
                    regiment.Type = regimentType;
                    regiment.Id = regimentId;
                    regiment.TemplateId = templateId;
                    regiment.CurrentNum = current;
                    regiment.MaxNum = max;

                    armyRegiment.Regiments.Add(regiment);
                    isReadingChunks = false;
                }
                else if (armyRegiment != null && armyRegiment.Type == RegimentType.Levy)
                {
                    // For levies, store current/max at the armyRegiment level
                    armyRegiment.CurrentNum = current;
                    armyRegiment.MaxNum = max;
                }
                
                if (armyRegiment != null)
                {
                    regiments.Add(armyRegiment);
                }
                continue;
            }
            
            if (armyRegiment != null)
            {
                if (trimmedLine.StartsWith("id="))
                {
                    if (int.TryParse(trimmedLine.Substring(3), out int id))
                    {
                        if (isReadingChunks)
                        {
                            regimentId = id;
                        }
                        else
                        {
                            armyRegiment.Id = id;
                        }
                    }
                }
                else if (trimmedLine.StartsWith("template="))
                {
                    if (int.TryParse(trimmedLine.Substring(9), out int template))
                    {
                        templateId = template;
                        if (!isReadingChunks)
                        {
                            armyRegiment.TemplateId = template;
                        }
                    }
                }
                else if (trimmedLine.StartsWith("type="))
                {
                    var typeValue = trimmedLine.Substring(5).Trim('"');
                    if (Enum.TryParse(typeValue, true, out RegimentType type))
                    {
                        regimentType = type;
                        if (!isReadingChunks)
                        {
                            armyRegiment.Type = type;
                        }
                    }
                }
                else if (trimmedLine.StartsWith("current="))
                {
                    if (int.TryParse(trimmedLine.Substring(8), out int curr))
                    {
                        current = curr;
                    }
                }
                else if (trimmedLine.StartsWith("max="))
                {
                    if (int.TryParse(trimmedLine.Substring(4), out int mx))
                    {
                        max = mx;
                    }
                }
                else if (trimmedLine.StartsWith("chunks="))
                {
                    isReadingChunks = true;
                }
            }
        }
        
        return regiments;
    }
}
