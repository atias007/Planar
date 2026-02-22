using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Planar.Common;
using Scalar.AspNetCore;
using System;

namespace Planar.Startup
{
    public static class OpenApiInitializer
    {
        private static string _version;

        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var version = Global.Version;
                    _version = $"v{version.Major}.{version.Minor}.{version.Build}";
                }

                return _version;
            }
        }

        public static void SetOpenApi(WebApplication app)
        {
            if (!AppSettings.General.SwaggerUI) { return; }
            if (app.Environment.IsProduction()) { return; }

            app.MapOpenApi();
            app.MapScalarApiReference("/api/document", (opt, ctx) =>
            {
                opt
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.AsyncHttp)
                    .WithJsonDocumentDownload();

                if (app.Environment.IsDevelopment())
                {
                    opt.ShowDeveloperTools = DeveloperToolsVisibility.Always;
                }
                else
                {
                    opt.ShowDeveloperTools = DeveloperToolsVisibility.Never;
                }

                opt.WithTitle($"{nameof(Planar)} (Version: {Version})");
            });
            ////app.UseSwagger();
            ////app.UseSwaggerUI(c =>
            ////{
            ////    c.SwaggerEndpoint($"/swagger/{SwaggerInitializer.SwaggerVersion}/swagger.json", "Planar");
            ////    c.InjectStylesheet("/Content/theme-flattop.css");
            ////});
        }

        private static OpenApiInfo GetOpenApiInfo()
        {
            const string lic = "https://opensource.org/licenses/MIT";
            const string schema = "http";
            const string planarSite = "www.planar.me";
            const string email = "admin@planar.me";
            const string name = "Tsahi Atias";
            const string license = "MIT";
            const string description = "Enterprise schedule system API";
            var result = new OpenApiInfo
            {
                Title = nameof(Planar),
                Version = Version,
                Contact = new OpenApiContact
                {
                    Email = email,
                    Name = name,
                    Url = new Uri($"{schema}://{planarSite}")
                },
                License = new OpenApiLicense
                {
                    Name = license,
                    Url = new Uri(lic)
                },
                Description = description
            };

            return result;
        }
    }
}