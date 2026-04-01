using System.Collections.Generic;

namespace CrusaderWars.client.LinuxSetup.Models
{
    public class LinuxSetupProgress
    {
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string? CurrentMessage { get; set; }
        public bool IsComplete { get; set; }
        public List<string> LogMessages { get; set; } = new List<string>();
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
