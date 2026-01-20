using System;
using System.Collections.Generic;
using System.Linq;

public class Combat
{
    public List<CombatSide> Sides { get; set; } = new List<CombatSide>();
}

public class CombatSide
{
    public string Side { get; set; }
    public List<int> Armies { get; set; } = new List<int>();
}

public class Army
{
    public int Id { get; set; }
    public List<ArmyRegiment> Regiments { get; set; } = new List<ArmyRegiment>();
}

public class UnitCasualitiesReport
{
    public string Side { get; set; }
    public List<RegimentCasualitiesReport> Regiments { get; set; } = new List<RegimentCasualitiesReport>();
}

public class RegimentCasualitiesReport
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int Starting { get; set; }
    public int Remaining { get; set; }
    public int Losses => Starting - Remaining;
}

public class CasualtyProcessor
{
    private void ChangeRegimentsSoldiers(Army army, UnitCasualitiesReport report)
    {
        foreach (var regiment in army.Regiments)
        {
            var regimentLosses = report.Regiments.FirstOrDefault(r => r.Id == regiment.Id);

            if (regimentLosses != null)
            {
                if (regiment.Type == RegimentType.Levy)
                {
                    // Handle levy regiments directly
                    var lossesRatio = (double)regimentLosses.Losses / regiment.StartingNum;
                    regiment.CurrentNum = Math.Max(0, (int)Math.Round(regiment.CurrentNum - (regiment.CurrentNum * lossesRatio)));
                }
                else
                {
                    // Handle normal regiments with chunks
                    var lossesRatio = (double)regimentLosses.Losses / regiment.StartingNum;
                    var survivors = regiment.Regiments.Sum(r => r.CurrentNum) - (int)Math.Round(regiment.Regiments.Sum(r => r.CurrentNum) * lossesRatio);
                    
                    // Distribute survivors among sub-regiments
                    var totalCurrent = regiment.Regiments.Sum(r => r.CurrentNum);
                    if (totalCurrent > 0)
                    {
                        foreach (var subRegiment in regiment.Regiments)
                        {
                            var subRegimentRatio = (double)subRegiment.CurrentNum / totalCurrent;
                            subRegiment.CurrentNum = Math.Max(0, (int)Math.Round(subRegiment.CurrentNum - (subRegiment.CurrentNum * lossesRatio)));
                        }
                    }
                }
            }
        }
    }

    private List<UnitCasualitiesReport> CreateUnitsReports(List<Army> armies, Combat combat)
    {
        var reports = new List<UnitCasualitiesReport>();

        foreach (var side in combat.Sides)
        {
            var report = new UnitCasualitiesReport();
            report.Side = side.Side;

            foreach (var army in armies.Where(a => side.Armies.Contains(a.Id)))
            {
                foreach (var regiment in army.Regiments)
                {
                    var unitReport = new RegimentCasualitiesReport();
                    unitReport.Id = regiment.Id;
                    unitReport.Name = regiment.Name;
                    unitReport.Type = regiment.Type.ToString();

                    if (regiment.Type == RegimentType.Levy)
                    {
                        unitReport.Starting = regiment.StartingNum;
                        unitReport.Remaining = regiment.CurrentNum;
                    }
                    else
                    {
                        unitReport.Starting = regiment.Regiments.Sum(r => r.MaxNum);
                        unitReport.Remaining = regiment.Regiments.Sum(r => r.CurrentNum);
                    }

                    report.Regiments.Add(unitReport);
                }
            }

            reports.Add(report);
        }

        return reports;
    }
}
