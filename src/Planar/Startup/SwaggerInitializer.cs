using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Planar.Startup
{
    public static class SwaggerInitializer
    {
        public static void InitializeSwagger(SwaggerGenOptions options)
        {
            var info = GetOpenApiInfo();
            options.SwaggerDoc("v1", info);
            options.EnableAnnotations();
        }

        private static OpenApiInfo GetOpenApiInfo()
        {
            var result = new OpenApiInfo
            {
                Title = "Planar",
                Version = "v1",
                Contact = new OpenApiContact
                {
                    Email = "admin@planar.me",
                    Name = "Tsahi Atias",
                    Url = new Uri("http://www.planar.me")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                Description = "Enterprise schedule system API"
            };

            return result;
        }
    }
}