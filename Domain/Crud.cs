using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Domain
{
    public class Page
    {
        public string Name { get; set; }
        public string Id { get => Regex.Replace(Name, @"\s+", ""); }
        public string? Image { get; set; }
        public double? Priority { get; set; }
        public List<Group> Groups { get; set; }
        public Dictionary<string, string> Files { get; set; }
        public string DefaultPreset { get; set; }
        public List<Preset> Presets { get; set; }

        public bool Visible { get; set; } = false;
    }
    public class Group
    {
        public string Name { get; set; }
        public string Id { get => Regex.Replace(Name, @"\s+", ""); }
        public string? Image { get; set; }
        public int? Size { get; set; }
        public List<GroupItem> Items {  get; set; }
    }
    public class GroupItem
    {
        public string Type { get; set; }
        public string Id { get => Regex.Replace(Name, @"\s+", ""); }
        public string Name { get; set; }
        public int? Size { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool Advanced { get; set; } = false;
        public string Value { get; set; }
        public string Target { get; set; }
    }
    public class Preset
    {
        public string Name { set; get; }
        public string Id { get => Regex.Replace(Name, @"\s+", ""); }
        public string Group { get; set; } = "";
        public Dictionary<string, Dictionary<string, object>> Data { get; set; }
    }
}
