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
            const string lic = "https://opensource.org/licenses/MIT";
            const string planarSite = "http://www.planar.me";
            const string email = "admin@planar.me";
            const string name = "Tsahi Atias";
            const string license = "MIT";
            const string description = "Enterprise schedule system API";
            var result = new OpenApiInfo
            {
                Title = nameof(Planar),
                Version = SwaggerVersion,
                Contact = new OpenApiContact
                {
                    Email = email,
                    Name = name,
                    Url = new Uri(planarSite)
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