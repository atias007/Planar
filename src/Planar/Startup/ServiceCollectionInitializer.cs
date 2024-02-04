using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Planar.API.Common.Entities;
using Planar.Authorization;
using Planar.Common;
using Planar.Filters;
using Planar.Service;
using Planar.Service.Services;
using System;
using System.Net;
using System.Threading.RateLimiting;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Prometheus;

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
            services.UseHttpClientMetrics();
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly, typeof(MainService).Assembly });

            var mvcBuilder = services.AddControllers();
            ODataInitializer.RegisterOData(mvcBuilder);

            if (AppSettings.General.SwaggerUI)
            {
                services.AddSwaggerGen(SwaggerInitializer.InitializeSwagger);
                services.AddFluentValidationRulesToSwagger();
            }

            if (AppSettings.General.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = AppSettings.General.HttpsPort;
                });
            }

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
                options.AddConcurrencyLimiter("concurrency", config =>
                {
                    config.PermitLimit = AppSettings.General.ConcurrencyRateLimiting;
                    config.QueueLimit = AppSettings.General.ConcurrencyRateLimiting / 2;
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });

            services.AddMemoryCache();
            services.AddPlanarServices();
            services.AddGrpc();
        }

        private static void SetAuthorization(IServiceCollection services)
        {
            if (AppSettings.Authentication.HasAuthontication)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = AuthenticationSettings.AuthenticationAudience,
                        ValidIssuer = AuthenticationSettings.AuthenticationIssuer,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = AppSettings.Authentication.Key
                    };
                });
            }

            // ATTENTION: Always set this policies event when no Authontication
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