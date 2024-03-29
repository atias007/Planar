using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using Planar.Hooks;
using Planar.Hooks.EmailTemplates;
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
            var htmlReplace = new Dictionary<string, object>
            {
                { "@@OpenApiVersion@@", SwaggerInitializer.SwaggerVersion }
            };

            var logo = GetBinaryContent("logo3.png");
            EmailHtmlGenerator.Logo175Content = Convert.ToBase64String(logo);

            OpenApiHtml = new Lazy<IResult>(() => Results.Content(GetContent("planar_openapi.html", htmlReplace), "text/html"));
            OpenApiJavaScript = new Lazy<IResult>(() => Results.Content(GetContent("redoc.standalone.js"), "application/javascript"));
            OpenApiCss = new Lazy<IResult>(() => Results.Content(GetContent("fonts.googleapis.css"), "text/css"));
            SwaggerCss = new Lazy<IResult>(() => Results.Content(GetContent("theme-flattop.css"), "text/css"));
            NoAccessImage = new Lazy<IResult>(() => Results.File(GetBinaryContent("no_access.jpg"), "image/jpeg"));
            PlanarLogo = new Lazy<IResult>(() => Results.File(GetBinaryContent("logo2.png"), "image/png"));
            EmailLogo = new Lazy<IResult>(() => Results.File(GetBinaryContent("logo3.png"), "image/png"));
        }

        // Generate a function to add two numbers

        public static Lazy<IResult> OpenApiHtml { get; set; }

        public static Lazy<IResult> OpenApiJavaScript { get; set; }

        public static Lazy<IResult> NoAccessImage { get; set; }

        public static Lazy<IResult> PlanarLogo { get; set; }

        public static Lazy<IResult> EmailLogo { get; set; }

        public static Lazy<IResult> SwaggerCss { get; set; }

        public static Lazy<IResult> OpenApiCss { get; set; }

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
        var isProduction = app.Environment.IsProduction();
        if (!isProduction && AppSettings.General.OpenApiUI)
        {
            app.MapGet("/", () => Content.OpenApiHtml.Value);
            app.MapGet("/content/redoc.standalone.js", () => Content.OpenApiJavaScript.Value);
            app.MapGet("/content/fonts.googleapis.css", () => Content.OpenApiCss.Value);
            app.MapGet("/content/theme-flattop.css", () => Content.SwaggerCss.Value);
            app.MapGet("/content/logo.png", () => Content.PlanarLogo.Value);
            app.MapGet("/content/email-logo.png", () => Content.EmailLogo.Value);
        }
        else
        {
            app.MapGet("/", () => Content.NoAccessImage.Value);
        }

        if (!AppSettings.General.SwaggerUI)
        {
            app.MapGet("/swagger", () => Content.NoAccessImage.Value);
        }
    }

    private static string GetContent(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Planar.Content.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        string result = reader.ReadToEnd();
        return result;
    }

    private static string GetContent(string name, Dictionary<string, object> replace)
    {
        var value = GetContent(name);
        if (replace == null) { return value; }

        foreach (var item in replace)
        {
            value = value.Replace(item.Key, Convert.ToString(item.Value));
        }

        return value;
    }
}