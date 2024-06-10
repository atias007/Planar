using MimeKit.Text;
using Planar.Common;
using Planar.Common.Resources;
using System.Data;
using System.Text;

namespace SqlTableReportJob
{
    internal static class ReportGenerator
    {
        public static string Generate(DataTable dataTable, string? title)
        {
            var main = GetResource("report_template");
            var header = ResourceManager.GetResource(ResourceMembers.Header);
            var footer = ResourceManager.GetResource(ResourceMembers.Footer);
            var style = ResourceManager.GetResource(ResourceMembers.Style);
            var head = ResourceManager.GetResource(ResourceMembers.Head);
            var htmlTable = GetHtmlTable(dataTable);

            head = ReplacePlaceHolder(head, "Title", title);
            header = HtmlUtil.SetLogo(header);
            main = ReplacePlaceHolder(main, "Head", head);
            main = ReplacePlaceHolder(main, "Style", style);
            main = ReplacePlaceHolder(main, "Header", header);
            main = ReplacePlaceHolder(main, "Footer", footer);
            main = ReplacePlaceHolder(main, "Table", htmlTable);
            return main;
        }

        private static string GetHtmlTable(DataTable table)
        {
            if (table.Rows.Count == 0) { return ResourceManager.GetResource(ResourceMembers.EmptyTable); }

            var tableTemplate = GetResource("report_table");
            var th = """
                <td class="tableheader"><div class="tableheaderdiv"><!-- {{Data}} --></div></td>
                """;
            var tr = """
                <td class="tablerow"><div class="tablediv"><!-- {{Data}} --></div></td>
                """;

            var sb = new StringBuilder();
            sb.AppendLine("<tr>");
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var data1 = ReplacePlaceHolder(th, "Data", table.Columns[i].ColumnName);
                sb.AppendLine(data1);
            }
            sb.AppendLine("</tr>");
            tableTemplate = ReplacePlaceHolder(tableTemplate, "TableHeader", sb.ToString());

            sb = new StringBuilder();
            foreach (DataRow r in table.Rows)
            {
                sb.AppendLine("<tr>");
                foreach (DataColumn c in table.Columns)
                {
                    var data2 = ReplacePlaceHolder(tr, "Data", Convert.ToString(r[c]));
                    sb.AppendLine(data2);
                }
                sb.AppendLine("</tr>");
            }

            tableTemplate = ReplacePlaceHolder(tableTemplate, "TableRows", sb.ToString());
            return tableTemplate;
        }

        private static string GetResource(string key)
        {
            var resourceName = $"{nameof(SqlTableReportJob)}.{key}.html";
            var assembly = typeof(ReportGenerator).Assembly ??
               throw new InvalidOperationException("Assembly is null");
            using var stream = assembly.GetManifestResourceStream(resourceName) ??
                throw new InvalidOperationException($"Resource '{resourceName}' not found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        private static string ReplacePlaceHolder(string template, string placeHolder, string? value, bool encode = false)
        {
            var find = $"<!-- {{{{{placeHolder}}}}} -->";

            var encodeValue =
                encode ?
                HtmlUtils.HtmlEncode(value) :
                value;

            return template.Replace(find, encodeValue);
        }
    }
}