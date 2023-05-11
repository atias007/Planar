namespace Planar.API.Common.Entities
{
    public class AvailableJobToAdd
    {
        public AvailableJobToAdd(string name, string relativeFolder)
        {
            Name = name;
            RelativeFolder = relativeFolder;
        }

        public string Name { get; private set; }
        public string RelativeFolder { get; private set; }
    }
}