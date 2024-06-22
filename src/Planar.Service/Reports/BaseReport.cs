using Microsoft.Extensions.DependencyInjection;
using MimeKit.Text;
using Planar.Common;
using Planar.Common.Resources;
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

        public static string EmptyTableHtml => ResourceManager.GetResource(ResourceMembers.EmptyTable);

        public IServiceScopeFactory ServiceScope => _serviceScope;

        public abstract string ReportName { get; }

        public abstract Task<string> Generate(DateScope dateScope);

        protected static string GetCounterText(int counter)
        {
            if (counter == 0) { return "-"; }
            return counter.ToString("N0");
        }

        private static string GetResourceByName(string resourceName)
        {
            var assembly = typeof(BaseReport).Assembly ??
               throw new InvalidOperationException("Assembly is null");
            using var stream = assembly.GetManifestResourceStream(resourceName) ??
                throw new InvalidOperationException($"Resource '{resourceName}' not found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        private static string GetResource(string? reportName, string key)
        {
            var resourceName =
                string.IsNullOrWhiteSpace(reportName) ?
                $"{nameof(Planar)}.{nameof(Service)}.HtmlTemplates.{key}.html" :
                $"{nameof(Planar)}.{nameof(Service)}.HtmlTemplates.{reportName}Report.{key}.html";

            return GetResourceByName(resourceName);
        }

        protected string GetResource(string key)
        {
            return GetResource(ReportName, key);
        }

        protected string GetMainTemplate()
        {
            var main = GetResource("main");
            var header = ResourceManager.GetResource(ResourceMembers.Header);
            var footer = ResourceManager.GetResource(ResourceMembers.Footer);
            var style = ResourceManager.GetResource(ResourceMembers.Style);
            var head = ResourceManager.GetResource(ResourceMembers.Head);

            head = ReplacePlaceHolder(head, "Title", ReportName);
            header = HtmlUtil.SetLogo(header);
            main = ReplacePlaceHolder(main, "Head", head);
            main = ReplacePlaceHolder(main, "Style", style);
            main = ReplacePlaceHolder(main, "Header", header);
            main = ReplacePlaceHolder(main, "Footer", footer);
            return main;
        }

        protected static string ReplaceEnvironmentPlaceHolder(string template)
        {
            return ReplacePlaceHolder(template, "Environment", AppSettings.General.Environment);
        }

        protected static string ReplacePlaceHolder(string template, string placeHolder, string? value, bool encode = false)
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