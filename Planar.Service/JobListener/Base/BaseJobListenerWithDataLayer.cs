using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using System;

namespace Planar.Service.JobListener.Base
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