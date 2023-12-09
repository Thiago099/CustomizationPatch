namespace Domain
{
    public class Patch
    {
        public string ModName { get; set; }
        public List<Form> Forms { get; set; }
        public string Description { get; set; }
    }
    public abstract class Form
    {
        public string RefID { get; set; }
    }
    public class Item : Form
    {
        public string Name { get; set; }
        public uint GoldValue { get; set; }
        public float Weight { get; set; }
    }
    public class Spell : Form
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Effect> Effects { get; set; }
    }
    public class Effect
    {
        public MagicEffect Base { get; set; }
        public float Magnitude { get; set; }
        public int Area { get; set; }
        public int Duration { get; set; }
    }
    public class MagicEffect : Form
    {

    }
}
