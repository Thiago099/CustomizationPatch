using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class PageUserData
    {
        public string? ChosenPreset { get; set; }
        public Dictionary<string, Dictionary<string, string>> ChosenData { get; set; } 
    }
}
