using Core.JsonConvertors;
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
using Prometheus;
using System;
using System.Net;
using System.Threading.RateLimiting;

namespace Planar.Startup;

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
        services.AddValidatorsFromAssemblies([typeof(Program).Assembly, typeof(MainService).Assembly]);

        var mvcBuilder = services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new SystemTextTimeSpanConverter());
                o.JsonSerializerOptions.Converters.Add(new SystemTextNullableTimeSpanConverter());
            });

        ODataInitializer.RegisterOData(mvcBuilder);

        if (AppSettings.General.SwaggerUI)
        {
            services.AddSwaggerGen(SwaggerInitializer.InitializeSwagger);
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
                config.QueueLimit = AppSettings.General.ConcurrencyRateLimiting;
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = AuthenticationSettings.AuthenticationAudience,
                    ValidIssuer = AuthenticationSettings.AuthenticationIssuer,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = AppSettings.Authentication.Key
                };
                ////options.Events = new JwtBearerEvents
                ////{
                ////    OnAuthenticationFailed = context =>
                ////    {
                ////        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                ////        {
                ////            context.Response.Headers.Append("Token-Expired", "true");
                ////        }
                ////        return System.Threading.Tasks.Task.CompletedTask;
                ////    },
                ////    OnTokenValidated = context =>
                ////    {
                ////        return System.Threading.Tasks.Task.CompletedTask;
                ////    },
                ////    OnChallenge = context =>
                ////    {
                ////        return System.Threading.Tasks.Task.CompletedTask;
                ////    },
                ////    OnMessageReceived = context =>
                ////    {
                ////        return System.Threading.Tasks.Task.CompletedTask;
                ////    },
                ////    OnForbidden = context =>
                ////    {
                ////        return System.Threading.Tasks.Task.CompletedTask;
                ////    },
                ////};
            });
        }

        // ATTENTION: Always set this policies event when no Authontication
        services.AddAuthorizationBuilder()
            .AddPolicy(Roles.Administrator.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Administrator)))
            .AddPolicy(Roles.Editor.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Editor)))
            .AddPolicy(Roles.Tester.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Tester)))
            .AddPolicy(Roles.Viewer.ToString(), policy => policy.Requirements.Add(new MinimumRoleRequirement(Roles.Viewer)));

        services.AddSingleton<IAuthorizationHandler, MinimumRoleHandler>();
    }
}