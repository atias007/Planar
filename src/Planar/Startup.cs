using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Planar.Filters;
using Planar.Service;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.General;
using Quartz.Logging;
using Serilog;
using System.Net;

namespace Planar
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("[x] Configure services");

            ////var quartzLogger = services.BuildServiceProvider().GetService<ILogger<QuartzLogProvider>>();
            ////LogProvider.SetCurrentLogProvider(new QuartzLogProvider(quartzLogger));

            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelStateAttribute>();
                options.Filters.Add<HttpResponseExceptionFilter>();
            });

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblies(new[] { GetType().Assembly, typeof(MainService).Assembly });

            var mvcBuilder = services.AddControllers();
            RegisterOData(mvcBuilder);

            if (AppSettings.SwaggerUI)
            {
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Planar", Version = "v1" });
                });
            }

            if (AppSettings.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = 2610;
                });
            }

            services.AddDbContext<PlanarContext>(o => o.UseSqlServer(
                    AppSettings.DatabaseConnectionString,
                    options => options.EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null)),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton
            );

            services.AddQuartzService();
            services.AddAutoMapper(Assembly.Load($"{nameof(Planar)}.{nameof(Service)}"));
            services.AddTransient<DataLayer>();
            services.AddTransient<MainService>();
            services.AddScoped<GroupDomain>();
            services.AddScoped<HistoryDomain>();
            services.AddScoped<JobDomain>();
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();
            services.AddScoped<ClusterUtil>();
            services.AddPlanarServices();
            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (AppSettings.DeveloperExceptionPage || !env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.SwaggerUI)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planar"));
            }

            // app.UseSerilogRequestLogging();

            if (AppSettings.UseHttpsRedirect)
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapGrpcService<ClusterService>();
            });
        }

        public void RegisterOData(IMvcBuilder builder)
        {
            builder.AddOData(option => option
                    .Select()
                    .Filter()
                    .Count()
                    .OrderBy()
                    .SetMaxTop(50)
                    .AddRouteComponents("odata", GetTraceEdmModel()));
        }

        public static IEdmModel GetTraceEdmModel()
        {
            var modelBuilder = new ODataConventionModelBuilder();
            var customers = modelBuilder.EntitySet<Service.Model.Trace>("TraceData");
            customers.EntityType.Page(50, 50);
            customers.EntityType.OrderBy("Id", "TimeStamp");

            var history = modelBuilder.EntitySet<Service.Model.JobInstanceLog>("HistoryData");
            history.EntityType.Page(50, 50);
            history.EntityType.OrderBy("Id", "StartDate", "EndDate", "Duration", "EffectedRows");

            return modelBuilder.GetEdmModel();
        }
    }
}