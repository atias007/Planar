using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Planar.Startup;

public static class ContentInitializer
{
    private struct Content
    {
        static Content()
        {
            NoAccessImage = new Lazy<IResult>(() => Results.File(GetBinaryContent("no_access.jpg"), "image/jpeg"));
            PlanarLogo = new Lazy<IResult>(() => Results.File(GetBinaryContent("logo2.png"), "image/png"));
            EmailLogo = new Lazy<IResult>(() => Results.File(GetBinaryContent("logo3.png"), "image/png"));
        }

        public static void Initialize()
        {
            var logo = GetBinaryContent("logo3.png");
            HtmlUtil.Logo175Content = Convert.ToBase64String(logo);
        }

        public static Lazy<IResult> NoAccessImage { get; set; }

        public static Lazy<IResult> PlanarLogo { get; set; }

        public static Lazy<IResult> EmailLogo { get; set; }

        private static byte[] GetBinaryContent(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Planar.Content.{name}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new MemoryStream();
            stream.CopyTo(reader);

            return reader.ToArray();
        }
    }

    public static void MapContent(WebApplication app)
    {
        Content.Initialize();

        var isProduction = app.Environment.IsProduction();
        if (!isProduction && AppSettings.General.OpenApiUI)
        {
            app.MapGet("/", () => Results.Redirect("/api/ui")).ExcludeFromDescription();
            app.MapGet("/content/logo.png", () => Content.PlanarLogo.Value).ExcludeFromDescription();
            app.MapGet("/content/email-logo.png", () => Content.EmailLogo.Value).ExcludeFromDescription();
        }
        else
        {
            app.MapGet("/", () => Content.NoAccessImage.Value).ExcludeFromDescription();
        }
    }
}