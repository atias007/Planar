using Microsoft.Extensions.Logging;
using Planner.Common;
using Planner.Service.Data;
using System;

namespace Planner.Service.JobListener.Base
{
    public class BaseJobListenerWithDataLayer<T> : BaseListener<T>
    {
        public DataLayer DAL
        {
            get
            {
                try
                {
                    return Global.ServiceProvider.GetService(typeof(DataLayer)) as DataLayer;
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error initialize DataLayer at BaseJobListenerWithDataLayer");
                    throw;
                }
            }
        }
    }
}