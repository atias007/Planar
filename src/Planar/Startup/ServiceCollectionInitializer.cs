using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Planar.Authorization;
using Planar.Common;
using Planar.Filters;
using Planar.Service;
using Planar.Service.Model.DataObjects;
using System;
using System.Net;

namespace Planar.Startup
{
    public static class ServiceCollectionInitializer
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("[x] Configure services");

            SetAuthorization(services);

            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelStateAttribute>();
                options.Filters.Add<HttpResponseExceptionFilter>();
            });

            services.AddHttpContextAccessor();
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly, typeof(MainService).Assembly });

            var mvcBuilder = services.AddControllers();
            ODataInitializer.RegisterOData(mvcBuilder);

            if (AppSettings.SwaggerUI)
            {
                services.AddSwaggerGen(SwaggerInitializer.InitializeSwagger);
            }

            if (AppSettings.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = AppSettings.HttpsPort;
                });
            }

            services.AddMemoryCache();
            services.AddPlanarServices();
            services.AddGrpc();
        }

        private static void SetAuthorization(IServiceCollection services)
        {
            if (AppSettings.AuthenticationMode != AuthMode.AllAnonymous)
            {
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = AppSettings.AuthenticationKey,
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                    });
            }

            services.AddAuthorization(option =>
            {
                option.AddPolicy(Roles.Administrator.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Administrator)));
                option.AddPolicy(Roles.Editor.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Editor)));
                option.AddPolicy(Roles.Tester.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Tester)));
                option.AddPolicy(Roles.Viewer.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Viewer)));
            });

            services.AddSingleton<IAuthorizationHandler, MinimumRoleHandler>();
        }
    }
}