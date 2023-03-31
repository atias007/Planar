using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Filters;
using Planar.Service;
using System;
using System.Net;

namespace Planar.Startup
{
    public static class ServiceCollectionInitializer
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("[x] Configure services");

            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelStateAttribute>();
                options.Filters.Add<HttpResponseExceptionFilter>();
            });

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
    }
}