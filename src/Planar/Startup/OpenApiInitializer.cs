using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using Scalar.AspNetCore;

namespace Planar.Startup;

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
        if (!AppSettings.General.OpenApiUI) { return; }
        if (app.Environment.IsProduction()) { return; }

        app.MapOpenApi();
        app.MapScalarApiReference("/api/ui", (opt, ctx) =>
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
    }
}