using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Planar.Service;
using System;
using System.IO;
using System.Reflection;

namespace Planar.Startup
{
    public static class ContentInitializer
    {
        private struct Content
        {
            static Content()
            {
                OpenApiHtml = new Lazy<IResult>(() => Results.Content(GetContent("planar_openapi.html"), "text/html"));
                OpenApiJavaScript = new Lazy<IResult>(() => Results.Content(GetContent("redoc.standalone.js"), "application/javascript"));
                OpenApiJson = new Lazy<IResult>(() => Results.Content(GetContent("planar_openapi.json"), "application/json"));
                OpenApiCss = new Lazy<IResult>(() => Results.Content(GetContent("fonts.googleapis.css"), "text/css"));
                NoAccessImage = new Lazy<IResult>(() => Results.File(GetBinaryContent("no_access.jpg"), "image/jpeg"));
            }

            public static Lazy<IResult> OpenApiHtml { get; set; }

            public static Lazy<IResult> OpenApiJson { get; set; }

            public static Lazy<IResult> OpenApiJavaScript { get; set; }

            public static Lazy<IResult> NoAccessImage { get; set; }

            public static Lazy<IResult> OpenApiCss { get; set; }
        }

        public static void MapContent(WebApplication app)
        {
            if (AppSettings.OpenApiUI)
            {
                app.MapGet("/", () => Content.OpenApiHtml.Value);
                app.MapGet("/content/openapi.spec.json", () => Content.OpenApiJson.Value);
                app.MapGet("/content/redoc.standalone.js", () => Content.OpenApiJavaScript.Value);
                app.MapGet("/content/fonts.googleapis.css", () => Content.OpenApiCss.Value);
            }
            else
            {
                app.MapGet("/", () => Content.NoAccessImage.Value);
            }

            if (!AppSettings.SwaggerUI)
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
}