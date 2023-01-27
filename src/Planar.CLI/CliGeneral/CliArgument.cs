namespace Planar.CLI
{
    public class CliArgument
    {
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        public override string ToString()
        {
            return $"{Key}={Value}";
        }
    }
}