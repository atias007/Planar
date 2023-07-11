using Planar.API.Common.Entities;
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

        public CliTable(PagingResponse? paging, string entityName) : this(showCount: true)
        {
            EntityName = entityName;
            Paging = paging;
        }

        public CliTable(PagingResponse? paging) : this(showCount: true)
        {
            Paging = paging;
        }

        public Table Table { get; }

        public bool ShowCount { get; }

        public string? EntityName { get; }

        public string? Title { get; set; }

        public PagingResponse? Paging { get; }
    }
}