namespace Planar.CLI.CliGeneral
{
    public class CliDumpObject
    {
        public CliDumpObject(object? obj)
        {
            Object = obj;
        }

        public string? Title { get; set; }

        public object? Object { get; private set; }
    }
}