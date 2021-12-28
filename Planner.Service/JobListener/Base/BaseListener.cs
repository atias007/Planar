using Microsoft.Extensions.Logging;
using Planner.Common;

namespace Planner.Service.JobListener.Base
{
    public class BaseListener<T>
    {
        private readonly Singleton<ILogger<T>> _logger = new(GetLogger);

        private static ILogger<T> GetLogger()
        {
            return Global.ServiceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>;
        }

        public ILogger<T> Logger
        {
            get
            {
                return _logger.Instance;
            }
        }
    }
}