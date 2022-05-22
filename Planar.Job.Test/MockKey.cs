namespace Planar.Job.Test
{
    public class MockKey : IKey
    {
        public string Name { get; private set; } = "UnitTest";
        public string Group { get; private set; } = "Default";
    }
}