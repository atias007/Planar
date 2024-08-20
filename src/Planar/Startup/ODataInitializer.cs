using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Planar.Service.Model;

namespace Planar.Startup
{
    public static class ODataInitializer
    {
        public static void RegisterOData(IMvcBuilder builder)
        {
            builder.AddOData(option => option
                    .Select()
                    .Filter()
                    .Count()
                    .OrderBy()
                    .SetMaxTop(50)
                    .AddRouteComponents("odata", GetEdmModel()));
        }

        public static IEdmModel GetEdmModel()
        {
            var modelBuilder = new ODataConventionModelBuilder();
            var customers = modelBuilder.EntitySet<Trace>("TraceData");
            customers.EntityType.Page(50, 50);
            customers.EntityType.OrderBy(
                nameof(Trace.Id),
                nameof(Trace.TimeStamp));

            var history = modelBuilder.EntitySet<JobInstanceLog>("HistoryData");
            history.EntityType.Page(50, 50);
            history.EntityType.OrderBy(
                nameof(JobInstanceLog.Id),
                nameof(JobInstanceLog.StartDate),
                nameof(JobInstanceLog.EndDate),
                nameof(JobInstanceLog.Duration),
                nameof(JobInstanceLog.EffectedRows));

            return modelBuilder.GetEdmModel();
        }
    }
}