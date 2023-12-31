using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public abstract class BaseReport
    {
        private readonly IServiceScopeFactory _serviceScope;

        protected BaseReport(IServiceScopeFactory serviceScope)
        {
            _serviceScope = serviceScope;
        }

        public string EmptyTableHtml => GetResource(null, "empty_table");

        public IServiceScopeFactory ServiceScope => _serviceScope;

        public abstract string ReportName { get; }

        public abstract Task<string> Generate(DateScope dateScope);

        private static string GetResource(string? reportName, string key)
        {
            var resourceName =
                string.IsNullOrWhiteSpace(reportName) ?
                $"{nameof(Planar)}.{nameof(Service)}.HtmlTemplates.{key}.html" :
                $"{nameof(Planar)}.{nameof(Service)}.HtmlTemplates.{reportName}Report.{key}.html";

            var assembly = typeof(BaseReport).Assembly ??
                throw new InvalidOperationException("Assembly is null");
            using var stream = assembly.GetManifestResourceStream(resourceName) ??
                throw new InvalidOperationException($"Resource '{resourceName}' not found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        protected string GetResource(string key)
        {
            return GetResource(ReportName, key);
        }

        protected string GetMainTemplate()
        {
            var main = GetResource("main");
            var header = GetResource(null, "header");
            var footer = GetResource(null, "footer");
            var style = GetResource(null, "style");
            var head = GetResource(null, "head");

            head = ReplacePlaceHolder(head, "Title", ReportName);
            main = ReplacePlaceHolder(main, "Head", head);
            main = ReplacePlaceHolder(main, "Style", style);
            main = ReplacePlaceHolder(main, "Header", header);
            main = ReplacePlaceHolder(main, "Footer", footer);
            return main;
        }

        protected static string ReplacePlaceHolder(string template, string placeHolder, string? value)
        {
            var find = $"<!-- {{{{{placeHolder}}}}} -->";
            return template.Replace(find, value);
        }
    }
}