using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Planar.Service;
using System.IO;
using System.Net.Mime;
using System.Reflection;

namespace Planar.Startup
{
    public static class ContentInitializer
    {
        private static Content _content = GetAllContent();

        private struct Content
        {
            public string Html { get; set; }

            public string Json { get; set; }

            public string JavaScript { get; set; }

            public byte[] NoAccessImage { get; set; }
        }

        public static void MapContent(WebApplication app)
        {
            if (AppSettings.OpenApiUI)
            {
                app.MapGet("/", () => Results.Content(_content.Html, "text/html"));
                app.MapGet("/content/openapi.spec.json", () => Results.Content(_content.Json, "application/json"));
                app.MapGet("/content/redoc.standalone.js", () => Results.Content(_content.JavaScript, "application/javascript"));
            }
            else
            {
                app.MapGet("/", () => Results.File(_content.NoAccessImage, "image/jpeg"));
            }

            if (AppSettings.SwaggerUI == false)
            {
                app.MapGet("/swagger", () => Results.File(_content.NoAccessImage, "image/jpeg"));
            }
        }

        private static Content GetAllContent()
        {
            var content = new Content
            {
                Html = GetContent("planar_openapi.html"),
                JavaScript = GetContent("redoc.standalone.js"),
                Json = GetContent("planar_openapi.json"),
                NoAccessImage = GetBinaryContent("no_access.jpg")
            };

            return content;
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