namespace Planar.Job
{
    internal class Key : IKey
    {
        public Key()
        {
        }

        public Key(string name, string group)
        {
            Name = name;
            Group = group;
        }

        public string Name { get; set; }
        public string Group { get; set; }
    }
}