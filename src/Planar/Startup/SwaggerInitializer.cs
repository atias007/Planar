using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Planar.Common;
using Swashbuckle.AspNetCore.Filters;
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

            // Authorization
            if (AppSettings.HasAuthontication)
            {
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "Standard authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                options.OperationFilter<SecurityRequirementsOperationFilter>();
            }

            options.EnableAnnotations();
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
                Version = SwaggerVersion,
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