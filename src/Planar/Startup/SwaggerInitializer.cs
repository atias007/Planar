using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Planar.Common;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Planar.Startup
{
    public static class SwaggerInitializer
    {
        private static string _swaggerVersion;

        public static string SwaggerVersion
        {
            get
            {
                if (_swaggerVersion == null)
                {
                    var version = Global.Version;
                    _swaggerVersion = $"v{version.Major}.{version.Minor}.{version.Build}";
                }

                return _swaggerVersion;
            }
        }

        public static void InitializeSwagger(SwaggerGenOptions options)
        {
            var info = GetOpenApiInfo();
            options.SwaggerDoc(SwaggerVersion, info);
            options.EnableAnnotations();
        }

        private static OpenApiInfo GetOpenApiInfo()
        {
            var result = new OpenApiInfo
            {
                Title = "Planar",
                Version = SwaggerVersion,
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