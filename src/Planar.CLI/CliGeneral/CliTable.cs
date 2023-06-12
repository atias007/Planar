using Spectre.Console;

namespace Planar.CLI.CliGeneral
{
    public class CliTable
    {
        public CliTable()
        {
            Table = new Table();
        }

        public CliTable(Table table)
        {
            Table = table;
        }

        public CliTable(bool showCount) : this()
        {
            ShowCount = showCount;
        }

        public CliTable(bool showCount, string entityName) : this(showCount)
        {
            EntityName = entityName;
        }

        public Table Table { get; }

        public bool ShowCount { get; }

        public string? EntityName { get; }
    }
}